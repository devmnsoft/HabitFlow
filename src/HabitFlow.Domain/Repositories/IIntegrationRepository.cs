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
    Task CreateWebhookAsync(IntegrationWebhook webhook, CancellationToken ct = default);
    Task AddAuditAsync(Guid clientId, Guid userId, string eventName, object metadata, CancellationToken ct = default);
}
