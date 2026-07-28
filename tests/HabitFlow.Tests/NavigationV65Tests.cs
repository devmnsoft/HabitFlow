using System.Security.Claims;
using HabitFlow.Web.Models;
using HabitFlow.Web.Services;
using Xunit;

namespace HabitFlow.Tests;

public sealed class NavigationV65Tests
{
    private readonly NavigationService _navigation = new();

    [Fact]
    public void Authenticated_person_has_visible_plan_access_in_account_context()
    {
        var user = Principal(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var items = _navigation.Get(NavigationContext.Account, user, "/account/plan");

        var plan = Assert.Single(items, item => item.Code == "my-plan");
        Assert.Equal("Meu plano", plan.Label);
        Assert.True(plan.IsActive);
    }

    [Fact]
    public async Task Required_feature_is_evaluated_before_a_link_is_rendered()
    {
        var navigation = new NavigationService(new DenyFeatureEvaluator());
        var user = Principal(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var items = await navigation.GetAsync(NavigationContext.Personal, user, "/reports");

        Assert.DoesNotContain(items, item => item.Code == "reports");
        Assert.Contains(items, item => item.Code == "today");
    }

    [Fact]
    public async Task My_plan_remains_visible_for_an_authenticated_person()
    {
        var navigation = new NavigationService(new DenyFeatureEvaluator());
        var user = Principal(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        var items = await navigation.GetAsync(NavigationContext.Account, user, "/account/plan");

        Assert.Contains(items, item => item.Code == "my-plan" && item.IsActive);
    }

    [Fact]
    public void Platform_navigation_is_hidden_without_platform_permission()
    {
        var items = _navigation.Get(NavigationContext.Platform, Principal(), "/superadmin");

        Assert.Empty(items);
    }

    [Fact]
    public void Platform_permission_reveals_platform_navigation()
    {
        var user = Principal(new Claim("permission", "platform.view"));

        var items = _navigation.Get(NavigationContext.Platform, user, "/superadmin");

        Assert.Contains(items, item => item.Code == "platform" && item.IsActive);
        Assert.DoesNotContain(items, item => item.Code == "platform-payments");
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, claims.Length > 0 ? "tests" : null));

    private sealed class DenyFeatureEvaluator : INavigationAccessEvaluator
    {
        public Task<bool> CanAccessAsync(ClaimsPrincipal user, string? permission, string? feature, CancellationToken cancellationToken) =>
            Task.FromResult(feature is null && permission is null);
    }
}
