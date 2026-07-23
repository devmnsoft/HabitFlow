using HabitFlow.Application;

namespace HabitFlow.Web.Services;

public sealed class BillingStatusHostedService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<BillingStatusHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("BillingJobs:Enabled", false))
        {
            logger.LogInformation("BillingStatusHostedService disabled by configuration.");
            return;
        }

        var interval = TimeSpan.FromMinutes(configuration.GetValue("BillingJobs:IntervalMinutes", 360));
        using var timer = new PeriodicTimer(interval);
        do
        {
            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<BillingStatusJob>().RunAsync(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
