using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HabitFlow.Tests;

public sealed class LayoutShellFunctionalTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public LayoutShellFunctionalTests(WebApplicationFactory<Program> factory) =>
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Theory]
    [InlineData("/")]
    [InlineData("/demo")]
    [InlineData("/habit-library")]
    [InlineData("/plans")]
    [InlineData("/help")]
    [InlineData("/login")]
    [InlineData("/register")]
    public async Task Public_routes_render_the_public_shell(string route)
    {
        using var response = await _client.GetAsync(route);
        var html = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        Assert.Contains("data-navigation-context=\"public\"", html, StringComparison.Ordinal);
        Assert.Contains("data-footer-context=\"public\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("data-navigation-variant=\"PlatformSidebar\"", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/habits")]
    [InlineData("/goals")]
    [InlineData("/progress/calendar")]
    [InlineData("/reports")]
    [InlineData("/profile")]
    [InlineData("/account/plan")]
    [InlineData("/billing")]
    [InlineData("/support")]
    [InlineData("/superadmin")]
    [InlineData("/superadmin/clients")]
    [InlineData("/superadmin/users")]
    [InlineData("/superadmin/plans")]
    [InlineData("/superadmin/subscriptions")]
    [InlineData("/superadmin/payments")]
    [InlineData("/superadmin/overdue")]
    [InlineData("/superadmin/support")]
    [InlineData("/superadmin/audit")]
    [InlineData("/superadmin/system-health")]
    public async Task Protected_navigation_routes_do_not_return_not_found(string route)
    {
        using var response = await _client.GetAsync(route);
        Assert.NotEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }
}
