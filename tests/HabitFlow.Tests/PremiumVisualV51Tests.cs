using Xunit;

namespace HabitFlow.Tests;

public sealed class PremiumVisualV51Tests
{
    [Fact]
    public void Help_center_routes_and_views_exist()
    {
        var controller = File.ReadAllText("../../../../src/HabitFlow.Web/Controllers/HelpController.cs");
        foreach (var route in new[] { "getting-started", "habits", "progress", "reports", "premium", "privacy", "support" })
            Assert.Contains(route, controller);
        Assert.Contains("Como usar o HabitFlow", File.ReadAllText("../../../../src/HabitFlow.Web/Views/Help/Index.cshtml"));
    }

    [Fact]
    public void Mnsoft_logo_uses_safe_view_component_fallback()
    {
        var component = File.ReadAllText("../../../../src/HabitFlow.Web/ViewComponents/MNSOFTLogoViewComponent.cs");
        var view = File.ReadAllText("../../../../src/HabitFlow.Web/Views/Shared/Components/MNSOFTLogo/Default.cshtml");
        Assert.Contains("logo-mnsoft-oficial.png", component);
        Assert.DoesNotContain("Assinatura visual temporária", view);
        Assert.Contains("Consultorias e soluções em TI", view);
        Assert.Contains("mnsoft-official-logo", view);
    }

    [Fact]
    public void Icon_partial_renders_inline_svg()
    {
        var icon = File.ReadAllText("../../../../src/HabitFlow.Web/Views/Shared/Partials/Icons/_Icon.cshtml");
        Assert.Contains("<svg", icon);
        Assert.Contains("currentColor", icon);
        Assert.Contains("aria-hidden", icon);
    }

    [Fact]
    public void Guided_tour_avoids_inner_html()
    {
        var js = File.ReadAllText("../../../../src/HabitFlow.Web/wwwroot/js/guided-tour.js");
        Assert.DoesNotContain("innerHTML", js);
        Assert.Contains("textContent", js);
        Assert.Contains("localStorage", js);
    }

    [Fact]
    public void Plans_page_contains_faq()
    {
        var plans = File.ReadAllText("../../../../src/HabitFlow.Web/Views/Plans/Index.cshtml");
        Assert.Contains("FAQ rápida", plans);
        Assert.Contains("Preciso pagar para começar?", plans);
    }
}
