using System.Security.Claims;

namespace HabitFlow.Web.Middleware;

public sealed class ClientBindingMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedPrefixes = ["/logout", "/login", "/register", "/admin/onboarding/recover-client", "/css", "/js", "/lib", "/images", "/health"];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (AllowedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context);
            return;
        }

        var user = context.User;
        var requiresClient = user.Identity?.IsAuthenticated == true && !user.IsInRole("SuperAdmin");
        var hasClient = Guid.TryParse(user.FindFirstValue("client_id"), out _);
        if (requiresClient && !hasClient)
        {
            context.Response.Redirect("/admin/onboarding/recover-client");
            return;
        }

        await next(context);
    }
}
