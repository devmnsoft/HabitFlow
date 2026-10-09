using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed record WebhookDispatchPayload(
    Guid EventId,
    string Event,
    Guid ClientId,
    Guid? UserId,
    DateTime Timestamp,
    object Data
);

public sealed class WebhookDispatcherService(
    IIntegrationRepository repository,
    PlanEntitlementService entitlements,
    HttpClient httpClient,
    ILogger<WebhookDispatcherService> logger)
{
    private const int MaxConsecutiveFailuresBeforePause = 5;

    public static string ComputeHmacSha256(string secret, string payload)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var bytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(key);
        return Convert.ToHexString(hmac.ComputeHash(bytes)).ToLowerInvariant();
    }

    public async Task<int> DispatchEventAsync(Guid clientId, Guid? userId, string eventName, object data, CancellationToken ct = default)
    {
        // Valida se o plano do tenant autoriza webhooks
        var allowed = await entitlements.CanUseFeatureAsync(userId ?? Guid.Empty, PlanFeatureCodes.Webhooks, ct);
        if (!allowed)
        {
            logger.LogInformation("Webhooks bloqueados pelo plano para tenant {ClientId}", clientId);
            return 0;
        }

        var webhooks = await repository.ListActiveWebhooksForEventAsync(clientId, eventName, ct);
        if (webhooks.Count == 0) return 0;

        var eventId = Guid.NewGuid();
        var payloadObj = new WebhookDispatchPayload(eventId, eventName, clientId, userId, DateTime.UtcNow, data);
        var jsonPayload = JsonSerializer.Serialize(payloadObj);

        var dispatchedCount = 0;
        foreach (var webhook in webhooks)
        {
            try
            {
                var success = await SendAttemptAsync(webhook, eventId, eventName, jsonPayload, 1, ct);
                if (success) dispatchedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro inesperado ao despachar webhook {WebhookId} evento {EventName}", webhook.Id, eventName);
            }
        }

        return dispatchedCount;
    }

    public async Task<bool> SendAttemptAsync(IntegrationWebhook webhook, Guid eventId, string eventName, string jsonPayload, short attemptNumber, CancellationToken ct = default)
    {
        var attemptId = Guid.NewGuid();
        var sw = Stopwatch.StartNew();
        var signature = ComputeHmacSha256(webhook.SecretCiphertext, jsonPayload);

        using var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-HabitFlow-Signature", $"sha256={signature}");
        request.Headers.Add("X-HabitFlow-Event", eventName);
        request.Headers.Add("X-HabitFlow-Delivery", attemptId.ToString());
        request.Headers.Add("User-Agent", "HabitFlow-Webhook/6.25.0");

        int? responseCode = null;
        string? responseBody = null;
        string? errorMessage = null;
        var status = "Failed";

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            var response = await httpClient.SendAsync(request, timeoutCts.Token);
            sw.Stop();
            responseCode = (int)response.StatusCode;

            var body = await response.Content.ReadAsStringAsync(ct);
            responseBody = body.Length <= 500 ? body : body[..500];

            if (response.IsSuccessStatusCode)
            {
                status = "Success";
                await repository.UpdateWebhookSuccessAsync(webhook.Id, ct);
                await repository.AddAuditAsync(webhook.ClientId, webhook.UserId, "webhook.delivery.succeeded", new { webhookId = webhook.Id, eventName, responseCode }, ct);
            }
            else
            {
                var autoPause = webhook.ConsecutiveFailures + 1 >= MaxConsecutiveFailuresBeforePause;
                await repository.UpdateWebhookFailureAsync(webhook.Id, autoPause, ct);
                await repository.AddAuditAsync(webhook.ClientId, webhook.UserId, "webhook.delivery.failed", new { webhookId = webhook.Id, eventName, responseCode, autoPause }, ct);
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            errorMessage = ex.Message;
            var autoPause = webhook.ConsecutiveFailures + 1 >= MaxConsecutiveFailuresBeforePause;
            await repository.UpdateWebhookFailureAsync(webhook.Id, autoPause, ct);
            await repository.AddAuditAsync(webhook.ClientId, webhook.UserId, "webhook.delivery.failed", new { webhookId = webhook.Id, eventName, error = ex.Message, autoPause }, ct);
        }

        var attempt = new WebhookDeliveryAttemptRecord(
            attemptId,
            webhook.Id,
            webhook.ClientId,
            eventId,
            eventName,
            attemptNumber,
            status,
            responseCode,
            responseBody,
            errorMessage,
            (int)sw.ElapsedMilliseconds,
            DateTime.UtcNow
        );

        await repository.RecordDeliveryAttemptAsync(attempt, ct);
        return status == "Success";
    }

    public async Task<Result> RetryDeliveryAsync(Guid clientId, Guid webhookId, Guid attemptId, CancellationToken ct = default)
    {
        var webhook = await repository.GetWebhookByIdAsync(clientId, webhookId, ct);
        if (webhook is null) return Result.Failure("webhook.not_found", "Webhook não encontrado.");

        var attempt = await repository.GetDeliveryAttemptAsync(clientId, attemptId, ct);
        if (attempt is null) return Result.Failure("attempt.not_found", "Tentativa de entrega não encontrada.");

        var payloadObj = new WebhookDispatchPayload(attempt.EventId, attempt.EventName, clientId, webhook.UserId, DateTime.UtcNow, new { retry = true });
        var jsonPayload = JsonSerializer.Serialize(payloadObj);

        var success = await SendAttemptAsync(webhook, attempt.EventId, attempt.EventName, jsonPayload, (short)(attempt.Attempt + 1), ct);
        return success
            ? Result.Success()
            : Result.Failure("webhook.retry_failed", "Tentativa de reenvio falhou. Verifique os logs de entrega.");
    }

    public async Task<CreatedSecret<IntegrationWebhook>> CreateWebhookAsync(Guid clientId, Guid userId, string name, string url, IEnumerable<string> events, CancellationToken ct = default)
    {
        name = name.Trim();
        if (name.Length is < 2 or > 80) throw new ArgumentException("O nome do webhook deve ter entre 2 e 80 caracteres.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new ArgumentException("A URL do webhook deve ser um endereço HTTPS válido.");

        var validEvents = events.Distinct(StringComparer.OrdinalIgnoreCase).Where(e => WebhookEvents.All.Contains(e, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (validEvents.Length == 0) throw new ArgumentException("Pelo menos um evento válido deve ser selecionado.");

        var secret = "whsec_" + Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var webhook = new IntegrationWebhook(Guid.NewGuid(), clientId, userId, name, url, validEvents, secret, true, DateTime.UtcNow, null);

        await repository.CreateWebhookAsync(webhook, ct);
        await repository.AddAuditAsync(clientId, userId, "webhook.created", new { webhook.Id, webhook.Name, webhook.Url, webhook.Events }, ct);
        return new(webhook, secret);
    }
}
