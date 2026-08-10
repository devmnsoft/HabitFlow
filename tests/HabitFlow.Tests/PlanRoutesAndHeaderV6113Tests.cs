using Xunit;

namespace HabitFlow.Tests;

public sealed class PlanRoutesAndHeaderV6113Tests
{
    private static readonly string Root = RepositoryRootLocator.Root;
    private static string Read(string path) => File.ReadAllText(Path.Combine(Root, path));

    [Theory]
    [InlineData("[HttpGet(\"account/plan\")]")]
    [InlineData("[HttpGet(\"account/plan/usage\")]")]
    [InlineData("[HttpGet(\"account/plan/change/{planCode}\")]")]
    [InlineData("[HttpPost(\"account/plan/change/{planCode}/confirm\")]")]
    public void Account_plan_routes_are_real_controller_actions(string route) =>
        Assert.Contains(route, Read("src/HabitFlow.Web/Controllers/AccountPlanController.cs"));

    [Fact]
    public void Usage_service_scopes_every_count_to_authenticated_client_and_user()
    {
        var service = Read("src/HabitFlow.Web/Services/PlanUsageService.cs");
        Assert.Contains("persistedClientId != clientId", service);
        Assert.Contains("CountActiveAsync(clientId, userId, ct)", service);
        Assert.DoesNotContain("new PlanUsageLimitViewModel(\"Hábitos ativos\", 0", service);
    }

    [Fact]
    public void Header_core_links_point_to_real_routes()
    {
        var userMenu = Read("src/HabitFlow.Web/Views/Shared/Partials/AppHeader/_UserMenu.cshtml");
        var pill = Read("src/HabitFlow.Web/Views/Shared/Partials/AppHeader/_PlanStatusPill.cshtml");
        var quick = Read("src/HabitFlow.Web/Services/HeaderQuickActionService.cs");
        Assert.Contains("/account/plan/usage", pill);
        Assert.Contains("/plans", userMenu);
        Assert.Contains("/habits/create", quick);
        Assert.Contains("/goals/create", quick);
    }

    [Theory]
    [InlineData("/account/plan/usage", "/account/plan")]
    [InlineData("/account/privacy", "/account/privacy")]
    [InlineData("/plans", "/plans")]
    public void Active_matcher_keeps_required_parent_state(string path, string target) =>
        Assert.True(new HabitFlow.Web.Services.ActiveNavigationMatcher().Matches(path, target));
}
