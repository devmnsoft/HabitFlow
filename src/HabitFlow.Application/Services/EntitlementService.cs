using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class EntitlementService(IClientRepository clients, AuditService audit, PlanEntitlementService plans, ILogger<EntitlementService> logger)
{
    public async Task<ClientPlan> GetEffectivePlanAsync(Guid userId, CancellationToken ct = default) => (await plans.GetEffectivePlanForUserAsync(userId, ct)) switch { PlanCodes.Ritmo => ClientPlan.Premium, PlanCodes.Team => ClientPlan.Premium, PlanCodes.Evolucao => ClientPlan.Enterprise, PlanCodes.Enterprise => ClientPlan.Enterprise, _ => ClientPlan.Free };
    public async Task<Result<ClientEntitlementsDto>> GetClientEntitlementsAsync(Guid clientId, CancellationToken ct = default)
    {
        var c = await clients.GetByIdAsync(clientId, ct);
        if (c is null) return Result<ClientEntitlementsDto>.Failure("client.not_found", "Cliente não encontrado.");
        var premium = c.BenefitsStatus is ClientBenefitsStatus.PremiumActive or ClientBenefitsStatus.EnterpriseActive;
        return Result<ClientEntitlementsDto>.Success(new(c.Id, c.Plan, c.BenefitsStatus, c.SubscriptionStatus, c.PaymentStatus, AppConstants.FreePlanHabitLimit, premium, premium));
    }
    public Task<bool> CanUsePremiumFeatureAsync(Guid userId, string featureCode, CancellationToken ct = default) => plans.CanUseFeatureAsync(userId, featureCode, ct);
    public Task<bool> CanCreateHabitAsync(Guid userId, int activeHabitsCount, CancellationToken ct = default) => plans.CanCreateHabitAsync(userId, activeHabitsCount, ct);
    public Task<bool> CanAccessAdvancedReportsAsync(Guid userId, CancellationToken ct = default) => plans.CanAccessAdvancedReportsAsync(userId, ct);
    public Task<bool> CanUseHabitLibraryPremiumAsync(Guid userId, CancellationToken ct = default) => plans.CanUseFullLibraryAsync(userId, ct);
    public Task ApplyPaymentStatusAsync(Guid clientId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<Result<Client>> BlockPaidBenefitsAsync(Guid clientId, string reason, User superAdmin, CancellationToken ct = default) => ChangeAsync(clientId, reason, superAdmin, true, ct);
    public Task<Result<Client>> ReleasePaidBenefitsAsync(Guid clientId, string reason, User superAdmin, CancellationToken ct = default) => ChangeAsync(clientId, reason, superAdmin, false, ct);
    private async Task<Result<Client>> ChangeAsync(Guid clientId, string reason, User superAdmin, bool block, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason)) return Result<Client>.Failure("entitlement.reason_required", "Motivo obrigatório.");
        var c = await clients.GetByIdAsync(clientId, ct); if (c is null) return Result<Client>.Failure("client.not_found", "Cliente não encontrado.");
        var previous = c.BenefitsStatus;
        var target = block ? (c.Plan == ClientPlan.Enterprise ? ClientBenefitsStatus.EnterpriseBlocked : ClientBenefitsStatus.PremiumBlocked) : (c.Plan == ClientPlan.Enterprise ? ClientBenefitsStatus.EnterpriseActive : c.Plan == ClientPlan.Premium ? ClientBenefitsStatus.PremiumActive : ClientBenefitsStatus.Free);
        var updated = c with { BenefitsStatus = target, BlockedPaidBenefitsAt = block ? DateTime.UtcNow : null, BlockedPaidBenefitsReason = block ? reason : null, UpdatedAt = DateTime.UtcNow };
        await clients.UpdateAsync(updated, ct);
        logger.LogInformation(new Microsoft.Extensions.Logging.EventId(6217, "billing.manual_adjustment.created"), "billing.manual_adjustment.created ClientId={ClientId} Reason={Reason} TargetStatus={TargetStatus}", clientId, reason, target);
        await audit.LogAsync("billing.manual_adjustment.created", reason, AuditSeverity.Warning, superAdmin.Id, superAdmin.Email, new { clientId, previous, target, block }, ct);
        await audit.LogAsync(block ? "paid_benefits_blocked" : "paid_benefits_released", reason, AuditSeverity.Warning, superAdmin.Id, superAdmin.Email, new { clientId, previous, target }, ct);
        return Result<Client>.Success(updated);
    }
}

public sealed class SuperAdminService(IClientRepository clients)
{
    public async Task<SuperAdminDashboardDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var all = await clients.SearchAsync(null, null, null, 0, 500, null, ct);
        var overdue = all.Count(c => c.PaymentStatus == ClientPaymentStatus.Overdue || c.SubscriptionStatus == ClientSubscriptionStatus.PastDue);
        return new SuperAdminDashboardDto(all.Count, all.Count(c => c.IsActive), all.Count(c => c.Plan == ClientPlan.Free), all.Count(c => c.Plan == ClientPlan.Premium), all.Count(c => c.Plan == ClientPlan.Enterprise), all.Take(10).ToList(), overdue, all.Count >= 500);
    }
}
public sealed record SuperAdminDashboardDto(int TotalClients, int ActiveClients, int FreeClients, int PremiumClients, int EnterpriseClients, IReadOnlyList<Client> AttentionClients, int OverdueClients, bool OverdueCapped);
