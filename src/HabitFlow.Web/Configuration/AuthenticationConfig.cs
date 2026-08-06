using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using HabitFlow.Domain;
using HabitFlow.Application;

namespace HabitFlow.Web.Configuration;

public static class AuthenticationConfig
{
    public static IServiceCollection AddHabitFlowAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
        {
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
            options.ExpireTimeSpan = TimeSpan.FromHours(configuration.GetValue("Authentication:CookieHours", 8));
            options.Events.OnValidatePrincipal = async context =>
            {
                var idText = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                var versionText = context.Principal?.FindFirstValue("session_version");
                if (!Guid.TryParse(idText, out var id) || !int.TryParse(versionText, out var version)) { context.RejectPrincipal(); return; }
                var user = await context.HttpContext.RequestServices.GetRequiredService<IUserRepository>().GetByIdAsync(id, context.HttpContext.RequestAborted);
                if (user is null || user.SessionVersion != version) { context.RejectPrincipal(); await context.HttpContext.SignOutAsync(); return; }
                var clock = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
                if (!Guid.TryParse(context.Principal?.FindFirstValue("session_id"), out var sessionId)
                    || await context.HttpContext.RequestServices.GetRequiredService<IUserSessionRepository>().GetActiveOwnedAsync(sessionId, id, user.ClientId, clock.GetUtcNow().UtcDateTime, context.HttpContext.RequestAborted) is null)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync();
                }
                else await context.HttpContext.RequestServices.GetRequiredService<UserSessionService>().TouchAsync(sessionId, id, user.ClientId, context.HttpContext.RequestAborted);
            };
        });
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Admin", "SuperAdmin"));
            options.AddPolicy("RequireSuperAdmin", policy => policy.RequireRole("SuperAdmin"));
            options.AddPolicy("RequireClientAccess", policy => policy.RequireAuthenticatedUser());
        });
        return services;
    }
}
