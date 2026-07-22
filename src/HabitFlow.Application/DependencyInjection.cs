using HabitFlow.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace HabitFlow.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddHabitFlowApplication(this IServiceCollection services)
    {
        services.AddSingleton<LogSanitizer>();
        services.AddSingleton<ProtocolGenerator>();
        services.AddSingleton<WhatsAppValidator>();
        services.AddSingleton<IUserFacingErrorMapper, UserFacingErrorMapper>();
        services.AddSingleton<FeedbackMapper>();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<HabitPolicy>();
        services.AddScoped<ProgressService>();
        services.AddScoped<AdminAuthorizationService>();
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<ClientService>();
        services.AddScoped<HabitService>();
        services.AddSingleton<HabitLibraryFallbackProvider>();
        services.AddScoped<HabitLibraryService>();
        services.AddScoped<GuidedJourneyService>();
        services.AddScoped<HabitScheduleService>();
        services.AddScoped<NotificationService>();
        services.AddScoped<ReportService>();
        services.AddScoped<OnboardingService>();
        services.AddScoped<AuditService>();
        services.AddScoped<AdminAuditService>();
        services.AddScoped<SettingsService>();
        services.AddScoped<SupportService>();
        services.AddScoped<ProfileService>();
        services.AddScoped<UserUiPreferenceService>();
        services.AddScoped<AdminService>();
        services.AddScoped<AdminDashboardService>();
        services.AddScoped<AdminUserService>();
        services.AddScoped<AdminMetricsService>();
        services.AddScoped<AdminSupportService>();
        services.AddScoped<AdminLgpdService>();
        services.AddScoped<AdminAuditQueryService>();
        services.AddScoped<AdminExportService>();
        services.AddScoped<AdminBillingService>();
        services.AddScoped<AdminRiskService>();
        services.AddScoped<DatabaseDiagnosticsService>();
        services.AddScoped<WhatsAppService>();
        services.AddScoped<LgpdService>();
        services.AddScoped<BillingService>();
        services.AddSingleton<PaymentMetadataSanitizer>();
        services.AddScoped<PlanService>();
        services.AddScoped<SubscriptionService>();
        services.AddScoped<PremiumAccessService>();
        services.AddScoped<PaymentCheckoutService>();
        services.AddScoped<PaymentWebhookService>();
        services.AddScoped<PaymentAuditService>();
        services.AddScoped<FinancialDashboardService>();
        services.AddHttpClient<IPaymentProviderService, MercadoPagoService>();
        services.AddHttpClient<TelegramService>();
        return services;
    }
}
