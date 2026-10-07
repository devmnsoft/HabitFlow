namespace HabitFlow.Web.Middleware;

public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private const string Policy = "default-src 'self'; script-src 'self' https://cdn.jsdelivr.net 'unsafe-inline'; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; font-src 'self' data: https://cdn.jsdelivr.net; img-src 'self' data: https:; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; frame-src 'none'";

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=()";
        headers["Content-Security-Policy"] = Policy;
        headers.Remove("X-Powered-By");
        await next(context);
    }
}
