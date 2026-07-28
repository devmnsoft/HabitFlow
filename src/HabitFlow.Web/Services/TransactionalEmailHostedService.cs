using HabitFlow.Application;
using HabitFlow.Infrastructure;
using Microsoft.Extensions.Options;

namespace HabitFlow.Web.Services;

public sealed class TransactionalEmailHostedService(IServiceScopeFactory scopes, IOptions<EmailOptions> options,
    ILogger<TransactionalEmailHostedService> logger) : BackgroundService
{
    public bool IsActive { get; private set; }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IsActive = true;
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (options.Value.Enabled)
                {
                    using var scope = scopes.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<TransactionalEmailProcessor>().ProcessAsync(stoppingToken);
                }
                await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        catch (Exception ex) { logger.LogError(ex, "Transactional email worker stopped unexpectedly"); }
        finally { IsActive = false; }
    }
}
