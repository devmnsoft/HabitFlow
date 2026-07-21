using System.Security.Claims;
using HabitFlow.Domain;

namespace HabitFlow.Web.Middleware;

public sealed class AccountStatusMiddleware(RequestDelegate next, ILogger<AccountStatusMiddleware> logger)
{
    private static readonly string[] AllowedWhenRestricted = ["/logout", "/auth/logout", "/profile", "/support", "/health", "/css", "/js", "/lib"];
    private static readonly string[] RestrictedAreas = ["/dashboard", "/habits", "/progress", "/reports"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var status = context.User.FindFirstValue("account_status") ?? AccountStatus.Active.ToString();
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";
            if ((status == AccountStatus.Blocked.ToString() || status == AccountStatus.Suspended.ToString() || status == AccountStatus.DeletedPending.ToString())
                && RestrictedAreas.Any(path.StartsWith) && !AllowedWhenRestricted.Any(path.StartsWith))
            {
                logger.LogWarning("Usuário com status {Status} tentou acessar {Path}", status, path);
                context.Response.Redirect("/support?restricted=" + Uri.EscapeDataString(status));
                return;
            }
        }
        await next(context);
    }
}
