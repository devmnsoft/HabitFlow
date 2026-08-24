namespace HabitFlow.Domain;

public sealed record PushSubscriptionRecord(Guid Id, Guid ClientId, Guid UserId, string Endpoint,
    string P256Dh, string Auth, string DeviceName, bool IsActive, DateTime CreatedAt, DateTime? LastSeenAt);

public sealed record PushNotificationPreference(Guid ClientId, Guid UserId, bool PushEnabled,
    bool InternalEnabled, TimeOnly? QuietStart, TimeOnly? QuietEnd, int MaximumPerDay, DateTime? PausedUntil);

public sealed record PushDeliveryAttempt(Guid Id, Guid ClientId, Guid UserId, Guid SubscriptionId,
    string Status, string? ErrorCode, DateTime AttemptedAt);

public interface IPushSubscriptionRepository
{
    Task UpsertAsync(PushSubscriptionRecord subscription, CancellationToken ct = default);
    Task<IReadOnlyList<PushSubscriptionRecord>> ListAsync(Guid clientId, Guid userId, bool activeOnly = false, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid clientId, Guid userId, Guid subscriptionId, CancellationToken ct = default);
    Task DeactivateAsync(Guid clientId, Guid userId, Guid subscriptionId, CancellationToken ct = default);
    Task<PushNotificationPreference> GetPreferenceAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task SavePreferenceAsync(PushNotificationPreference preference, CancellationToken ct = default);
    Task RecordAttemptAsync(PushDeliveryAttempt attempt, CancellationToken ct = default);
}
