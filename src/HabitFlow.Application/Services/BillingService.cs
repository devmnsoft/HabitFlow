using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class BillingService(IBillingRepository repo, ILogger<BillingService> logger)
{
    public async Task<Result> AddAsync(BillingEvent billingEvent, CancellationToken ct = default)
    {
        try { await repo.AddAsync(billingEvent, ct); return Result.Success(); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao registrar evento de billing {EventType}", billingEvent.EventType); return Result.Failure("billing.add_error", "Não foi possível registrar o evento financeiro."); }
    }
}
