using HabitFlow.Web.Models;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace HabitFlow.Web.Services;

public sealed class LayoutContextResolver
{
    private static readonly HashSet<string> PublicControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Home", "Demo", "Auth", "Plans", "Help", "Legal", "Invites", "HabitLibrary"
    };

    private static readonly HashSet<string> AccountControllers = new(StringComparer.OrdinalIgnoreCase)
    {
        "Account", "Billing", "Profile", "ClientUsers", "Invites", "Support"
    };

    public NavigationContext Resolve(
        HttpContext httpContext,
        RouteData routeData,
        ViewDataDictionary viewData)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(routeData);
        ArgumentNullException.ThrowIfNull(viewData);

        if (viewData["NavigationContext"] is NavigationContext explicitContext)
        {
            return explicitContext;
        }

        var controller = routeData.Values["controller"]?.ToString() ?? string.Empty;
        var path = httpContext.Request.Path.Value ?? string.Empty;

        if (PublicControllers.Contains(controller))
        {
            return controller.Equals("HabitLibrary", StringComparison.OrdinalIgnoreCase) &&
                   httpContext.User.Identity?.IsAuthenticated == true
                ? NavigationContext.Personal
                : NavigationContext.Public;
        }

        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return NavigationContext.Public;
        }

        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/superadmin", StringComparison.OrdinalIgnoreCase) ||
            controller.StartsWith("Admin", StringComparison.OrdinalIgnoreCase) ||
            controller.StartsWith("SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            return NavigationContext.Platform;
        }

        if (AccountControllers.Contains(controller))
        {
            return NavigationContext.Account;
        }

        return NavigationContext.Personal;
    }
}
