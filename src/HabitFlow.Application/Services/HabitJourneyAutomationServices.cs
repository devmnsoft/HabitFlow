using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class HabitJourneyService(
    IHabitJourneyRepository journeys,
    PlanEntitlementService entitlements,
    AuditService audit,
    ILogger<HabitJourneyService> logger)
{
    public Task<IReadOnlyList<HabitJourney>> ListAsync(Guid clientId, CancellationToken ct = default) =>
        journeys.ListAvailableAsync(clientId, ct);

    public Task<HabitJourneyDetails?> DetailsAsync(Guid journeyId, Guid clientId, Guid userId, CancellationToken ct = default) =>
        journeys.GetDetailsAsync(journeyId, clientId, userId, ct);

    public async Task<Result<HabitJourneyMember>> JoinAsync(JoinHabitJourneyCommand command, CancellationToken ct = default)
    {
        if (command.ClientId == Guid.Empty || command.UserId == Guid.Empty)
            return Result<HabitJourneyMember>.Failure("journey.tenant_required", "A conta e a pessoa sao obrigatorias.");
        if (!command.Confirmed)
            return Result<HabitJourneyMember>.Failure("journey.confirmation_required", "Confirme a adesao antes de criar a jornada.");

        var details = await journeys.GetDetailsAsync(command.JourneyId, command.ClientId, command.UserId, ct);
        if (details is null || !details.Journey.IsActive)
            return Result<HabitJourneyMember>.Failure("journey.not_found", "Jornada indisponivel.");

        var plan = await entitlements.GetEffectivePlanAsync(command.ClientId, ct);
        if (!HabitTemplateAccess.MeetsMinimumPlan(plan, details.Journey.MinimumPlan))
        {
            await audit.LogAsync("journey.join_blocked", "Plano nao permite aderir a jornada", AuditSeverity.Warning, command.UserId,
                metadata: new { command.ClientId, command.JourneyId, requiredPlan = details.Journey.MinimumPlan, plan, command.CorrelationId }, ct: ct);
            return Result<HabitJourneyMember>.Failure("journey.plan_required", "Seu plano nao inclui esta jornada.");
        }

        var existing = details.Membership ?? await journeys.GetMembershipAsync(command.JourneyId, command.ClientId, command.UserId, ct);
        if (existing is not null)
            return Result<HabitJourneyMember>.Success(existing);

        var member = new HabitJourneyMember(Guid.NewGuid(), command.JourneyId, command.ClientId, command.UserId, "Active", 0, DateTime.UtcNow, null);
        try
        {
            await journeys.JoinAsync(member, ct);
            await audit.LogAsync("journey.joined", "Usuario aderiu a jornada guiada", AuditSeverity.Info, command.UserId,
                metadata: new { command.ClientId, command.JourneyId, command.CorrelationId }, ct: ct);
            return Result<HabitJourneyMember>.Success(member);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao aderir a jornada {JourneyId} para {UserId}", command.JourneyId, command.UserId);
            return Result<HabitJourneyMember>.Failure("journey.join_error", "Nao foi possivel aderir agora.");
        }
    }

    public async Task<Result> LeaveAsync(Guid journeyId, Guid clientId, Guid userId, string correlationId, CancellationToken ct = default)
    {
        await journeys.LeaveAsync(journeyId, clientId, userId, ct);
        await audit.LogAsync("journey.left", "Usuario saiu da jornada sem apagar historico", AuditSeverity.Info, userId,
            metadata: new { clientId, journeyId, correlationId }, ct: ct);
        return Result.Success();
    }
}

public sealed class HabitAutomationService(IHabitAutomationRepository automations, PlanEntitlementService entitlements, AuditService audit)
{
    public Task<IReadOnlyList<HabitAutomation>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default) =>
        automations.ListForUserAsync(clientId, userId, ct);

    public async Task<Result<HabitAutomation>> CreateAsync(Guid clientId, Guid userId, HabitAutomationType type, Guid? habitId, string frequency, CancellationToken ct = default)
    {
        if (!await entitlements.CanUseFeatureAsync(userId, PlanFeatureCodes.RemindersPerHabit, ct) && type is HabitAutomationType.SmartReminder)
            return Result<HabitAutomation>.Failure("automation.plan_required", "Automacoes de lembrete exigem plano com lembretes.");
        var now = DateTime.UtcNow;
        var automation = new HabitAutomation(Guid.NewGuid(), clientId, userId, habitId, type, string.IsNullOrWhiteSpace(frequency) ? "Weekly" : frequency.Trim(),
            HabitAutomationStatus.Active, new TimeOnly(22, 0), new TimeOnly(7, 0), null, null, now, now);
        await automations.CreateAsync(automation, ct);
        await audit.LogAsync("automation.created", "Automacao criada", AuditSeverity.Info, userId,
            metadata: new { clientId, automation.Id, type = type.ToString() }, ct: ct);
        return Result<HabitAutomation>.Success(automation);
    }

    public async Task<Result> PauseAsync(Guid id, Guid clientId, Guid userId, CancellationToken ct = default)
    {
        await automations.UpdateStatusAsync(id, clientId, userId, HabitAutomationStatus.Paused, ct);
        await audit.LogAsync("automation.paused", "Automacao pausada pelo usuario", AuditSeverity.Info, userId, metadata: new { clientId, id }, ct: ct);
        return Result.Success();
    }
}
