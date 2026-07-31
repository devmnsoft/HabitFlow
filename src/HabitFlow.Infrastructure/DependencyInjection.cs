using HabitFlow.Application;
using HabitFlow.Domain;
using HabitFlow.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HabitFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHabitFlowInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DapperTypeHandlers.Register();
        services.AddScoped<DbConnectionFactory>();
        services.AddScoped<SqlExecutor>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISuperAdminProvisioningRepository, SuperAdminProvisioningRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<IPasswordResetRequestRepository, PasswordResetRequestRepository>();
        services.AddScoped<ITransactionalEmailOutboxRepository, TransactionalEmailOutboxRepository>();
        services.AddScoped<ITransactionalEmailSender, GmailSmtpEmailSender>();
        services.AddScoped<IUserSessionRevocationService, UserSessionRevocationService>();
        services.AddScoped<TransactionalEmailProcessor>();
        services.AddScoped<IUserInviteRepository, UserInviteRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<IHabitRepository, HabitRepository>();
        services.AddScoped<IUserGoalRepository, UserGoalRepository>();
        services.AddScoped<IGoalProgressRepository, GoalProgressEventRepository>();
        services.AddScoped<IMilestoneRepository, MilestoneRepository>();
        services.AddScoped<IHabitObjectiveRepository, HabitObjectiveRepository>();
        services.AddScoped<IHabitTemplateRepository, HabitTemplateRepository>();
        services.AddScoped<IHabitTemplateFavoriteRepository, HabitTemplateFavoriteRepository>();
        services.AddScoped<IHabitCompletionRepository, HabitCompletionRepository>();
        services.AddScoped<IHabitWeekDayRepository, HabitWeekDayRepository>();
        services.AddScoped<IProgressCalendarRepository, ProgressCalendarRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserReportRepository, UserReportRepository>();
        services.AddScoped<IUserUiPreferenceRepository, UserUiPreferenceRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IAdminAuditRepository, AdminAuditRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ISupportRepository, SupportRepository>();
        services.AddScoped<ILgpdRepository, LgpdRepository>();
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<IBillingStatusRepository, BillingStatusRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IPlanCatalogRepository, PlanCatalogRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPaymentTransactionRepository, PaymentTransactionRepository>();
        services.AddScoped<IPaymentWebhookRepository, PaymentWebhookRepository>();
        services.AddScoped<IPaymentAuditRepository, PaymentAuditRepository>();
        services.AddScoped<IFinancialDashboardRepository, FinancialDashboardRepository>();
        services.AddScoped<IClientOnboardingRepository, ClientOnboardingRepository>();
        services.AddScoped<IClientCommunicationRepository, ClientCommunicationRepository>();
        services.AddScoped<IBillingCommunicationRuleRepository, BillingCommunicationRuleRepository>();
        services.AddScoped<IJobExecutionLogRepository, JobExecutionLogRepository>();
        services.AddScoped<ISuperAdminOperationalRepository, SuperAdminOperationalRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IAdminMetricsRepository, AdminMetricsRepository>();
        services.AddScoped<IAdminBillingRepository, AdminBillingRepository>();
        services.AddScoped<IAdminSupportRepository, AdminSupportRepository>();
        services.AddScoped<IAdminLgpdRepository, AdminLgpdRepository>();
        services.AddScoped<IAdminAuditQueryRepository, AdminAuditQueryRepository>();
        services.AddScoped<IAdminExportRepository, AdminExportRepository>();
        services.AddScoped<IDatabaseDiagnosticsRepository, DatabaseDiagnosticsRepository>();
        return services;
    }
}
