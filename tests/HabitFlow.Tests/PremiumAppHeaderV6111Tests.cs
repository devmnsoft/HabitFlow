using System.Security.Claims;
using HabitFlow.Web.Models;
using HabitFlow.Web.Services;
using Xunit;

namespace HabitFlow.Tests;

public sealed class PremiumAppHeaderV6111Tests
{
    [Theory]
    [InlineData("/habits/4dddc", "/habits")]
    [InlineData("/goals/42", "/goals")]
    [InlineData("/progress/day/2026-08-09", "/progress")]
    [InlineData("/reports/weekly", "/reports")]
    [InlineData("/account/security/sessions", "/account/security")]
    public void Matcher_keeps_parent_active_for_child_routes(string path, string target) =>
        Assert.True(new ActiveNavigationMatcher().Matches(path, target));

    [Fact]
    public void Matcher_does_not_match_similar_route_prefixes()
    {
        var matcher = new ActiveNavigationMatcher();
        Assert.False(matcher.Matches("/habits-old", "/habits"));
        Assert.False(matcher.Matches("/dashboard", "/"));
    }

    [Fact]
    public void Quick_actions_use_real_product_routes()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.NameIdentifier, "user")], "test"));
        var actions = new HeaderQuickActionService().Build(user);
        Assert.Equal(6, actions.Count);
        Assert.All(actions, action => Assert.StartsWith("/", action.Url));
        Assert.Contains(actions, action => action.Url == "/habits/create");
        Assert.Contains(actions, action => action.Url == "/goals/create");
    }

    [Fact]
    public void Anonymous_users_do_not_receive_product_quick_actions() =>
        Assert.Empty(new HeaderQuickActionService().Build(new ClaimsPrincipal(new ClaimsIdentity())));

    [Fact]
    public void Billing_link_is_permission_gated()
    {
        var service = new HeaderActionService();
        var regular = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.Name, "Ana")], "test"));
        var billing = new ClaimsPrincipal(new ClaimsIdentity([new("permission", "Client.Billing.View")], "test"));
        Assert.False(service.HasBillingAccess(regular));
        Assert.True(service.HasBillingAccess(billing));
    }
}
