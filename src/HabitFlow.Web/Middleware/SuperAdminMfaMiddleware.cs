using System.Security.Claims;
using HabitFlow.Domain;

namespace HabitFlow.Web.Middleware;

public sealed class SuperAdminMfaMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserMfaRepository repository)
    {
        if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole(nameof(UserRole.SuperAdmin)) &&
            context.Request.Path.StartsWithSegments("/superadmin", StringComparison.OrdinalIgnoreCase))
        {
            var userId = Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : Guid.Empty;
            var clientId = Guid.TryParse(context.User.FindFirstValue("client_id"), out var tenant) ? tenant : (Guid?)null;
            var setting = await repository.GetAsync(userId, clientId, context.RequestAborted);
            var target = setting?.IsEnabled == true ? "/account/security/mfa/challenge" : "/account/security/mfa";
            if (setting?.IsEnabled != true || !context.User.HasClaim("mfa_verified", "true"))
            {
                context.Response.Redirect($"{target}?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
                return;
            }
        }
        await next(context);
    }
}
