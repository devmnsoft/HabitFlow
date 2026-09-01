using HabitFlow.Application;

namespace HabitFlow.Web.Services;

public sealed class SuperAdminBootstrapHostedService(
    IServiceScopeFactory scopes,
    ILogger<SuperAdminBootstrapHostedService> logger) : IHostedService
{
    private static readonly EventId Started = new(619501, "security.superadmin.bootstrap.started");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var correlationId = Guid.NewGuid().ToString("N");
        logger.LogInformation(Started, "security.superadmin.bootstrap.started CorrelationId={CorrelationId} Result={Result}", correlationId, "started");
        var password = Environment.GetEnvironmentVariable("HABITFLOW_SUPERADMIN_INITIAL_PASSWORD");
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("SuperAdmin bootstrap skipped: HABITFLOW_SUPERADMIN_INITIAL_PASSWORD is not configured. CorrelationId={CorrelationId} Result={Result}", correlationId, "skipped_missing_secret");
            return;
        }

        var options = new SuperAdminOptions
        {
            Email = Environment.GetEnvironmentVariable("HABITFLOW_SUPERADMIN_EMAIL") ?? "comercial@mnsoft.com.br",
            Document = Environment.GetEnvironmentVariable("HABITFLOW_SUPERADMIN_DOCUMENT") ?? "18160057000113",
            InitialPassword = password
        };
        await using var scope = scopes.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<SuperAdminBootstrapService>().BootstrapAsync(options, correlationId, cancellationToken);
        logger.LogInformation("SuperAdmin bootstrap completed Event={Event} CorrelationId={CorrelationId} UserId={UserId} Tenant={Tenant} Result={Result}",
            result.Created ? "security.superadmin.bootstrap.created" : result.Updated ? "security.superadmin.bootstrap.updated" : "security.superadmin.bootstrap.skipped_existing",
            correlationId, result.User.Id, "MNSOFT", "success");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
