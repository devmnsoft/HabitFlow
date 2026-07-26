using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class PlanCatalogRepository(SqlExecutor db) : IPlanCatalogRepository
{
    public async Task<IReadOnlyList<PublicPlan>> GetPublicCatalogAsync(CancellationToken ct = default)
    {
        var plans = (await db.QueryAsync<PlanRow>("""
            select id, code, public_name, headline, description, audience_text, badge_text, is_featured, sort_order
            from habitflow.plans where is_active and is_public order by sort_order, public_name
            """, null, ct)).ToList();
        var result = new List<PublicPlan>(plans.Count);
        foreach (var plan in plans)
        {
            var prices = (await db.QueryAsync<PlanPrice>("""
                select distinct on (billing_cycle) id, billing_cycle, amount, currency
                from habitflow.plan_prices
                where plan_id=@id and is_active and valid_from <= now() and (valid_until is null or valid_until > now())
                order by billing_cycle, valid_from desc
                """, new { plan.Id }, ct)).ToList();
            var features = (await GetFeaturesAsync(plan.Code, ct)).Values.ToList();
            result.Add(new(plan.Id, plan.Code, plan.PublicName, plan.Headline, plan.Description, plan.AudienceText, plan.BadgeText, plan.IsFeatured, plan.SortOrder, prices, features));
        }
        return result;
    }

    public Task<ClientPlanAccess?> GetClientAccessAsync(Guid clientId, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<ClientPlanAccess>("select id client_id, contracted_plan_code, effective_plan_code, benefits_status, grace_period_until from habitflow.clients where id=@clientId", new { clientId }, ct);

    public Task<Guid?> GetClientIdForUserAsync(Guid userId, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<Guid?>("select client_id from habitflow.users where id=@userId", new { userId }, ct);

    public async Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetFeaturesAsync(string planCode, CancellationToken ct = default) =>
        (await db.QueryAsync<PlanFeatureValue>("""
            select f.code, f.name, f.value_type, pf.bool_value, pf.int_value, pf.string_value
            from habitflow.plans p join habitflow.plan_features pf on pf.plan_id=p.id
            join habitflow.feature_catalog f on f.code=pf.feature_code and f.is_active
            where p.code=@planCode
            """, new { planCode }, ct)).ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

    private sealed record PlanRow(Guid Id, string Code, string PublicName, string? Headline, string? Description, string? AudienceText, string? BadgeText, bool IsFeatured, int SortOrder);
}
