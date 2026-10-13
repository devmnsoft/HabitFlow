using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

/// <summary>v6.28.2 — regras comerciais: Free = trial de 15 dias com bloqueio total após expirar,
/// Ritmo R$ 10/mês com até 5 hábitos, troca de plano real e painel SuperAdmin de usuários.
/// Testes de arquivo (como SaaSVendableV6196Tests) para a migração e o gate SQL; testes de unidade
/// para as mensagens de bloqueio nos serviços de criação.</summary>
public sealed class CommercialRulesV6282Tests
{
    private static string Read(params string[] parts)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine(root, Path.Combine(parts)));
    }

    [Fact]
    public void Migration_110_declares_trial_gate_pricing_and_blocked_plan()
    {
        var sql = Read("database", "migrations", "110_v6282_commercial_trial_gate_ritmo_r10.sql");

        // Coluna de fim do trial no cliente + backfill de clientes em Trial.
        Assert.Contains("ADD COLUMN IF NOT EXISTS trial_ends_at", sql);
        Assert.Contains("subscription_status = 'Trial'", sql);
        Assert.Contains("interval '15 days'", sql);

        // Ritmo a R$ 10,00/mês e R$ 100,00/ano; limite de 5 hábitos ativos.
        Assert.Contains("SET amount = 10.00", sql);
        Assert.Contains("SET amount = 100.00", sql);
        Assert.Contains("int_value = 5", sql);
        Assert.Contains("'active_habits_limit'", sql);

        // Plano interno blocked: features desligadas, limites 0, histórico 30 dias, não público/não vendável.
        Assert.Contains("'blocked'", sql);
        Assert.Contains("is_public=false", sql);
        Assert.Contains("is_sellable=false", sql);
        Assert.Contains("'history_days_limit' THEN 30", sql);

        // Comunicação do trial de 15 dias no catálogo público.
        Assert.Contains("15 dias grátis", sql);
    }

    [Fact]
    public void Plan_catalog_repository_gates_trial_and_blocks_expired_clients()
    {
        var repo = Read("src", "HabitFlow.Infrastructure", "Repositories", "PlanCatalogRepository.cs");

        // O plano blocked é conhecido (sem isso o gate cairia para free por fail-closed).
        Assert.Contains("PlanCodes.Blocked", repo);
        // O gate: trial ativo -> ritmo; trial expirado -> blocked; motivo de restrição explícito.
        Assert.Contains("subscription_status = 'Trial'", repo);
        Assert.Contains("'ritmo'", repo);
        Assert.Contains("'blocked'", repo);
        Assert.Contains("terminou. Assine o plano Ritmo", repo);
        // Benefícios persistidos pelo enum precisam ser aceitos (bug corrigido: PremiumActive caía para Free).
        Assert.Contains("\"PremiumActive\"", repo);
        Assert.Contains("\"EnterpriseActive\"", repo);
    }

    [Fact]
    public void Superadmin_users_dashboard_lists_and_blocks_with_audit()
    {
        var repo = Read("src", "HabitFlow.Infrastructure", "Repositories", "SaaSOperationsRepositories.cs");
        Assert.Contains("ListUsersAsync", repo);
        Assert.Contains("SetUserBlockedAsync", repo);
        // Bloqueio encerra sessões (session_version) e é auditado; SuperAdmin nunca é bloqueável.
        Assert.Contains("session_version=session_version+1", repo);
        Assert.Contains("role <> 'SuperAdmin'", repo);
        Assert.Contains("user.block", repo);
        // Troca de plano do SuperAdmin também atualiza os códigos de entitlement.
        Assert.Contains("contracted_plan_code=@planCode, effective_plan_code=@planCode", repo);
        // Pagamento aprovado encerra o gate de trial.
        Assert.Contains("subscription_status='Active'", repo);

        var view = Read("src", "HabitFlow.Web", "Views", "SuperAdmin", "Users", "Index.cshtml");
        Assert.Contains("/superadmin/users/@u.Id/@(u.AccountStatus == \"Blocked\" ? \"unblock\" : \"block\")", view);
        Assert.Contains("AntiForgeryToken", view);
    }

    [Fact]
    public void Plan_switch_by_user_downgrades_now_and_honestly_reports_checkout()
    {
        var controller = Read("src", "HabitFlow.Web", "Controllers", "AccountPlanController.cs");
        // Downgrade imediato no cliente + cancelamento da assinatura ativa.
        Assert.Contains("ChangePlanAsync", controller);
        Assert.Contains("CancelSubscriptionAsync", controller);
        // Upgrade usa o checkout real e nunca simula pagamento: mensagem honesta quando indisponível.
        Assert.Contains("StartCheckoutAsync", controller);
        Assert.Contains("payment.checkout_unavailable", controller);
        Assert.Contains("recursos pagos ficam travados até a confirmação", controller);
    }

    [Fact]
    public async Task Creating_a_goal_while_blocked_explains_the_expired_trial()
    {
        var catalog = new StubPlanCatalog(PlanCodes.Blocked);
        var service = new GoalService(new GoalRepoMock(), new FeatureAccessService(new PlanEntitlementService(catalog)));

        var result = await service.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), "Meta", null, "ActiveDays", 10, DateOnly.FromDateTime(DateTime.UtcNow), null);

        Assert.True(result.IsFailure);
        Assert.Equal("plan.trial_expired", result.Error.Code);
        Assert.Contains("R$ 10,00/mês", result.Error.Message);
    }

    [Fact]
    public async Task Goal_creation_works_when_the_plan_allows_it()
    {
        var catalog = new StubPlanCatalog(PlanCodes.Ritmo);
        var service = new GoalService(new GoalRepoMock(), new FeatureAccessService(new PlanEntitlementService(catalog)));

        var result = await service.CreateAsync(Guid.NewGuid(), Guid.NewGuid(), "Meta", null, "ActiveDays", 10, DateOnly.FromDateTime(DateTime.UtcNow), null);

        Assert.True(result.IsSuccess);
    }

    private sealed class StubPlanCatalog(string effectivePlan) : IPlanCatalogRepository
    {
        public Task<IReadOnlyList<PublicPlan>> GetPublicCatalogAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PublicPlan>>([]);

        public Task<ClientPlanAccess?> GetClientAccessAsync(Guid clientId, CancellationToken ct = default) =>
            Task.FromResult<ClientPlanAccess?>(new(clientId, effectivePlan, effectivePlan, "Free", null));

        public Task<Guid?> GetClientIdForUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult<Guid?>(Guid.NewGuid());

        public Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetFeaturesAsync(string planCode, CancellationToken ct = default)
        {
            var blocked = planCode.Equals(PlanCodes.Blocked, StringComparison.OrdinalIgnoreCase);
            IReadOnlyDictionary<string, PlanFeatureValue> features = new Dictionary<string, PlanFeatureValue>
            {
                [PlanFeatureCodes.ActiveGoalsLimit] = new(PlanFeatureCodes.ActiveGoalsLimit, "Objetivos ativos", "Integer", null, blocked ? 0 : -1, null)
            };
            return Task.FromResult(features);
        }

        public Task<bool> IsCheckoutEligibleAsync(string planCode, string billingCycle, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<PlanIntegrityCatalogItem>> GetIntegrityCatalogAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<PlanIntegrityCatalogItem>>([]);
    }
}
