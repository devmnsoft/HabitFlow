using System.Text.Json;
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

public sealed class PushSubscriptionService(IPushSubscriptionRepository repository, TimeProvider clock)
{
    public Task<IReadOnlyList<PushSubscriptionRecord>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default) => repository.ListAsync(clientId, userId, false, ct);
    public Task<bool> RemoveAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) => repository.RemoveAsync(clientId, userId, id, ct);
    public async Task SubscribeAsync(Guid clientId, Guid userId, string endpoint, string p256dh, string auth, string device, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(p256dh) || string.IsNullOrWhiteSpace(auth))
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
        if (value.Language is not ("pt-BR" or "en-US")) throw new ArgumentException("Idioma inválido.");
        return repository.SavePreferenceAsync(value with { MaximumPerDay = Math.Clamp(value.MaximumPerDay, 1, 20) }, ct);
    }
}

public sealed class PushNotificationService(IPushSubscriptionRepository repository, IPushNotificationSender sender,
    Microsoft.Extensions.Options.IOptions<PushNotificationOptions> options, TimeProvider clock)
{
    public async Task<int> SendSafeReminderAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        var preference = await repository.GetPreferenceAsync(clientId, userId, ct);
        if (!options.Value.Enabled || !preference.PushEnabled || !preference.HabitReminders || preference.PausedUntil > clock.GetUtcNow().UtcDateTime) return 0;
        var localTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(clock.GetUtcNow(), preference.Timezone).TimeOfDay;
        if (preference.QuietStart is { } start && preference.QuietEnd is { } end &&
            (start < end ? localTime >= start.ToTimeSpan() && localTime < end.ToTimeSpan() : localTime >= start.ToTimeSpan() || localTime < end.ToTimeSpan())) return 0;
        var sent = 0;
        foreach (var subscription in await repository.ListAsync(clientId, userId, true, ct))
        {
            PushSendResult result;
            try { result = await sender.SendAsync(subscription, new("Hora do seu hábito", "Você tem um hábito planejado para agora."), ct); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch { result = new(false, false, "provider_error"); }
            await repository.RecordAttemptAsync(new(Guid.NewGuid(), clientId, userId, subscription.Id, result.Succeeded ? "Delivered" : "Failed", result.ErrorCode, clock.GetUtcNow().UtcDateTime), ct);
            if (result.SubscriptionInvalid) await repository.DeactivateAsync(clientId, userId, subscription.Id, ct);
            if (result.Succeeded) sent++;
        }
        return sent;
    }
}
