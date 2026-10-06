using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class BillingEventLogRepository(SqlExecutor db) : IBillingEventLogRepository
{
    public Task LogAsync(Guid? clientId, Guid? userId, string eventCode, string status, string? provider, string? planCode, string? sanitizedMetadataJson, Guid correlationId, CancellationToken ct = default) =>
        db.ExecuteAsync(
            """
            insert into habitflow.billing_event_log(id, client_id, user_id, event_code, correlation_id, status, provider, plan_code, sanitized_metadata)
            values (@id, @clientId, @userId, @eventCode, @correlationId, @status, @provider, @planCode, coalesce(@metadata::jsonb, '{}'::jsonb))
            """,
            new { id = Guid.NewGuid(), clientId, userId, eventCode, correlationId, status, provider, planCode, metadata = string.IsNullOrWhiteSpace(sanitizedMetadataJson) ? null : sanitizedMetadataJson }, ct);
}
