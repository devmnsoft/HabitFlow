using System.Text.RegularExpressions;
using Xunit;

namespace HabitFlow.Tests;

public sealed class PublicPagesV6105Tests
{
    private static readonly string Root = RepositoryRootLocator.Find();
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact] public void Public_catalog_enforces_sellability_price_and_implemented_features()
    {
        var sql = Read("src/HabitFlow.Infrastructure/Repositories/PlanCatalogRepository.cs");
        Assert.Contains("p.code = 'free'", sql); Assert.Contains("p.is_sellable", sql); Assert.Contains("p.sales_status = 'Available'", sql);
        Assert.Contains("pp.currency='BRL'", sql); Assert.Contains("pp.amount > 0", sql); Assert.Contains("implementation_status <> 'Implemented'", sql);
    }

    [Fact] public void Privacy_fallback_is_complete_and_actionable()
    {
        var view = Read("src/HabitFlow.Web/Views/Legal/Privacy.cshtml");
        foreach (var section in new[] { "Quais dados usamos", "Por que usamos seus dados", "Fornecedores e compartilhamento", "Seus direitos", "Como solicitar", "/account/privacy", "/legal/cookies" }) Assert.Contains(section, view);
        Assert.DoesNotContain("DPO", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] public void Plans_are_rendered_from_commercial_view_model()
    {
        var view = Read("src/HabitFlow.Web/Views/Plans/Index.cshtml");
        Assert.Contains("PlanLandingPageViewModel", view); Assert.DoesNotContain("IReadOnlyList<HabitFlow.Domain.PublicPlan>", view);
        Assert.Contains("_CommercialPlanCard", view); Assert.Contains("_PlanComparisonTable", view); Assert.Contains("_PlanFaq", view);
    }

    [Fact] public void Navigation_separates_public_policy_from_account_center()
    {
        var navigation = Read("src/HabitFlow.Web/Services/NavigationService.cs");
        Assert.Contains("\"public-privacy\"", navigation); Assert.Contains("Url: \"/account/privacy\"", navigation);
        var layout = Read("src/HabitFlow.Web/Views/Shared/_Layout.cshtml");
        Assert.Contains("/legal/cookies", layout); Assert.Contains("/account/security", layout); Assert.Contains("/account/plan/usage", layout);
    }

    [Fact] public void Informational_dialogs_are_safe_and_accessible()
    {
        foreach (var asset in new[] { "src/HabitFlow.Web/wwwroot/js/legal-pages.js", "src/HabitFlow.Web/wwwroot/js/plans-premium.js" })
        { var js=Read(asset); Assert.Contains("createElement",js); Assert.Contains("aria-describedby",js); Assert.DoesNotContain("innerHTML",js); Assert.DoesNotMatch(new Regex(@"\b(alert|confirm|prompt)\s*\("),js); }
    }
}
