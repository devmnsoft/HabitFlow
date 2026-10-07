using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class AiAdminRepository(SqlExecutor db) : IAiAdminRepository
{
    private const string Day = "created_at >= (date_trunc('day', now() AT TIME ZONE 'utc') AT TIME ZONE 'utc')";

    public async Task<AiRuntimeSettings> GetSettingsAsync(CancellationToken ct = default)
    {
        var row = await db.QuerySingleOrDefaultAsync<AiRuntimeSettings>("""
            select enabled, default_provider, groq_enabled, gemini_enabled, deepseek_enabled,
                   groq_models, gemini_models, deepseek_models, global_daily_limit, updated_at
            from habitflow.ai_settings where id=true
            """, null, ct);
        return row ?? new(false, "", false, false, false, "", "", "", 200, DateTime.UtcNow);
    }

    public Task SaveSettingsAsync(AiRuntimeSettings settings, Guid? userId, CancellationToken ct = default) => db.ExecuteAsync("""
        insert into habitflow.ai_settings(id, enabled, default_provider, groq_enabled, gemini_enabled, deepseek_enabled, groq_models, gemini_models, deepseek_models, global_daily_limit, updated_by, updated_at)
        values(true, @Enabled, @DefaultProvider, @GroqEnabled, @GeminiEnabled, @DeepSeekEnabled, @GroqModels, @GeminiModels, @DeepSeekModels, @GlobalDailyLimit, @userId, now())
        on conflict(id) do update set enabled=excluded.enabled, default_provider=excluded.default_provider,
          groq_enabled=excluded.groq_enabled, gemini_enabled=excluded.gemini_enabled, deepseek_enabled=excluded.deepseek_enabled,
          groq_models=excluded.groq_models, gemini_models=excluded.gemini_models, deepseek_models=excluded.deepseek_models,
          global_daily_limit=excluded.global_daily_limit, updated_by=excluded.updated_by, updated_at=now()
        """, new { settings.Enabled, settings.DefaultProvider, settings.GroqEnabled, settings.GeminiEnabled, settings.DeepSeekEnabled, settings.GroqModels, settings.GeminiModels, settings.DeepSeekModels, settings.GlobalDailyLimit, userId }, ct);

    public async Task<IReadOnlyList<AiPlanLimit>> ListPlanLimitsAsync(CancellationToken ct = default) =>
        (await db.QueryAsync<AiPlanLimit>("select plan_code, daily_limit from habitflow.ai_plan_limits order by plan_code", null, ct)).ToArray();

    public Task SavePlanLimitAsync(string planCode, int dailyLimit, CancellationToken ct = default) => db.ExecuteAsync("""
        insert into habitflow.ai_plan_limits(plan_code, daily_limit, updated_at) values(@planCode, @dailyLimit, now())
        on conflict(plan_code) do update set daily_limit=excluded.daily_limit, updated_at=now()
        """, new { planCode, dailyLimit }, ct);

    public Task<int> GetEffectiveDailyLimitAsync(Guid clientId, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<int>("""
        select coalesce(
          (select daily_limit from habitflow.ai_tenant_limits where client_id=@clientId),
          (select l.daily_limit from habitflow.ai_plan_limits l join habitflow.clients c on lower(c.effective_plan_code)=l.plan_code where c.id=@clientId),
          5)
        """, new { clientId }, ct);

    public Task<int> CountUserTodayAsync(Guid clientId, Guid userId, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<int>($"""
        select count(*)::int from habitflow.ai_usage_events where client_id=@clientId and user_id=@userId and event_code='ai.request.completed' and {Day}
        """, new { clientId, userId }, ct);

    public Task<int> CountGlobalTodayAsync(CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<int>($"""
        select count(*)::int from habitflow.ai_usage_events where event_code='ai.request.completed' and {Day}
        """, null, ct);

    public async Task RecordAsync(Guid clientId, Guid userId, string provider, string model, string status, string eventCode, string correlationId, int durationMs, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        await db.ExecuteAsync("""
            insert into habitflow.ai_usage_events(id, client_id, user_id, provider, model, status, event_code, correlation_id, duration_ms, created_at)
            values(@id, @clientId, @userId, @provider, @model, @status, @eventCode, @correlationId, @durationMs, now())
            """, new { id, clientId, userId, provider, model, status, eventCode, correlationId, durationMs }, ct);
        if (eventCode is "ai.request.failed" or "ai.provider.unavailable" or "ai.provider.timeout")
            await db.ExecuteAsync("""
                insert into habitflow.ai_provider_events(id, client_id, user_id, provider, model, status, event_code, correlation_id, created_at)
                values(@id, @clientId, @userId, @provider, @model, @status, @eventCode, @correlationId, now())
                """, new { id = Guid.NewGuid(), clientId, userId, provider, model, status, eventCode, correlationId }, ct);
        if (eventCode is "ai.request.blocked_by_guardrail")
            await db.ExecuteAsync("""
                insert into habitflow.ai_safety_events(id, client_id, user_id, status, event_code, correlation_id, created_at)
                values(@id, @clientId, @userId, @status, @eventCode, @correlationId, now())
                """, new { id = Guid.NewGuid(), clientId, userId, status, eventCode, correlationId }, ct);
    }

    public async Task<IReadOnlyList<AiUsageEventRow>> RecentAsync(Guid? clientId, int take, CancellationToken ct = default) =>
        (await db.QueryAsync<AiUsageEventRow>("""
            select id, client_id, user_id, provider, model, status, event_code, correlation_id, duration_ms, created_at
            from habitflow.ai_usage_events
            where @clientId is null or client_id=@clientId
            order by created_at desc limit @take
            """, new { clientId, take }, ct)).ToArray();

    public async Task<IReadOnlyList<AiUsageEventRow>> RecentFailuresAsync(Guid? clientId, int take, CancellationToken ct = default) =>
        (await db.QueryAsync<AiUsageEventRow>("""
            select id, client_id, user_id, provider, model, status, event_code, correlation_id, 0 as duration_ms, created_at
            from habitflow.ai_provider_events
            where @clientId is null or client_id=@clientId
            order by created_at desc limit @take
            """, new { clientId, take }, ct)).ToArray();

    public async Task<IReadOnlyList<AiTenantConsumption>> ConsumptionAsync(Guid? clientId, CancellationToken ct = default) =>
        (await db.QueryAsync<AiTenantConsumption>($"""
            select client_id,
                   (count(*) filter (where event_code='ai.request.completed'))::int as calls,
                   (count(*) filter (where event_code in ('ai.request.blocked_by_plan','ai.request.blocked_by_guardrail')))::int as blocked,
                   (count(*) filter (where event_code in ('ai.request.failed','ai.provider.unavailable','ai.provider.timeout')))::int as failed
            from habitflow.ai_usage_events
            where {Day} and (@clientId is null or client_id=@clientId)
            group by client_id
            order by calls desc
            limit 50
            """, new { clientId }, ct)).ToArray();
}
