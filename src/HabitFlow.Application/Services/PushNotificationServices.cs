using System.Text.Json;
using System.Net;
using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class PushNotificationOptions
{
    public bool Enabled { get; set; }
    public string Subject { get; set; } = "mailto:suporte@habitflow.app";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}

public sealed record PushPayload(string Title, string Body, string Url = "/my-day");
public sealed record PushSendResult(bool Succeeded, bool SubscriptionInvalid, string? ErrorCode = null);
public interface IPushNotificationSender { Task<PushSendResult> SendAsync(PushSubscriptionRecord subscription, PushPayload payload, CancellationToken ct = default); }

public static class PushEndpointSafetyPolicy
{
    private static readonly StringComparer IgnoreCase = StringComparer.OrdinalIgnoreCase;

    public static async Task<bool> IsAllowedAsync(string endpoint, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length > 0 || !uri.IsDefaultPort)
            return false;
        if (IsBlockedHostName(uri.Host)) return false;
        if (IPAddress.TryParse(uri.Host, out var literal)) return IsPublicAddress(literal);
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.Host, ct);
            return addresses.Length > 0 && addresses.All(IsPublicAddress);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsBlockedHostName(string host)
    {
        var normalized = host.TrimEnd('.');
        return IgnoreCase.Equals(normalized, "localhost")
            || normalized.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".local", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith(".internal", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address)) return false;
        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.Broadcast)) return false;
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return b[0] != 10
                && !(b[0] == 127)
                && !(b[0] == 169 && b[1] == 254)
                && !(b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                && !(b[0] == 192 && b[1] == 168)
                && !(b[0] == 100 && b[1] >= 64 && b[1] <= 127)
                && !(b[0] == 0)
                && !(b[0] >= 224);
        }
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            return !address.IsIPv6LinkLocal && !address.IsIPv6SiteLocal && !address.IsIPv6Multicast && !address.Equals(IPAddress.IPv6Loopback);
        return false;
    }
}

public sealed class PushSubscriptionService(IPushSubscriptionRepository repository, TimeProvider clock)
{
    public Task<IReadOnlyList<PushSubscriptionRecord>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default) => repository.ListAsync(clientId, userId, false, ct);
    public Task<bool> RemoveAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) => repository.RemoveAsync(clientId, userId, id, ct);
    public async Task SubscribeAsync(Guid clientId, Guid userId, string endpoint, string p256dh, string auth, string device, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(p256dh) || string.IsNullOrWhiteSpace(auth) || !await PushEndpointSafetyPolicy.IsAllowedAsync(endpoint, ct))
            throw new ArgumentException("Subscription de push inválida.");
        await repository.UpsertAsync(new(Guid.NewGuid(), clientId, userId, endpoint, p256dh, auth,
            string.IsNullOrWhiteSpace(device) ? "Este dispositivo" : device[..Math.Min(device.Length, 80)], true, clock.GetUtcNow().UtcDateTime, clock.GetUtcNow().UtcDateTime), ct);
    }
}

public sealed class PushNotificationPreferenceService(IPushSubscriptionRepository repository)
{
    public Task<PushNotificationPreference> GetAsync(Guid clientId, Guid userId, CancellationToken ct = default) => repository.GetPreferenceAsync(clientId, userId, ct);
    public Task SaveAsync(PushNotificationPreference value, CancellationToken ct = default)
    {
        if ((value.QuietStart is null) != (value.QuietEnd is null) || (value.QuietStart is not null && value.QuietStart == value.QuietEnd))
            throw new ArgumentException("Informe o início e o fim do horário silencioso, usando horários diferentes.");
        if (!TimeZoneInfo.GetSystemTimeZones().Any(zone => zone.Id == value.Timezone))
            throw new ArgumentException("Timezone inválido.");
        if (value.Language is not ("pt-BR" or "en-US" or "es-ES" or "fr-FR")) throw new ArgumentException("Idioma inválido.");
        return repository.SavePreferenceAsync(value with { MaximumPerDay = Math.Clamp(value.MaximumPerDay, 1, 20) }, ct);
    }
}

public sealed class PushNotificationService(IPushSubscriptionRepository repository, IPushNotificationSender sender,
    Microsoft.Extensions.Options.IOptions<PushNotificationOptions> options, TimeProvider clock)
{
    public async Task<int> SendSafeReminderAsync(Guid clientId, Guid userId, PushReminderContext? context = null, CancellationToken ct = default)
    {
        var preference = await repository.GetPreferenceAsync(clientId, userId, ct);
        if (!options.Value.Enabled || !preference.PushEnabled || !preference.HabitReminders || preference.PausedUntil > clock.GetUtcNow().UtcDateTime) return 0;
        var localNow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(clock.GetUtcNow(), preference.Timezone);
        var localTime = localNow.TimeOfDay;
        if (preference.QuietStart is { } start && preference.QuietEnd is { } end &&
            (start < end ? localTime >= start.ToTimeSpan() && localTime < end.ToTimeSpan() : localTime >= start.ToTimeSpan() || localTime < end.ToTimeSpan())) return 0;
        var subscriptions = await repository.ListAsync(clientId, userId, true, ct);
        if (subscriptions.Count == 0) return 0;
        var delivery = context ?? new PushReminderContext(null, clock.GetUtcNow(), "BrowserPush");
        var localDate = DateOnly.FromDateTime(localNow.DateTime);
        if (!await repository.TryReserveDailyQuotaAsync(clientId, userId, localDate, delivery.Channel, preference.MaximumPerDay, ct)) return 0;
        var sent = 0;
        foreach (var subscription in subscriptions)
        {
            if (!await PushEndpointSafetyPolicy.IsAllowedAsync(subscription.Endpoint, ct))
            {
                await repository.DeactivateAsync(clientId, userId, subscription.Id, ct);
                continue;
            }
            var attemptedAt = clock.GetUtcNow().UtcDateTime;
            var attempt = new PushDeliveryAttempt(Guid.NewGuid(), clientId, userId, subscription.Id, "Processing",
                null, attemptedAt, delivery.Channel, delivery.ScheduledFor.UtcDateTime, delivery.ReminderId);
            if (!await repository.TryBeginAttemptAsync(attempt, ct)) continue;
            PushSendResult result;
            try { result = await sender.SendAsync(subscription, new("Hora do seu hábito", "Você tem um hábito planejado para agora."), ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch { result = new(false, false, "provider_error"); }
            var status = result.Succeeded ? "ProviderAccepted" : result.SubscriptionInvalid ? "SubscriptionInvalid" : "Failed";
            await repository.CompleteAttemptAsync(clientId, userId, attempt.Id, status, result.ErrorCode, clock.GetUtcNow().UtcDateTime, ct);
            if (result.SubscriptionInvalid) await repository.DeactivateAsync(clientId, userId, subscription.Id, ct);
            if (result.Succeeded) sent++;
        }
        if (sent == 0) await repository.ReleaseDailyQuotaAsync(clientId, userId, localDate, delivery.Channel, ct);
        return sent;
    }
}
