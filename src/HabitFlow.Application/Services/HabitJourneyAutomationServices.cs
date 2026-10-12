using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed record JourneyCatalogQuery(string? Search = null, string? Goal = null, string? Category = null, int? MaxDurationDays = null, string? Plan = null);

public sealed class HabitJourneyService(
    IHabitJourneyRepository journeys,
    IHabitRepository habits,
    IHabitWeekDayRepository weekDays,
    PlanEntitlementService entitlements,
    IUnitOfWork unitOfWork,
    AuditService audit,
    UserTimeZoneService timeZone,
    ILogger<HabitJourneyService> logger)
{
    public async Task<IReadOnlyList<HabitJourney>> ListAsync(Guid clientId, JourneyCatalogQuery? query = null, CancellationToken ct = default)
    {
        var list = await journeys.ListAvailableAsync(clientId, ct);
        query ??= new();
        return list.Where(j =>
            (string.IsNullOrWhiteSpace(query.Search) || Contains(j.Name, query.Search) || Contains(j.Description, query.Search) || Contains(j.Goal, query.Search) || j.Tags.Any(t => Contains(t, query.Search))) &&
            (string.IsNullOrWhiteSpace(query.Goal) || Contains(j.Goal, query.Goal)) &&
            (string.IsNullOrWhiteSpace(query.Category) || j.Category.Equals(query.Category.Trim(), StringComparison.OrdinalIgnoreCase)) &&
            (!query.MaxDurationDays.HasValue || j.SuggestedDurationDays <= query.MaxDurationDays.Value) &&
            (string.IsNullOrWhiteSpace(query.Plan) || HabitTemplateAccess.MeetsMinimumPlan(query.Plan, j.MinimumPlan)))
            .OrderByDescending(j => j.IsOfficial).ThenBy(j => j.Category).ThenBy(j => j.Name).ToList();
    }

    public Task<HabitJourneyDetails?> DetailsAsync(Guid journeyId, Guid clientId, Guid userId, CancellationToken ct = default) =>
        journeys.GetDetailsAsync(journeyId, clientId, userId, ct);

    public async Task<Result<HabitJourneyMember>> JoinAsync(JoinHabitJourneyCommand command, CancellationToken ct = default)
    {
        if (command.ClientId == Guid.Empty || command.UserId == Guid.Empty)
            return Result<HabitJourneyMember>.Failure("journey.tenant_required", "A conta e a pessoa sao obrigatorias.");
        if (!command.Confirmed || command.HabitSelections.Count == 0)
            return Result<HabitJourneyMember>.Failure("journey.confirmation_required", "Confirme a adesao antes de criar a jornada.");

        var details = await journeys.GetDetailsAsync(command.JourneyId, command.ClientId, command.UserId, ct);
        if (details is null || !details.Journey.IsActive)
            return Result<HabitJourneyMember>.Failure("journey.not_found", "Jornada indisponivel.");
        var selectedStepIds = command.HabitSelections.Select(x => x.StepId).ToHashSet();
        if (selectedStepIds.Count != command.HabitSelections.Count || !selectedStepIds.IsSubsetOf(details.Steps.Select(x => x.Id).ToHashSet()))
            return Result<HabitJourneyMember>.Failure("journey.steps_invalid", "Revise os habitos selecionados antes de confirmar.");

        var plan = await entitlements.GetEffectivePlanAsync(command.ClientId, ct);
        if (!HabitTemplateAccess.MeetsMinimumPlan(plan, details.Journey.MinimumPlan))
        {
            await audit.LogAsync("journey.join_blocked", "Plano nao permite aderir a jornada", AuditSeverity.Warning, command.UserId,
                metadata: new { command.ClientId, command.JourneyId, requiredPlan = details.Journey.MinimumPlan, plan, command.CorrelationId }, ct: ct);
            return Result<HabitJourneyMember>.Failure("journey.plan_required", "Seu plano nao inclui esta jornada.");
        }

        var existing = details.Membership ?? await journeys.GetMembershipAsync(command.JourneyId, command.ClientId, command.UserId, ct);
        var activeUserHabits = await habits.ListActiveAsync(command.ClientId, command.UserId, ct);
        var guardedHabitNames = activeUserHabits.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var activeHabits = await habits.CountActiveAsync(command.ClientId, command.UserId, ct);
        var alreadyLinkedStepIds = details.LinkedHabits.Select(x => x.StepId).ToHashSet();
        var newHabitCount = command.HabitSelections.Count(x => !x.ExistingHabitId.HasValue && !alreadyLinkedStepIds.Contains(x.StepId));
        for (var i = 0; i < newHabitCount; i++)
            if (!await entitlements.CanCreateHabitAsync(command.UserId, activeHabits + i, ct))
                return Result<HabitJourneyMember>.Failure("journey.habit_limit", "Seu plano nao possui espaco para todos os habitos selecionados.");

        var member = existing ?? new HabitJourneyMember(Guid.NewGuid(), command.JourneyId, command.ClientId, command.UserId, "Active", 0, DateTime.UtcNow, null);
        try
        {
            await unitOfWork.BeginTransactionAsync(ct);
            await journeys.JoinAsync(member, ct);
            foreach (var selection in command.HabitSelections)
            {
                if (details.LinkedHabits.Any(x => x.StepId == selection.StepId)) continue;
                var step = details.Steps.Single(x => x.Id == selection.StepId);
                if (selection.ExistingHabitId.HasValue)
                {
                    var existingHabit = activeUserHabits.FirstOrDefault(x => x.Id == selection.ExistingHabitId.Value);
                    if (existingHabit is null)
                    {
                        await unitOfWork.RollbackAsync(ct);
                        return Result<HabitJourneyMember>.Failure("journey.existing_habit_invalid", "O habito existente informado nao pertence a sua conta.");
                    }
                    await journeys.LinkHabitAsync(member.Id, command.JourneyId, selection.StepId, command.ClientId, command.UserId, existingHabit.Id, existingHabit.Name, ct);
                    continue;
                }
                var requestedName = string.IsNullOrWhiteSpace(selection.HabitName) ? step.SuggestedHabitName : selection.HabitName.Trim();
                if (!selection.DuplicateExisting && guardedHabitNames.Contains(requestedName))
                {
                    await unitOfWork.RollbackAsync(ct);
                    return Result<HabitJourneyMember>.Failure("journey.equivalent_habit_found", "Ja existe um habito com esse nome. Vincule o existente ou confirme a duplicacao.");
                }
                var habit = BuildHabit(command.ClientId, command.UserId, details.Journey, step, selection);
                await habits.CreateAsync(habit, ct);
                if (selection.FrequencyType == HabitFrequencyType.CustomWeekly)
                    await weekDays.ReplaceAsync(habit.Id, selection.SelectedDays, ct);
                await journeys.LinkHabitAsync(member.Id, command.JourneyId, selection.StepId, command.ClientId, command.UserId, habit.Id, habit.Name, ct);
                guardedHabitNames.Add(habit.Name);
            }
            await audit.LogAsync("journey.joined", "Usuario aderiu a jornada guiada", AuditSeverity.Info, command.UserId,
                metadata: new { command.ClientId, command.JourneyId, habits = command.HabitSelections.Count, command.CorrelationId }, ct: ct);
            await unitOfWork.CommitAsync(ct);
            return Result<HabitJourneyMember>.Success(member);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(ct);
            logger.LogError(ex, "Erro ao aderir a jornada {JourneyId} para {UserId}", command.JourneyId, command.UserId);
            return Result<HabitJourneyMember>.Failure("journey.join_error", "Nao foi possivel aderir agora.");
        }
    }

    private static bool Contains(string? source, string? term) =>
        !string.IsNullOrWhiteSpace(source) && !string.IsNullOrWhiteSpace(term) &&
        source.Contains(term.Trim(), StringComparison.OrdinalIgnoreCase);

    public async Task<Result> LeaveAsync(Guid journeyId, Guid clientId, Guid userId, string correlationId, CancellationToken ct = default)
    {
        await journeys.LeaveAsync(journeyId, clientId, userId, ct);
        await audit.LogAsync("journey.left", "Usuario saiu da jornada sem apagar historico", AuditSeverity.Info, userId,
            metadata: new { clientId, journeyId, correlationId }, ct: ct);
        return Result.Success();
    }

    private Habit BuildHabit(Guid clientId, Guid userId, HabitJourney journey, HabitJourneyStep step, JoinHabitJourneyHabitSelection selection)
    {
        var now = DateTime.UtcNow;
        var name = string.IsNullOrWhiteSpace(selection.HabitName) ? step.SuggestedHabitName : selection.HabitName.Trim();
        if (name.Length > 120) name = name[..120];
        return new Habit(Guid.NewGuid(), userId, name, "#2563EB", journey.Category, false, null, now, now,
            selection.FrequencyType, selection.TargetPerWeek, selection.ReminderTime, $"Criado a partir do programa {journey.Name}.",
            ClientId: clientId, StartDate: timeZone.Today(), TemplateIdempotencyKey: Guid.NewGuid(),
            EndDate: timeZone.Today().AddDays(Math.Max(1, journey.SuggestedDurationDays) - 1));
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
