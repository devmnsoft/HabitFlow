using HabitFlow.Web.Services;
using HabitFlow.Application;

namespace HabitFlow.Web.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddHabitFlowWeb(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddOptions<SessionSecurityOptions>().Bind(configuration.GetSection("Security:Sessions"))
            .Validate(x => x.LifetimeDays is >= 1 and <= 365 && x.TouchIntervalMinutes is >= 1 and <= 60, "Configuração de sessões inválida.").ValidateOnStart();
        services.AddControllersWithViews();
        services.Configure<EmailOptions>(configuration.GetSection("Email"));
        services.AddOptions<EmailOptions>().Bind(configuration.GetSection("Email")).Validate(options =>
        {
            if (!options.Enabled) return true;
            if (!Uri.TryCreate(options.PasswordReset.PublicBaseUrl, UriKind.Absolute, out var uri)) return false;
            if (!environment.IsDevelopment() && (uri.Scheme != Uri.UriSchemeHttps || uri.IsLoopback)) return false;
            return options.PasswordReset.AllowedBaseUrls.Length == 0 || options.PasswordReset.AllowedBaseUrls.Any(value =>
                string.Equals(value.TrimEnd('/'), options.PasswordReset.PublicBaseUrl.TrimEnd('/'), StringComparison.OrdinalIgnoreCase));
        }, "Email:PasswordReset:PublicBaseUrl deve estar na allowlist e usar HTTPS fora de desenvolvimento.").ValidateOnStart();
        services.AddSingleton<IBrandAssetService, BrandAssetService>();
        services.AddHabitFlowAuthentication(configuration, environment);
        services.AddScoped<ApplicationFeedbackService>();
        services.AddScoped<FeedbackService>();
        services.AddScoped<UserFeedbackService>();
        services.AddScoped<AccountPrivacyService>();
        services.AddScoped<INavigationAccessEvaluator, NavigationAccessEvaluator>();
        services.AddScoped<RequestPlanAccessContext>();
        services.AddScoped<NavigationService>();
        services.AddSingleton<ActiveNavigationMatcher>();
        services.AddScoped<ActiveRouteService>();
        services.AddScoped<HeaderNavigationService>();
        services.AddScoped<HeaderActionService>();
        services.AddScoped<HeaderQuickActionService>();
        services.AddScoped<PlanLandingPageService>();
        services.AddScoped<PlanUsageService>();
        services.AddSingleton<LayoutContextResolver>();
        services.AddHostedService<BillingCommunicationJob>();
        services.AddHostedService<TransactionalEmailHostedService>();
        return services;
    }
}
