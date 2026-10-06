using HabitFlow.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class BillingStatusJob(IBillingStatusRepository repository, IAuditRepository audit, IConfiguration configuration, ILogger<BillingStatusJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var graceDays = configuration.GetValue("BillingJobs:GracePeriodDays", 3);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var overdue = await repository.MarkOverdueInvoicesAsync(today, ct);
        var blocked = await repository.BlockBenefitsAfterGracePeriodAsync(today, graceDays, ct);
        var expiredTrials = await repository.ExpireTrialsAsync(ct);
        var syncedUsers = await repository.SyncUsersWithExpiredTrialsAsync(ct);
        await audit.AddSystemAsync(new SystemAuditLog(Guid.NewGuid(), null, null, AuditSeverity.Info, "billing-job", "BillingStatusJobExecuted", $"Job de cobrança executado: {overdue} faturas vencidas, {blocked} clientes bloqueados, {expiredTrials} trials expirados, {syncedUsers} usuários sincronizados para Free.", null, null, null, DateTime.UtcNow, false), ct);
        logger.LogInformation("BillingStatusJob executed. Overdue={Overdue} Blocked={Blocked} ExpiredTrials={ExpiredTrials} SyncedUsers={SyncedUsers}", overdue, blocked, expiredTrials, syncedUsers);
    }
}
