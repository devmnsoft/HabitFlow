namespace HabitFlow.Tests;

public sealed class PlansV6174Tests
{
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Fact]
    public void Plans_page_is_public_catalog_driven_and_has_real_checkout_states()
    {
        var controller = Read("src/HabitFlow.Web/Controllers/PlansController.cs");
        var service = Read("src/HabitFlow.Web/Services/PlanLandingPageService.cs");
        var card = Read("src/HabitFlow.Web/Views/Plans/Partials/_CommercialPlanCard.cshtml");
        Assert.Contains("[AllowAnonymous]", controller);
        Assert.Contains("GetPublicCatalogAsync", service);
        Assert.Contains("/billing/checkout", card);
        Assert.Contains("Entrar para assinar", card);
        Assert.DoesNotContain("checkout?success=true", card, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Plans_page_exposes_comparison_faq_and_honest_roadmap_labels()
    {
        var index = Read("src/HabitFlow.Web/Views/Plans/Index.cshtml");
        var comparison = Read("src/HabitFlow.Web/Views/Plans/Partials/_PlanComparisonTable.cshtml");
        var service = Read("src/HabitFlow.Web/Services/PlanLandingPageService.cs");
        Assert.Contains("_PlanComparisonTable", index);
        Assert.Contains("Premium mensal", comparison);
        Assert.Contains("Premium anual", comparison);
        Assert.Contains("Posso começar grátis?", service);
        Assert.Contains("Em breve", service);
        Assert.Contains("Exportação PDF", service);
    }
}
