using System.Text.Json;
using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class IntegrationRepository(SqlExecutor db) : IIntegrationRepository
{
    const string KeyColumns = "id,client_id,user_id,name,key_prefix,key_hash,scopes,created_at,last_used_at,revoked_at";
    const string WebhookColumns = "id,client_id,user_id,name,url,events,secret_ciphertext,enabled,created_at,last_success_at,consecutive_failures,last_failed_at,is_paused";
    const string DeliveryColumns = "id,webhook_id,client_id,event_id,event_name,attempt,status,response_code,response_body,error_message,duration_ms,created_at";

    public Task<ApiKeyRecord?> FindApiKeyAsync(string hash, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<ApiKeyRecord>($"select {KeyColumns} from habitflow.api_keys where key_hash=@hash and revoked_at is null", new { hash }, ct);

    public async Task<IReadOnlyList<ApiKeyRecord>> ListApiKeysAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        (await db.QueryAsync<ApiKeyRecord>($"select {KeyColumns} from habitflow.api_keys where client_id=@clientId and user_id=@userId order by created_at desc", new { clientId, userId }, ct)).ToList();

    public Task CreateApiKeyAsync(ApiKeyRecord k, CancellationToken ct = default) =>
        db.ExecuteAsync("insert into habitflow.api_keys(id,client_id,user_id,name,key_prefix,key_hash,scopes,created_at) values(@Id,@ClientId,@UserId,@Name,@KeyPrefix,@KeyHash,@Scopes,@CreatedAt)", k, ct);

    public async Task<bool> RenameApiKeyAsync(Guid clientId, Guid userId, Guid id, string name, CancellationToken ct = default) =>
        await db.ExecuteAsync("update habitflow.api_keys set name=@name where id=@id and client_id=@clientId and user_id=@userId and revoked_at is null", new { clientId, userId, id, name }, ct) > 0;

    public async Task<bool> RevokeApiKeyAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) =>
        await db.ExecuteAsync("update habitflow.api_keys set revoked_at=now() where id=@id and client_id=@clientId and user_id=@userId and revoked_at is null", new { clientId, userId, id }, ct) > 0;

    public Task TouchApiKeyAsync(Guid id, CancellationToken ct = default) =>
        db.ExecuteAsync("update habitflow.api_keys set last_used_at=now() where id=@id", new { id }, ct);

    public Task<CalendarFeed?> GetCalendarFeedAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<CalendarFeed>("select id,client_id,user_id,token_hash,enabled,include_habits,include_routines,created_at,last_used_at from habitflow.calendar_feeds where client_id=@clientId and user_id=@userId", new { clientId, userId }, ct);

    public Task<CalendarFeed?> FindCalendarFeedAsync(string tokenHash, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<CalendarFeed>("select id,client_id,user_id,token_hash,enabled,include_habits,include_routines,created_at,last_used_at from habitflow.calendar_feeds where token_hash=@tokenHash and enabled=true", new { tokenHash }, ct);

    public Task UpsertCalendarFeedAsync(CalendarFeed f, CancellationToken ct = default) =>
        db.ExecuteAsync("insert into habitflow.calendar_feeds(id,client_id,user_id,token_hash,enabled,include_habits,include_routines,created_at) values(@Id,@ClientId,@UserId,@TokenHash,@Enabled,@IncludeHabits,@IncludeRoutines,@CreatedAt) on conflict(client_id,user_id) do update set token_hash=excluded.token_hash,enabled=excluded.enabled,include_habits=excluded.include_habits,include_routines=excluded.include_routines", f, ct);

    public Task TouchCalendarFeedAsync(Guid id, CancellationToken ct = default) =>
        db.ExecuteAsync("update habitflow.calendar_feeds set last_used_at=now() where id=@id", new { id }, ct);

    public async Task<IReadOnlyList<IntegrationWebhook>> ListWebhooksAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        (await db.QueryAsync<IntegrationWebhook>($"select {WebhookColumns} from habitflow.integration_webhooks where client_id=@clientId and user_id=@userId order by created_at desc", new { clientId, userId }, ct)).ToList();

    public Task<IntegrationWebhook?> GetWebhookByIdAsync(Guid clientId, Guid id, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<IntegrationWebhook>($"select {WebhookColumns} from habitflow.integration_webhooks where id=@id and client_id=@clientId", new { clientId, id }, ct);

    public async Task<IReadOnlyList<IntegrationWebhook>> ListActiveWebhooksForEventAsync(Guid clientId, string eventName, CancellationToken ct = default) =>
        (await db.QueryAsync<IntegrationWebhook>($"select {WebhookColumns} from habitflow.integration_webhooks where client_id=@clientId and enabled=true and is_paused=false and @eventName = any(events)", new { clientId, eventName }, ct)).ToList();

    public Task CreateWebhookAsync(IntegrationWebhook w, CancellationToken ct = default) =>
        db.ExecuteAsync("insert into habitflow.integration_webhooks(id,client_id,user_id,name,url,events,secret_ciphertext,enabled,consecutive_failures,is_paused,created_at) values(@Id,@ClientId,@UserId,@Name,@Url,@Events,@SecretCiphertext,@Enabled,@ConsecutiveFailures,@IsPaused,@CreatedAt)", w, ct);

    public async Task<bool> ToggleWebhookAsync(Guid clientId, Guid userId, Guid id, bool enabled, CancellationToken ct = default) =>
        await db.ExecuteAsync("update habitflow.integration_webhooks set enabled=@enabled, is_paused=false, consecutive_failures=0 where id=@id and client_id=@clientId and user_id=@userId", new { clientId, userId, id, enabled }, ct) > 0;

    public async Task<bool> DeleteWebhookAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default) =>
        await db.ExecuteAsync("delete from habitflow.integration_webhooks where id=@id and client_id=@clientId and user_id=@userId", new { clientId, userId, id }, ct) > 0;

    public Task UpdateWebhookSuccessAsync(Guid id, CancellationToken ct = default) =>
        db.ExecuteAsync("update habitflow.integration_webhooks set last_success_at=now(), consecutive_failures=0 where id=@id", new { id }, ct);

    public Task UpdateWebhookFailureAsync(Guid id, bool autoPause, CancellationToken ct = default) =>
        db.ExecuteAsync("update habitflow.integration_webhooks set last_failed_at=now(), consecutive_failures=consecutive_failures+1, is_paused=(case when @autoPause then true else is_paused end) where id=@id", new { id, autoPause }, ct);

    public Task RecordDeliveryAttemptAsync(WebhookDeliveryAttemptRecord attempt, CancellationToken ct = default) =>
        db.ExecuteAsync("insert into habitflow.webhook_delivery_attempts(id,webhook_id,client_id,event_id,event_name,attempt,status,response_code,response_body,error_message,duration_ms,created_at) values(@Id,@WebhookId,@ClientId,@EventId,@EventName,@Attempt,@Status,@ResponseCode,@ResponseBody,@ErrorMessage,@DurationMs,@CreatedAt)", attempt, ct);

    public async Task<IReadOnlyList<WebhookDeliveryAttemptRecord>> ListDeliveryAttemptsAsync(Guid clientId, Guid webhookId, int limit = 20, CancellationToken ct = default) =>
        (await db.QueryAsync<WebhookDeliveryAttemptRecord>($"select {DeliveryColumns} from habitflow.webhook_delivery_attempts where client_id=@clientId and webhook_id=@webhookId order by created_at desc limit @limit", new { clientId, webhookId, limit }, ct)).ToList();

    public Task<WebhookDeliveryAttemptRecord?> GetDeliveryAttemptAsync(Guid clientId, Guid attemptId, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<WebhookDeliveryAttemptRecord>($"select {DeliveryColumns} from habitflow.webhook_delivery_attempts where id=@attemptId and client_id=@clientId", new { clientId, attemptId }, ct);

    public Task AddAuditAsync(Guid clientId, Guid userId, string eventName, object metadata, CancellationToken ct = default) =>
        db.ExecuteAsync("insert into habitflow.integration_events(id,client_id,user_id,event_name,metadata,created_at) values(gen_random_uuid(),@clientId,@userId,@eventName,cast(@json as jsonb),now())", new { clientId, userId, eventName, json = JsonSerializer.Serialize(metadata) }, ct);
}
