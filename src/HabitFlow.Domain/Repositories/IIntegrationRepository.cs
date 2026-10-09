namespace HabitFlow.Domain;

public interface IIntegrationRepository
{
    Task<ApiKeyRecord?> FindApiKeyAsync(string hash, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKeyRecord>> ListApiKeysAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task CreateApiKeyAsync(ApiKeyRecord key, CancellationToken ct = default);
    Task<bool> RenameApiKeyAsync(Guid clientId, Guid userId, Guid id, string name, CancellationToken ct = default);
    Task<bool> RevokeApiKeyAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default);
    Task TouchApiKeyAsync(Guid id, CancellationToken ct = default);
    Task<CalendarFeed?> GetCalendarFeedAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<CalendarFeed?> FindCalendarFeedAsync(string tokenHash, CancellationToken ct = default);
    Task UpsertCalendarFeedAsync(CalendarFeed feed, CancellationToken ct = default);
    Task TouchCalendarFeedAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationWebhook>> ListWebhooksAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<IntegrationWebhook?> GetWebhookByIdAsync(Guid clientId, Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationWebhook>> ListActiveWebhooksForEventAsync(Guid clientId, string eventName, CancellationToken ct = default);
    Task CreateWebhookAsync(IntegrationWebhook webhook, CancellationToken ct = default);
    Task<bool> ToggleWebhookAsync(Guid clientId, Guid userId, Guid id, bool enabled, CancellationToken ct = default);
    Task<bool> DeleteWebhookAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default);
    Task UpdateWebhookSuccessAsync(Guid id, CancellationToken ct = default);
    Task UpdateWebhookFailureAsync(Guid id, bool autoPause, CancellationToken ct = default);
    Task RecordDeliveryAttemptAsync(WebhookDeliveryAttemptRecord attempt, CancellationToken ct = default);
    Task<IReadOnlyList<WebhookDeliveryAttemptRecord>> ListDeliveryAttemptsAsync(Guid clientId, Guid webhookId, int limit = 20, CancellationToken ct = default);
    Task<WebhookDeliveryAttemptRecord?> GetDeliveryAttemptAsync(Guid clientId, Guid attemptId, CancellationToken ct = default);
    Task AddAuditAsync(Guid clientId, Guid userId, string eventName, object metadata, CancellationToken ct = default);
}
