using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class SystemHealthRepository(SqlExecutor db) : ISystemHealthRepository
{
    public async Task<bool> PingDatabaseAsync(CancellationToken ct = default)
    {
        var res = await db.QuerySingleOrDefaultAsync<int>("select 1;", null, ct);
        return res == 1;
    }

    public async Task SaveHealthCheckAsync(SystemHealthStatusItem item, CancellationToken ct = default)
    {
        const string sql = """
            insert into habitflow.system_health_checks (
                component_name, status, duration_ms, details, error_message, operational_recommendation, last_checked_at
            ) values (
                @ComponentName, @Status, @DurationMs, @Details, @ErrorMessage, @OperationalRecommendation, @LastCheckedAt
            )
            on conflict (component_name) do update set
                status = excluded.status,
                duration_ms = excluded.duration_ms,
                details = excluded.details,
                error_message = excluded.error_message,
                operational_recommendation = excluded.operational_recommendation,
                last_checked_at = excluded.last_checked_at;
            """;

        await db.ExecuteAsync(sql, item, ct);
    }

    public async Task<IReadOnlyList<SystemHealthStatusItem>> GetLatestHealthChecksAsync(CancellationToken ct = default)
    {
        const string sql = """
            select
                component_name as "ComponentName",
                status as "Status",
                duration_ms as "DurationMs",
                details as "Details",
                error_message as "ErrorMessage",
                operational_recommendation as "OperationalRecommendation",
                last_checked_at as "LastCheckedAt"
            from habitflow.system_health_checks
            order by component_name asc;
            """;

        return (await db.QueryAsync<SystemHealthStatusItem>(sql, null, ct)).ToList();
    }

    public async Task RecordHistoryAsync(string componentName, string status, int durationMs, string? error, CancellationToken ct = default)
    {
        const string sql = """
            insert into habitflow.system_health_history (
                id, component_name, status, duration_ms, error_message, recorded_at
            ) values (
                gen_random_uuid(), @componentName, @status, @durationMs, @error, now()
            );
            """;

        await db.ExecuteAsync(sql, new { componentName, status, durationMs, error }, ct);
    }

    public async Task<IReadOnlyList<SystemHealthStatusItem>> GetHealthHistoryAsync(string? componentName = null, int limit = 50, CancellationToken ct = default)
    {
        var sql = """
            select
                component_name as "ComponentName",
                status as "Status",
                duration_ms as "DurationMs",
                null as "Details",
                error_message as "ErrorMessage",
                null as "OperationalRecommendation",
                recorded_at as "LastCheckedAt"
            from habitflow.system_health_history
            """;

        if (!string.IsNullOrWhiteSpace(componentName))
        {
            sql += " where component_name = @componentName";
        }

        sql += " order by recorded_at desc limit @limit";

        return (await db.QueryAsync<SystemHealthStatusItem>(sql, new { componentName, limit = Math.Clamp(limit, 1, 200) }, ct)).ToList();
    }
}
