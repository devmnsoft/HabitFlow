using HabitFlow.Application;
using HabitFlow.Domain;
using Xunit;

namespace HabitFlow.Tests;

public sealed class V6121SearchPlanIntegrityTests
{
    private static readonly string Root = RepositoryRootLocator.Root;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact] public void Global_search_supports_both_button_contracts_and_shortcuts()
    {
        var js = Read("src/HabitFlow.Web/wwwroot/js/global-search.js");
        Assert.Contains("[data-search-open], [data-global-search-open]", js);
        Assert.Contains("event.ctrlKey || event.metaKey", js);
        Assert.Contains("event.key.toLowerCase() === 'k'", js);
        Assert.DoesNotContain("innerHTML", js);
    }

    [Fact] public void Public_layout_has_neither_app_search_button_nor_modal()
    {
        Assert.Contains("Model.Context != HabitFlow.Web.Models.NavigationContext.Public", Read("src/HabitFlow.Web/Views/Shared/Partials/Header/_HeaderSearchButton.cshtml"));
        Assert.Contains("navigationContext != NavigationContext.Public", Read("src/HabitFlow.Web/Views/Shared/_Layout.cshtml"));
        Assert.DoesNotContain("HeaderSearchButton", Read("src/HabitFlow.Web/Views/Shared/Partials/Header/_PublicHeader.cshtml"));
    }

    [Fact] public void Plan_integrity_detects_a_partial_public_promise()
    {
        var service = new PlanIntegrityService(new FakeCatalog(), new PlanFeatureImplementationVerifier());
        var report = service.Audit([new("ritmo", "Ritmo", true, true, "Available", "Monthly", 19.90m,
            "advanced_reports", "Relatórios avançados", "Partial", false, true, null, null)]);
        Assert.False(report.IsValid);
        Assert.Contains(report.Issues, x => x.Code == "feature.not_market_ready");
    }

    [Fact] public void Checkout_query_requires_available_sellable_paid_plan_and_selected_active_cycle()
    {
        var repository = Read("src/HabitFlow.Infrastructure/Repositories/PlanCatalogRepository.cs");
        Assert.Contains("p.code <> 'free'", repository);
        Assert.Contains("p.sales_status='Available'", repository);
        Assert.Contains("pp.billing_cycle=@billingCycle", repository);
        Assert.Contains("pp.is_active", repository);
        Assert.Contains("implementation_status <> 'Implemented'", repository);
    }

    private sealed class FakeCatalog : IPlanCatalogRepository
    {
        public Task<IReadOnlyList<PlanIntegrityCatalogItem>> GetIntegrityCatalogAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PlanIntegrityCatalogItem>>([]);
        public Task<IReadOnlyList<PublicPlan>> GetPublicCatalogAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PublicPlan>>([]);
        public Task<ClientPlanAccess?> GetClientAccessAsync(Guid clientId, CancellationToken ct = default) => Task.FromResult<ClientPlanAccess?>(null);
        public Task<Guid?> GetClientIdForUserAsync(Guid userId, CancellationToken ct = default) => Task.FromResult<Guid?>(null);
        public Task<IReadOnlyDictionary<string, PlanFeatureValue>> GetFeaturesAsync(string planCode, CancellationToken ct = default) => Task.FromResult<IReadOnlyDictionary<string, PlanFeatureValue>>(new Dictionary<string, PlanFeatureValue>());
        public Task<bool> IsCheckoutEligibleAsync(string planCode, string billingCycle, CancellationToken ct = default) => Task.FromResult(false);
    }
}
