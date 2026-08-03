using HabitFlow.Domain;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Infrastructure;

public sealed class PlanCatalogRepository(SqlExecutor db, ILogger<PlanCatalogRepository> logger) : IPlanCatalogRepository
{
    private static readonly HashSet<string> KnownPlanCodes = new(StringComparer.OrdinalIgnoreCase)
        { PlanCodes.Free, PlanCodes.Ritmo, PlanCodes.Evolucao };
    private static readonly HashSet<string> KnownBenefitsStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "Free", "Active", "PastDue", "Blocked" };
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

    public async Task<ClientPlanAccess?> GetClientAccessAsync(Guid clientId, CancellationToken ct = default)
    {
        ClientPlanAccessRow? row;
        try
        {
            row = await db.QuerySingleOrDefaultAsync<ClientPlanAccessRow>("""
                select id as "ClientId",
                       coalesce(contracted_plan_code, 'free') as "ContractedPlanCode",
                       coalesce(effective_plan_code, 'free') as "EffectivePlanCode",
                       coalesce(benefits_status, 'Free') as "BenefitsStatus",
                       grace_period_until as "GracePeriodUntil"
                from habitflow.clients
                where id = @clientId
                """, new { clientId }, ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            RuntimeDiagnostics.PlanAccessQueryFailures.Add(1);
            RuntimeDiagnostics.DapperMaterializationFailures.Add(1);
            logger.LogError(ex, "Falha na consulta/materialização de acesso ao plano para cliente {ClientIdMask}.", Mask(clientId));
            throw;
        }

        if (row is null) return null;
        var effectivePlan = row.EffectivePlanCode;
        if (!KnownPlanCodes.Contains(row.ContractedPlanCode) || !KnownPlanCodes.Contains(effectivePlan))
        {
            logger.LogError("Código de plano desconhecido para cliente {ClientIdMask}. Contratado={ContractedPlanCode}; efetivo={EffectivePlanCode}. Acesso efetivo reduzido para free.",
                Mask(clientId), row.ContractedPlanCode, row.EffectivePlanCode);
            RuntimeDiagnostics.UnknownPlanCodes.Add(1);
            effectivePlan = PlanCodes.Free;
        }

        var benefitsStatus = row.BenefitsStatus;
        if (!KnownBenefitsStatuses.Contains(benefitsStatus))
        {
            logger.LogError("Status de benefícios inválido para cliente {ClientIdMask}: {BenefitsStatus}.", Mask(clientId), benefitsStatus);
            RuntimeDiagnostics.InvalidBenefitsStatus.Add(1);
            benefitsStatus = "Free";
            effectivePlan = PlanCodes.Free;
        }

        return new ClientPlanAccess(row.ClientId, row.ContractedPlanCode, effectivePlan, benefitsStatus, row.GracePeriodUntil);
    }

    public Task<Guid?> GetClientIdForUserAsync(Guid userId, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<Guid?>("select client_id from habitflow.users where id=@userId", new { userId }, ct);

    public async Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetFeaturesAsync(string planCode, CancellationToken ct = default) =>
        (await db.QueryAsync<PlanFeatureValue>("""
            select f.code, f.name, f.value_type, pf.bool_value, pf.int_value, pf.string_value
            from habitflow.plans p join habitflow.plan_features pf on pf.plan_id=p.id
            join habitflow.feature_catalog f on f.code=pf.feature_code and f.is_active
            where p.code=@planCode and f.implementation_status='Implemented' and f.is_marketable
            """, new { planCode }, ct)).ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

    public Task<bool> IsCheckoutEligibleAsync(string planCode, string billingCycle, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<bool>("""
            select exists(
              select 1 from habitflow.plans p
              join habitflow.plan_prices pp on pp.plan_id=p.id
              where p.code=@planCode and p.code <> 'free' and p.is_active and p.is_public and p.is_sellable
                and p.sales_status='Available' and pp.is_active and pp.billing_cycle=@billingCycle
                and pp.amount > 0 and pp.currency='BRL' and pp.valid_from <= now()
                and (pp.valid_until is null or pp.valid_until > now())
                and not exists (
                  select 1 from habitflow.plan_features pf join habitflow.feature_catalog f on f.code=pf.feature_code
                  where pf.plan_id=p.id and f.is_public and (f.implementation_status <> 'Implemented' or not f.is_marketable)))
            """, new { planCode, billingCycle }, ct);

    private sealed record PlanRow(Guid Id, string Code, string PublicName, string? Headline, string? Description, string? AudienceText, string? BadgeText, bool IsFeatured, int SortOrder);
    internal sealed class ClientPlanAccessRow
    {
        public Guid ClientId { get; set; }
        public string ContractedPlanCode { get; set; } = PlanCodes.Free;
        public string EffectivePlanCode { get; set; } = PlanCodes.Free;
        public string BenefitsStatus { get; set; } = "Free";
        public DateOnly? GracePeriodUntil { get; set; }
    }

    private static string Mask(Guid id) => $"{id:N}"[..8] + "…";
}
