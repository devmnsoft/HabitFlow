using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Extensions.Options;

namespace HabitFlow.Web.Services;

public sealed class ReminderDispatchHostedService(IServiceScopeFactory scopes,
    IOptions<ReminderDispatchOptions> configured, ILogger<ReminderDispatchHostedService> logger) : BackgroundService
{
    private readonly string workerId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = configured.Value;
        if (!options.Enabled) return;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(options.IntervalSeconds));
        do
        {
            var started = TimeProvider.System.GetUtcNow();
            ReminderDispatchResult? result = null;
            try
            {
                using var scope = scopes.CreateScope();
                result = await scope.ServiceProvider.GetRequiredService<ReminderDispatchProcessor>()
                    .ProcessAsync(workerId, options, stoppingToken);
                scope.ServiceProvider.GetRequiredService<ReminderDispatchHealthService>().Record(started, result, true);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception)
            {
                logger.LogError(exception, "Reminder dispatch cycle failed for worker {WorkerId}", workerId);
                using var scope = scopes.CreateScope();
                scope.ServiceProvider.GetRequiredService<ReminderDispatchHealthService>().Record(started, result, false);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
