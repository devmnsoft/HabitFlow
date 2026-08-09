using System.Security.Claims;
using HabitFlow.Web.Models;
using HabitFlow.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace HabitFlow.Web.ViewComponents;

public sealed class AppHeaderViewComponent(
    LayoutContextResolver contextResolver,
    HeaderNavigationService navigation,
    HeaderQuickActionService quickActions,
    HeaderActionService actions,
    NavigationService legacyNavigation) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var principal = UserClaimsPrincipal;
        var authenticated = principal.Identity?.IsAuthenticated == true;
        var context = contextResolver.Resolve(HttpContext, ViewContext.RouteData, ViewData);
        var name = principal.FindFirst(ClaimTypes.Name)?.Value ?? principal.Identity?.Name ?? "Visitante";
        var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        var model = new AppHeaderViewModel(authenticated, context, name, email,
            name.Length > 0 ? name[..1].ToUpperInvariant() : "H",
            HttpContext.Request.Path.Value ?? "/",
            actions.ResolvePlanName(principal), legacyNavigation.HasPlatformAccess(principal), actions.HasBillingAccess(principal),
            await navigation.BuildAsync(context, principal, HttpContext.Request.Path, HttpContext.RequestAborted),
            quickActions.Build(principal), new(authenticated && context == NavigationContext.Personal));
        return View(model);
    }
}
