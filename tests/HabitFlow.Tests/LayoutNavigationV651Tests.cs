using System.Security.Claims;
using HabitFlow.Web.Models;
using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace HabitFlow.Tests;

public sealed class LayoutNavigationV651Tests
{
    private readonly LayoutContextResolver _resolver = new();

    [Theory]
    [InlineData("Home", "/", false, NavigationContext.Public)]
    [InlineData("Auth", "/login", false, NavigationContext.Public)]
    [InlineData("Dashboard", "/dashboard", true, NavigationContext.Personal)]
    [InlineData("HabitLibrary", "/habit-library", true, NavigationContext.Personal)]
    [InlineData("Profile", "/profile", true, NavigationContext.Account)]
    [InlineData("Billing", "/billing", true, NavigationContext.Account)]
    [InlineData("SuperAdmin", "/superadmin", true, NavigationContext.Platform)]
    [InlineData("AdminSettings", "/admin/settings", true, NavigationContext.Platform)]
    public void Resolves_context_from_identity_controller_and_path(
        string controller,
        string path,
        bool authenticated,
        NavigationContext expected)
    {
        var httpContext = Context(path, authenticated);
        var routeData = new RouteData();
        routeData.Values["controller"] = controller;

        Assert.Equal(expected, _resolver.Resolve(httpContext, routeData, ViewData()));
    }

    [Fact]
    public void Explicit_view_data_override_has_priority()
    {
        var viewData = ViewData();
        viewData["NavigationContext"] = NavigationContext.Account;

        Assert.Equal(NavigationContext.Account, _resolver.Resolve(Context("/", false), new RouteData(), viewData));
    }

    [Fact]
    public void Navigation_separates_enabled_and_current_state()
    {
        var item = NavigationService.Definitions.Single(item => item.Code == "home");
        var current = new NavigationService().Get(NavigationContext.Public, Context("/", false).User, "/").Single(item => item.Code == "home");

        Assert.True(item.IsEnabled);
        Assert.False(item.IsCurrent);
        Assert.True(current.IsCurrent);
    }

    [Fact]
    public void Every_navigation_icon_exists_in_the_catalog() =>
        Assert.All(NavigationService.Definitions, item => Assert.True(NavigationIconCatalog.Contains(item.Icon), $"Ícone ausente: {item.Icon}"));

    [Fact]
    public void Menu_urls_are_real_paths_and_variants_are_complete()
    {
        Assert.All(NavigationService.Definitions, item => Assert.StartsWith("/", item.Url));
        Assert.DoesNotContain(NavigationService.Definitions, item => item.Url.Contains('#'));
        Assert.Equal(6, Enum.GetValues<NavigationVariant>().Length);
    }

    private static DefaultHttpContext Context(string path, bool authenticated)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            authenticated ? [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())] : [],
            authenticated ? "tests" : null));
        return context;
    }

    private static ViewDataDictionary ViewData() => new(new EmptyModelMetadataProvider(), new ModelStateDictionary());
}
