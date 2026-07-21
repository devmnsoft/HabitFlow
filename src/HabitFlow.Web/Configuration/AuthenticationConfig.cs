using Microsoft.AspNetCore.Authentication.Cookies;

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
        });
        services.AddAuthorization();
        return services;
    }
}
