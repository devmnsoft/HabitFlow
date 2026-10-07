using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class ProductActivationRepository(SqlExecutor db) : IProductActivationRepository
{
    private const string CoreSql = """
        select
            c.id as "ClientId",
            c.name as "Name",
            c.status as "Status",
            c.plan as "Plan",
            c.subscription_status as "SubscriptionStatus",
            c.payment_status as "PaymentStatus",
            c.benefits_status as "BenefitsStatus",
            coalesce((select count(*) from habitflow.users u where u.client_id = c.id and u.account_status = 'Active' and u.last_activity_at >= now() - interval '30 days'), 0)::int as "ActiveUsers",
            coalesce((select count(*) from habitflow.users u where u.client_id = c.id and (u.account_status <> 'Active' or u.last_activity_at is null or u.last_activity_at < now() - interval '30 days')), 0)::int as "InactiveUsers",
            coalesce((select count(*) from habitflow.habits h where h.client_id = c.id and h.is_archived = false), 0)::int as "Habits",
            exists(select 1 from habitflow.users u where u.client_id = c.id and u.role in ('Admin', 'TenantAdmin', 'TenantOwner')) as "HasAdmin",
            coalesce(o.company_data_completed, false) or (c.name is not null and length(btrim(c.name)) > 1 and c.email is not null) as "CompanyDone",
            coalesce(o.billing_data_completed, false) as "BillingDone",
            coalesce(o.first_user_invited, false) as "UsersInvited",
            coalesce(o.first_habit_created, false) or exists(select 1 from habitflow.habits h where h.client_id = c.id and h.is_archived = false) as "FirstHabit",
            coalesce(o.plan_reviewed, false) or c.plan is not null as "PlanReviewed",
            exists(select 1 from habitflow.users u where u.client_id = c.id and u.last_activity_at >= now() - interval '7 days') as "Active7Days",
            exists(select 1 from habitflow.users u where u.client_id = c.id and u.last_activity_at >= now() - interval '15 days') as "Active15Days"
        from habitflow.clients c
        left join habitflow.client_onboarding o on o.client_id = c.id
        where (@ClientId is null or c.id = @ClientId)
          and (@Plan is null or c.plan = @Plan)
          and (@Status is null or c.status = @Status)
          and (@Name is null or c.name ilike @Name)
          and (@From is null or c.created_at::date >= @From)
          and (@To is null or c.created_at::date <= @To)
        order by c.name
        limit 200
        """;

    public async Task<IReadOnlyList<TenantActivationRow>> ListTenantsAsync(HomologationQuery query, CancellationToken ct = default)
    {
        var parameters = new
        {
            query.ClientId,
            Plan = Allow(query.Plan, "Free", "Premium", "Enterprise"),
            Status = Allow(query.Status, "Active", "Inactive", "Blocked"),
            Name = Like(query.TenantName),
            query.From,
            query.To
        };
        var rows = (await db.QueryAsync<TenantActivationDbRow>(CoreSql, parameters, ct)).ToList();
        var completions = await TryCounts("""
            select h.client_id as "ClientId", count(*)::int as "Total"
            from habitflow.habit_completions hc
            join habitflow.habits h on h.id = hc.habit_id
            where hc.completed_date >= current_date - 7
            group by h.client_id
            """, ct);
        var tickets = await TryCounts("""
            select client_id as "ClientId", count(*)::int as "Total"
            from habitflow.support_tickets_v2
            where status in ('Open', 'InAnalysis', 'WaitingCustomer', 'WaitingMnsoft')
            group by client_id
            """, ct);
        var ai = await TryCounts("""
            select client_id as "ClientId", count(*)::int as "Total"
            from habitflow.ai_usage_events
            where created_at >= now() - interval '7 days' and event_code = 'ai.request.completed'
            group by client_id
            """, ct);
        var templates = await TryCounts("""
            select client_id as "ClientId", count(*)::int as "Total"
            from habitflow.habits
            where client_id is not null and source_template_id is not null and is_archived = false
            group by client_id
            """, ct);
        var notifications = await TryIds("""
            select distinct client_id as "ClientId"
            from habitflow.notification_preferences
            where internal_enabled = true or habit_reminders = true or weekly_summary = true
            """, ct);
        var support = await TryFlag("""
            select (support_email is not null and btrim(support_email) <> '')
            from habitflow.support_settings
            order by updated_at desc
            limit 1
            """, ct);
        var aiEnabled = await TryFlag("select enabled from habitflow.ai_settings where id = true", ct);
        return rows.Select(row => new TenantActivationRow(
            row.ClientId, row.Name, row.Status, row.Plan, row.SubscriptionStatus, row.PaymentStatus, row.BenefitsStatus,
            row.ActiveUsers, row.InactiveUsers, row.Habits,
            completions.Counts.GetValueOrDefault(row.ClientId), completions.Known,
            tickets.Counts.GetValueOrDefault(row.ClientId), tickets.Known,
            ai.Counts.GetValueOrDefault(row.ClientId), ai.Known,
            row.HasAdmin, row.CompanyDone, row.BillingDone, row.UsersInvited, row.FirstHabit, row.PlanReviewed,
            row.Active7Days, row.Active15Days, notifications.Known && notifications.Ids.Contains(row.ClientId),
            support, aiEnabled, templates.Counts.GetValueOrDefault(row.ClientId), templates.Known,
            notifications.Known)).ToList();
    }

    public async Task<IReadOnlyList<ImplantationOverride>> ListOverridesAsync(Guid clientId, CancellationToken ct = default)
    {
        try
        {
            var rows = await db.QueryAsync<OverrideRow>("""
                select step_code as "StepCode", status as "Status", reason as "Reason"
                from habitflow.tenant_onboarding_step_overrides
                where client_id = @clientId
                """, new { clientId }, ct);
            return rows.Select(row => new ImplantationOverride(row.StepCode, Parse(row.Status), row.Reason)).ToList();
        }
        catch (Exception ex) when (IsMissingSchema(ex))
        {
            return [];
        }
    }

    public Task SaveOverrideAsync(Guid clientId, Guid userId, string stepCode, string status, string? reason, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.tenant_onboarding_step_overrides(client_id, step_code, status, reason, updated_by, updated_at)
            values (@clientId, @stepCode, @status, @reason, @userId, now())
            on conflict (client_id, step_code) do update
            set status = excluded.status, reason = excluded.reason, updated_by = excluded.updated_by, updated_at = now()
            """, new { clientId, stepCode, status, reason, userId }, ct);

    public Task RecordNotificationEventAsync(Guid clientId, Guid? userId, string eventCode, string channel, string status, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.notification_events(id, client_id, user_id, event_code, channel, status, created_at)
            values (@id, @clientId, @userId, @eventCode, @channel, @status, now())
            """, new { id = Guid.NewGuid(), clientId, userId, eventCode, channel, status }, ct);

    public Task RecordCustomerSuccessAsync(Guid clientId, int? score, string status, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.customer_success_events(id, client_id, event_code, score, status, created_at)
            values (@id, @clientId, 'customer.health.calculated', @score, @status, now())
            """, new { id = Guid.NewGuid(), clientId, score, status }, ct);

    private async Task<(bool Known, Dictionary<Guid, int> Counts)> TryCounts(string sql, CancellationToken ct)
    {
        try
        {
            var rows = await db.QueryAsync<CountRow>(sql, null, ct);
            return (true, rows.ToDictionary(row => row.ClientId, row => row.Total));
        }
        catch (Exception ex) when (IsMissingSchema(ex))
        {
            return (false, []);
        }
    }

    private async Task<(bool Known, HashSet<Guid> Ids)> TryIds(string sql, CancellationToken ct)
    {
        try
        {
            var rows = await db.QueryAsync<IdRow>(sql, null, ct);
            return (true, rows.Select(row => row.ClientId).ToHashSet());
        }
        catch (Exception ex) when (IsMissingSchema(ex))
        {
            return (false, []);
        }
    }

    private async Task<bool?> TryFlag(string sql, CancellationToken ct)
    {
        try { return await db.QuerySingleOrDefaultAsync<bool?>(sql, null, ct); }
        catch (Exception ex) when (IsMissingSchema(ex)) { return null; }
    }

    private static bool IsMissingSchema(Exception ex)
    {
        var text = ex.ToString();
        return text.Contains("42P01", StringComparison.Ordinal) || text.Contains("42703", StringComparison.Ordinal);
    }

    private static string? Allow(string? value, params string[] allowed) =>
        allowed.FirstOrDefault(item => item.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? Like(string? value)
    {
        var text = (value ?? "").Trim().Replace("%", "", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
        return text.Length == 0 ? null : "%" + text + "%";
    }

    private static ImplantationStepStatus Parse(string? status) => status switch
    {
        "EmAndamento" => ImplantationStepStatus.EmAndamento,
        "Concluido" => ImplantationStepStatus.Concluido,
        "Bloqueado" => ImplantationStepStatus.Bloqueado,
        "Ignorado" => ImplantationStepStatus.Ignorado,
        _ => ImplantationStepStatus.Pendente
    };

    private sealed class TenantActivationDbRow
    {
        public Guid ClientId { get; init; }
        public string Name { get; init; } = "";
        public string Status { get; init; } = "";
        public string Plan { get; init; } = "";
        public string SubscriptionStatus { get; init; } = "";
        public string PaymentStatus { get; init; } = "";
        public string BenefitsStatus { get; init; } = "";
        public int ActiveUsers { get; init; }
        public int InactiveUsers { get; init; }
        public int Habits { get; init; }
        public bool HasAdmin { get; init; }
        public bool CompanyDone { get; init; }
        public bool BillingDone { get; init; }
        public bool UsersInvited { get; init; }
        public bool FirstHabit { get; init; }
        public bool PlanReviewed { get; init; }
        public bool Active7Days { get; init; }
        public bool Active15Days { get; init; }
    }

    private sealed class CountRow { public Guid ClientId { get; init; } public int Total { get; init; } }
    private sealed class IdRow { public Guid ClientId { get; init; } }
    private sealed class OverrideRow { public string StepCode { get; init; } = ""; public string Status { get; init; } = ""; public string? Reason { get; init; } }
}
