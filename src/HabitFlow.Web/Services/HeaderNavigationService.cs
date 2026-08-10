using System.Security.Claims;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Services;

public sealed class HeaderNavigationService(NavigationService navigation, ActiveNavigationMatcher matcher)
{
    public async Task<HeaderNavigationViewModel> BuildAsync(NavigationContext context, ClaimsPrincipal user, string path, CancellationToken ct)
    {
        var source = await navigation.GetAsync(context, user, path, ct);
        var items = source.Select(x => new HeaderNavigationItem(x.Code, x.Label, x.Description, x.Icon, x.Url, matcher.Matches(path, x.Url))).ToArray();
        var primaryCodes = context == NavigationContext.Public
            ? new[] { "home", "demo", "library", "plans" }
            : new[] { "today", "my-day", "habits", "goals" };
        var primary = items.Where(x => primaryCodes.Contains(x.Code)).ToArray();
        // Compact desktop keeps these destinations in "Mais" after their links
        // are progressively hidden from the horizontal navigation.
        var compactMoreCodes = new[] { "habits", "goals" };
        var secondary = items.Where(x => !primaryCodes.Contains(x.Code) ||
            (context == NavigationContext.Personal && compactMoreCodes.Contains(x.Code))).ToArray();
        IReadOnlyList<HeaderNavigationGroup> groups = secondary.Length == 0 ? [] : [new("Mais", secondary)];
        var bottomCodes = new[] { "today", "my-day", "habits", "progress" };
        var bottom = items.Where(x => bottomCodes.Contains(x.Code)).ToList();
        if (user.Identity?.IsAuthenticated == true)
            bottom.Add(new("account", "Conta", "Sua conta", "profile", "/profile", matcher.Matches(path, "/profile") || path.StartsWith("/account", StringComparison.OrdinalIgnoreCase)));
        return new(primary, groups, bottom);
    }
}
