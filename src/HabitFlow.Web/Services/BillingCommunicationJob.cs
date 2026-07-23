using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Web.Services;

public sealed class BillingCommunicationJob(IServiceProvider services, IConfiguration configuration, ILogger<BillingCommunicationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("BillingCommunicationJob:Enabled", false)) return;
        var minutes = Math.Max(15, configuration.GetValue("BillingCommunicationJob:IntervalMinutes", 360));
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunOnceAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(minutes), stoppingToken);
        }
    }

    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var logs = scope.ServiceProvider.GetRequiredService<IJobExecutionLogRepository>();
        var rules = scope.ServiceProvider.GetRequiredService<IBillingCommunicationRuleRepository>();
        var id = await logs.StartAsync(nameof(BillingCommunicationJob), ct);
        try
        {
            var activeRules = await rules.ListActiveAsync(ct);
            logger.LogInformation("BillingCommunicationJob carregou {Count} regras ativas. Processamento de faturas deve usar Dapper e evitar duplicidade por client_id/invoice_id/tipo.", activeRules.Count);
            await logs.FinishAsync(id, "Success", activeRules.Count, null, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha no BillingCommunicationJob");
            await logs.FinishAsync(id, "Failed", 0, "Falha segura no job de comunicação de cobrança.", ct);
        }
    }
}
