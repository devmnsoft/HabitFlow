using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class OperationalAuditRepository(SqlExecutor db) : IOperationalAuditRepository
{
    public async Task RecordAuditAsync(OperationalAuditEvent auditEvent, CancellationToken ct = default)
    {
        const string sql = """
            insert into habitflow.operational_audit_events (
                id, event_name, correlation_id, client_id, executor_user_id, executor_email,
                severity, status, payload_json, occurred_at
            ) values (
                @Id, @EventName, @CorrelationId, @ClientId, @ExecutorUserId, @ExecutorEmail,
                @Severity, @Status, @PayloadJson::jsonb, coalesce(@OccurredAt, now())
            );
            """;

        await db.ExecuteAsync(sql, auditEvent, ct);
    }

    public async Task<IReadOnlyList<OperationalAuditEvent>> SearchAuditAsync(
        Guid? clientId = null,
        string? executorUserId = null,
        string? eventName = null,
        string? severity = null,
        DateTime? from = null,
        DateTime? to = null,
        int limit = 100,
        CancellationToken ct = default)
    {
        var sql = """
            select
                id as "Id",
                event_name as "EventName",
                correlation_id as "CorrelationId",
                client_id as "ClientId",
                executor_user_id as "ExecutorUserId",
                executor_email as "ExecutorEmail",
                severity as "Severity",
                status as "Status",
                payload_json::text as "PayloadJson",
                occurred_at as "OccurredAt"
            from habitflow.operational_audit_events
            where 1=1
            """;

        if (clientId.HasValue && clientId.Value != Guid.Empty)
        {
            sql += " and client_id = @clientId";
        }

        if (!string.IsNullOrWhiteSpace(executorUserId))
        {
            sql += " and (executor_user_id = @executorUserId or executor_email ilike @executorLike)";
        }

        if (!string.IsNullOrWhiteSpace(eventName))
        {
            sql += " and event_name ilike @eventLike";
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            sql += " and severity = @severity";
        }

        if (from.HasValue)
        {
            sql += " and occurred_at >= @from";
        }

        if (to.HasValue)
        {
            sql += " and occurred_at <= @to";
        }

        sql += " order by occurred_at desc limit @limit";

        var parameters = new
        {
            clientId,
            executorUserId,
            executorLike = string.IsNullOrWhiteSpace(executorUserId) ? null : $"%{executorUserId.Trim()}%",
            eventName,
            eventLike = string.IsNullOrWhiteSpace(eventName) ? null : $"%{eventName.Trim()}%",
            severity,
            from,
            to,
            limit = Math.Clamp(limit, 1, 500)
        };

        return (await db.QueryAsync<OperationalAuditEvent>(sql, parameters, ct)).ToList();
    }
}
