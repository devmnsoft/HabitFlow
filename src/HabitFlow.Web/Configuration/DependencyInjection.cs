using HabitFlow.Web.Services;

namespace HabitFlow.Web.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddHabitFlowWeb(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddControllersWithViews();
        services.AddSingleton<IBrandAssetService, BrandAssetService>();
        services.AddHabitFlowAuthentication(configuration, environment);
        return services;
    }
}
