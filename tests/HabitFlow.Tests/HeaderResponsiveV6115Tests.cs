using System.Security.Claims;
using HabitFlow.Web.Models;
using HabitFlow.Web.Services;
using Xunit;

namespace HabitFlow.Tests;

public sealed class HeaderResponsiveV6115Tests
{
    [Fact]
    public async Task More_is_last_and_contains_progress_for_authenticated_navigation()
    {
        var navigation = new NavigationService();
        var service = new HeaderNavigationService(navigation, new ActiveNavigationMatcher());
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user")], "test"));
        var model = await service.BuildAsync(NavigationContext.Personal, user, "/dashboard", CancellationToken.None);
        Assert.Equal(["today", "my-day", "habits", "goals"], model.Primary.Select(item => item.Code));
        Assert.Single(model.Secondary);
        Assert.Equal("Mais", model.Secondary[0].Label);
        Assert.Contains(model.Secondary[0].Items, item => item.Code == "progress");
        Assert.Contains(model.Secondary[0].Items, item => item.Code == "habits");
        Assert.Contains(model.Secondary[0].Items, item => item.Code == "goals");
    }

    [Fact]
    public void Responsive_styles_define_all_release_boundaries()
    {
        var css = File.ReadAllText(Path.Combine(RepositoryRootLocator.Root, "src", "HabitFlow.Web", "wwwroot", "css", "app-header-v2.css"));
        foreach (var boundary in new[] { "1440px", "1280px", "1279px", "1024px", "1023px", "767px", "359px", "319px", "239px" })
            Assert.Contains(boundary, css);
        Assert.Contains("grid-template-columns", css);
        Assert.Contains("text-overflow:ellipsis", css);
    }
}
