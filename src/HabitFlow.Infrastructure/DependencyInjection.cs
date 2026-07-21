using Dapper;
using HabitFlow.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HabitFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHabitFlowInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        services.AddScoped<DbConnectionFactory>();
        services.AddScoped<SqlExecutor>();
        services.AddScoped<UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IHabitRepository, HabitRepository>();
        services.AddScoped<IHabitCompletionRepository, HabitCompletionRepository>();
        services.AddScoped<IHabitWeekDayRepository, HabitWeekDayRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserReportRepository, UserReportRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        services.AddScoped<IAdminAuditRepository, AdminAuditRepository>();
        services.AddScoped<ISettingsRepository, SettingsRepository>();
        services.AddScoped<ISupportRepository, SupportRepository>();
        services.AddScoped<ILgpdRepository, LgpdRepository>();
        services.AddScoped<IBillingRepository, BillingRepository>();
        return services;
    }
}
