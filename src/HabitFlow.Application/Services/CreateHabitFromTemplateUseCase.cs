using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed record CreateHabitFromTemplateCommand(
    Guid ClientId, Guid UserId, Guid TemplateId, string Name,
    HabitFrequencyType FrequencyType, int? TargetPerWeek, IReadOnlyCollection<int> SelectedDays,
    TimeOnly? PreferredTime, string Color, string? Category, string? Notes, DateOnly StartDate,
    Guid? ExistingGoalId, bool CreateGoal, string? GoalTitle, GoalTargetType? GoalTargetType,
    decimal? GoalTargetValue, bool AllowVariation, string? OnboardingSource, Guid? CollectionId,
    Guid IdempotencyKey, string CorrelationId);

public sealed record TemplatePlanUsage(int ActiveHabits, int? Limit, int Remaining);

public sealed record CreateHabitFromTemplateResult(
    Habit Habit, UserGoal? Goal, bool GoalLinked, bool WasAlreadyAdded, bool IsVariation,
    TemplatePlanUsage PlanUsage, bool OnboardingUpdated, IReadOnlyList<GoalProgressUpdate> GoalUpdates,
    IReadOnlyList<MilestoneNotification> NewMilestones, bool NotificationCreated, string Message);

public sealed class HabitTemplateCustomizationValidator
{
    private static readonly HashSet<string> Colors = new(StringComparer.OrdinalIgnoreCase)
        { "#2563EB", "#16A34A", "#9333EA", "#EA580C", "#DC2626", "#0891B2", "#4F46E5", "#64748B" };
    public Result Validate(CreateHabitFromTemplateCommand command, DateOnly today)
    {
        var name = command.Name?.Trim() ?? string.Empty;
        if (name.Length is < 2 or > 120 || name.Any(char.IsControl))
            return Result.Failure("template.name_invalid", "Informe um nome entre 2 e 120 caracteres, sem caracteres de controle.");
        if (!Enum.IsDefined(command.FrequencyType))
            return Result.Failure("template.frequency_invalid", "Selecione uma frequência válida.");
        var days = command.SelectedDays ?? Array.Empty<int>();
        if (days.Count != days.Distinct().Count() || days.Any(day => day is < 0 or > 6))
            return Result.Failure("template.days_invalid", "Selecione dias da semana válidos, sem repetição.");
        if (command.FrequencyType == HabitFrequencyType.CustomWeekly && days.Count == 0)
            return Result.Failure("template.days_required", "Selecione ao menos um dia para a frequência personalizada.");
        if (command.TargetPerWeek is < 1 or > 7 ||
            (command.FrequencyType == HabitFrequencyType.CustomWeekly && command.TargetPerWeek != days.Count))
            return Result.Failure("template.target_invalid", "A meta semanal deve corresponder aos dias selecionados e estar entre 1 e 7.");
        if (!Colors.Contains(command.Color ?? string.Empty))
            return Result.Failure("template.color_invalid", "Selecione uma cor disponível na paleta.");
        if (command.Category?.Trim().Length > 80)
            return Result.Failure("template.category_invalid", "A categoria deve ter até 80 caracteres.");
        if (command.Notes?.Length > 1000)
            return Result.Failure("template.notes_invalid", "As notas devem ter até 1.000 caracteres.");
        if (command.StartDate < today.AddYears(-1) || command.StartDate > today.AddYears(1))
            return Result.Failure("template.start_date_invalid", "A data de início deve estar entre um ano atrás e um ano à frente.");
        if (command.ExistingGoalId.HasValue && command.CreateGoal)
            return Result.Failure("template.goal_choice_invalid", "Selecione um objetivo existente ou crie um novo.");
        if (command.CreateGoal && (string.IsNullOrWhiteSpace(command.GoalTitle) || command.GoalTitle.Trim().Length > 140 ||
            command.GoalTargetType is null || !Enum.IsDefined(command.GoalTargetType.Value) ||
            command.GoalTargetValue is null or <= 0 or > 1_000_000 || command.GoalTargetValue != decimal.Truncate(command.GoalTargetValue.Value)))
            return Result.Failure("template.goal_invalid", "Revise o título, o tipo e o valor do novo objetivo.");
        if (command.IdempotencyKey == Guid.Empty || string.IsNullOrWhiteSpace(command.CorrelationId))
            return Result.Failure("template.idempotency_required", "Não foi possível identificar esta solicitação.");
        return Result.Success();
    }

    public static IReadOnlyCollection<string> AllowedColors => Colors;
}

public sealed class CreateHabitFromTemplateUseCase(
    IUserRepository users, IHabitTemplateRepository templates, IHabitRepository habits,
    IHabitWeekDayRepository weekDays, IUserGoalRepository goals, PlanEntitlementService entitlements,
    IUnitOfWork unitOfWork, AuditService audit, NotificationService notifications,
    HabitTemplateCustomizationValidator validator, TimeProvider clock, UserTimeZoneService timeZone,
    TemplateActivationService activation, ILogger<CreateHabitFromTemplateUseCase> logger)
{
    public async Task<Result<CreateHabitFromTemplateResult>> ExecuteAsync(CreateHabitFromTemplateCommand command, CancellationToken ct = default)
    {
        if (command.ClientId == Guid.Empty || command.UserId == Guid.Empty)
            return Result<CreateHabitFromTemplateResult>.Failure("template.tenant_required", "A conta e a pessoa são obrigatórias.");
        // StartDate is a business date in the person's configured timezone, not a UTC date.
        var validation = validator.Validate(command, timeZone.Today());
        if (validation.IsFailure) return Result<CreateHabitFromTemplateResult>.Failure(validation.Error.Code, validation.Error.Message);

        try
        {
            await unitOfWork.BeginTransactionAsync(ct);
            var user = await users.GetByIdAsync(command.UserId, ct);
            if (user?.ClientId != command.ClientId)
                return await Rollback("template.user_forbidden", "A pessoa não pertence à conta informada.", ct);
            var template = await templates.GetAsync(command.TemplateId, ct);
            if (template is not { IsActive: true } || template.PublishedAt is null || template.PublishedAt > clock.GetUtcNow().UtcDateTime)
                return await Rollback("template.unavailable", "Este template não está disponível.", ct);

            var existing = await habits.FindByIdempotencyKeyAsync(command.ClientId, command.UserId, command.IdempotencyKey, ct)
                ?? (!command.AllowVariation
                    ? await habits.FindActiveBySourceTemplateAsync(command.ClientId, command.UserId, command.TemplateId, false, ct)
                    : null);
            var limit = await entitlements.GetIntegerFeatureAsync(command.UserId, PlanFeatureCodes.ActiveHabitsLimit, ct);
            var active = await habits.CountActiveAsync(command.ClientId, command.UserId, ct);
            var usage = new TemplatePlanUsage(active, limit, limit is null or < 0 ? int.MaxValue : Math.Max(0, limit.Value - active));
            if (existing is not null)
            {
                await unitOfWork.CommitAsync(ct);
                return Result<CreateHabitFromTemplateResult>.Success(new(existing, null, false, true, existing.IsTemplateVariation, usage, false, [], [], false, "Este hábito já havia sido adicionado."));
            }
            if (!await entitlements.CanCreateHabitAsync(command.UserId, active, ct))
                return await Rollback("template.habit_limit", "Seu plano não possui espaço para outro hábito ativo.", ct);
            if (!string.Equals(template.MinimumPlanCode, PlanCodes.Free, StringComparison.OrdinalIgnoreCase) &&
                !await entitlements.CanUseFullLibraryAsync(command.UserId, ct))
                return await Rollback("template.plan_required", "Este template requer acesso à Biblioteca completa.", ct);

            UserGoal? goal = null;
            if (command.ExistingGoalId.HasValue)
            {
                goal = await goals.GetAsync(command.ExistingGoalId.Value, command.ClientId, command.UserId, ct);
                if (goal is null) return await Rollback("template.goal_forbidden", "O objetivo selecionado não foi encontrado nesta conta.", ct);
            }
            else if (command.CreateGoal)
            {
                var goalLimit = await entitlements.GetIntegerFeatureAsync(command.UserId, PlanFeatureCodes.ActiveGoalsLimit, ct);
                var goalCount = await goals.CountActiveAsync(command.ClientId, command.UserId, ct);
                if (goalLimit is >= 0 && goalCount >= goalLimit) return await Rollback("template.goal_limit", "Seu plano não possui espaço para outro objetivo ativo.", ct);
                var now = clock.GetUtcNow().UtcDateTime;
                goal = new(Guid.NewGuid(), command.ClientId, command.UserId, null, command.GoalTitle!.Trim(), null,
                    command.GoalTargetType!.Value.ToString(), checked((int)command.GoalTargetValue!.Value), 0, command.StartDate, null,
                    "Active", command.Color, template.IconCode, now, now, null);
                await goals.CreateAsync(goal, ct);
            }

            var habit = await activation.ActivateAsync(template, command, goal, ct);
            await notifications.CreateAsync(command.UserId, "habit_template_added", "Hábito adicionado", "Agora você pode acompanhá-lo no Dashboard.", "habit", habit.Id, ct);
            await unitOfWork.CommitAsync(ct);
            var finalUsage = usage with { ActiveHabits = active + 1, Remaining = usage.Limit is null or < 0 ? int.MaxValue : Math.Max(0, usage.Limit.Value - active - 1) };
            var goalUpdates = goal is null
                ? Array.Empty<GoalProgressUpdate>()
                : new[]
                {
                    new GoalProgressUpdate(goal.Id, goal.Title, goal.CurrentValue, goal.CurrentValue,
                        goal.TargetValue, goal.TargetValue <= 0 ? 0 : Math.Min(100m,
                            Math.Round(goal.CurrentValue * 100m / goal.TargetValue, 1)),
                        false, goal.Status, command.CreateGoal, true)
                };
            return Result<CreateHabitFromTemplateResult>.Success(new(habit, goal, goal is not null, false, command.AllowVariation, finalUsage, false, goalUpdates, [], true, "Hábito adicionado com sucesso."));
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync(ct);
            logger.LogError(ex, "Falha transacional ao ativar template {TemplateId} para {UserId}; correlation {CorrelationId}", command.TemplateId, command.UserId, command.CorrelationId);
            return Result<CreateHabitFromTemplateResult>.Failure("template.create_error", "Não foi possível adicionar o hábito agora.");
        }
    }

    private async Task<Result<CreateHabitFromTemplateResult>> Rollback(string code, string message, CancellationToken ct)
    {
        await unitOfWork.RollbackAsync(ct);
        return Result<CreateHabitFromTemplateResult>.Failure(code, message);
    }
}
