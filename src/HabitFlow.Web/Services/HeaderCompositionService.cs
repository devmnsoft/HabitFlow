using System.Security.Claims;
using HabitFlow.Web.Models;

namespace HabitFlow.Web.Services;

public sealed class HeaderCompositionService(
    HeaderNavigationService navigation,
    HeaderQuickActionService quickActions,
    HeaderActionService actions,
    NavigationService legacyNavigation)
{
    public async Task<AppHeaderViewModel> ComposeAsync(HttpContext httpContext, ClaimsPrincipal principal,
        NavigationContext context, CancellationToken cancellationToken)
    {
        var authenticated = principal.Identity?.IsAuthenticated == true;
        var name = principal.FindFirst(ClaimTypes.Name)?.Value ?? principal.Identity?.Name ?? "Visitante";
        var path = httpContext.Request.Path.Value ?? "/";
        return new(authenticated, context, name, principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            name.Length > 0 ? name[..1].ToUpperInvariant() : "H", path, actions.ResolvePlanName(principal),
            legacyNavigation.HasPlatformAccess(principal), actions.HasBillingAccess(principal),
            await navigation.BuildAsync(context, principal, path, cancellationToken),
            quickActions.Build(principal), new(authenticated && context != NavigationContext.Public));
    }
}
