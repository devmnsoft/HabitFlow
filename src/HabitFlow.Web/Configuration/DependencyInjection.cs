using HabitFlow.Web.Services;
using HabitFlow.Application;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace HabitFlow.Web.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddHabitFlowWeb(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddOptions<ReminderDispatchOptions>().Bind(configuration.GetSection("ReminderDispatch"))
            .Validate(x => x.IntervalSeconds is >= 1 and <= 3600 && x.BatchSize is >= 1 and <= 500
                && x.LeaseSeconds is >= 5 and <= 3600 && x.MaxAttempts is >= 1 and <= 20,
                "Configuração do processador de lembretes inválida.").ValidateOnStart();
        services.AddOptions<SessionSecurityOptions>().Bind(configuration.GetSection("Security:Sessions"))
            .Validate(x => x.LifetimeDays is >= 1 and <= 365 && x.TouchIntervalMinutes is >= 1 and <= 60, "Configuração de sessões inválida.").ValidateOnStart();
        // Unsafe browser requests are protected by default. The only exceptions are
        // signed provider webhooks, which explicitly use IgnoreAntiforgeryToken.
        services.AddControllersWithViews(options => options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
        services.AddRateLimiter(options => options.AddFixedWindowLimiter("assistant", limiter =>
        {
            limiter.PermitLimit = 10;
            limiter.Window = TimeSpan.FromMinutes(1);
            limiter.QueueLimit = 0;
            limiter.AutoReplenishment = true;
        }));
        services.AddRateLimiter(options => options.AddFixedWindowLimiter("notification-test", limiter =>
        {
            limiter.PermitLimit = 3; limiter.Window = TimeSpan.FromMinutes(10); limiter.QueueLimit = 0;
        }));
        services.AddOptions<PushNotificationOptions>().Bind(configuration.GetSection("WebPush")).Validate(options =>
            !options.Enabled || (!string.IsNullOrWhiteSpace(options.Subject) && !string.IsNullOrWhiteSpace(options.PublicKey) && !string.IsNullOrWhiteSpace(options.PrivateKey)),
            "WebPush habilitado exige Subject, PublicKey e PrivateKey.").ValidateOnStart();
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
        services.AddScoped<HeaderContextResolver>();
        services.AddScoped<HeaderCompositionService>();
        services.AddScoped<PlanLandingPageService>();
        services.AddScoped<PlanFeatureImplementationVerifier>();
        services.AddScoped<PlanIntegrityService>();
        services.AddScoped<PlanUsageService>();
        services.AddSingleton<LayoutContextResolver>();
        services.AddHostedService<BillingCommunicationJob>();
        services.AddHostedService<TransactionalEmailHostedService>();
        services.AddHostedService<ReminderDispatchHostedService>();
        return services;
    }
}
