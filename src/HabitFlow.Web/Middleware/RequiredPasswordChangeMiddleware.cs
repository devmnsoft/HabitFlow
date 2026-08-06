namespace HabitFlow.Web.Middleware;

public sealed class RequiredPasswordChangeMiddleware(RequestDelegate next)
{
    private static readonly string[] Allowed = [
        "/account/security/change-required-password", "/logout", "/legal/terms", "/legal/privacy", "/support"
    ];

    public Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var allowed = Allowed.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            || path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/icons/", StringComparison.OrdinalIgnoreCase)
            || path is "/favicon.svg";
        if (context.User.Identity?.IsAuthenticated == true && context.User.HasClaim("must_change_password", "true") && !allowed)
        {
            context.Response.Redirect("/account/security/change-required-password");
            return Task.CompletedTask;
        }
        return next(context);
    }
}
