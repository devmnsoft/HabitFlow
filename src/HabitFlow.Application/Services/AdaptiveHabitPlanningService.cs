using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public enum HabitCompletionMode { Binary, Quantity }

public sealed record HabitPlanningInput(
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal? TargetQuantity,
    string? TargetUnit,
    string? MinimumVersionName,
    decimal? MinimumVersionQuantity,
    int RetroactiveAdjustmentDays);

public sealed record HabitCompletionEvaluation(
    HabitCompletionMode Mode,
    bool IsCompleted,
    decimal? RecordedQuantity,
    decimal? TargetQuantity,
    string? Unit,
    string Message);

public sealed class AdaptiveHabitPlanningService
{
    private static readonly HashSet<string> AllowedUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        "min", "minutes", "pages", "steps", "ml", "liters", "km", "times", "sessions"
    };

    public Result ValidatePlanning(HabitPlanningInput input)
    {
        if (input.StartDate.HasValue && input.EndDate.HasValue && input.EndDate.Value < input.StartDate.Value)
            return Result.Failure("habit.validity_range", "A data final deve ser posterior ao início do hábito.");

        if (input.RetroactiveAdjustmentDays is < 0 or > 31)
            return Result.Failure("habit.retroactive_window", "A janela de ajuste retroativo deve ficar entre 0 e 31 dias.");

        if (input.TargetQuantity.HasValue != !string.IsNullOrWhiteSpace(input.TargetUnit))
            return Result.Failure("habit.quantity_unit", "Informe quantidade e unidade juntas, ou deixe ambas vazias.");

        if (input.TargetQuantity is <= 0)
            return Result.Failure("habit.quantity_positive", "A quantidade alvo deve ser maior que zero.");

        if (!string.IsNullOrWhiteSpace(input.TargetUnit) && !AllowedUnits.Contains(input.TargetUnit.Trim()))
            return Result.Failure("habit.unit_invalid", "A unidade escolhida nao e compatível com o planejamento de habitos.");

        if (input.MinimumVersionQuantity.HasValue && input.MinimumVersionQuantity <= 0)
            return Result.Failure("habit.minimum_quantity_positive", "A versão mínima deve ter quantidade maior que zero.");

        if (input.MinimumVersionQuantity.HasValue && input.TargetQuantity.HasValue && input.MinimumVersionQuantity > input.TargetQuantity)
            return Result.Failure("habit.minimum_quantity_too_high", "A versão mínima não pode ser maior que a meta principal.");

        return Result.Success();
    }

    public bool CanAdjustDate(Habit habit, DateOnly targetDate, DateOnly today) =>
        targetDate <= today && targetDate >= today.AddDays(-Math.Max(0, habit.RetroactiveAdjustmentDays));

    public HabitCompletionEvaluation EvaluateCompletion(Habit habit, decimal? recordedQuantity)
    {
        if (habit.TargetQuantity is null)
            return new(HabitCompletionMode.Binary, true, recordedQuantity, null, null, "Conclusao binaria registrada.");

        if (recordedQuantity is null)
            return new(HabitCompletionMode.Quantity, false, null, habit.TargetQuantity, habit.TargetUnit, "Informe a quantidade realizada.");

        var completed = recordedQuantity.Value >= habit.TargetQuantity.Value;
        return new(HabitCompletionMode.Quantity, completed, recordedQuantity, habit.TargetQuantity, habit.TargetUnit,
            completed ? "Quantidade suficiente para concluir o habito." : "Quantidade registrada, mas abaixo da meta definida.");
    }

    public string? NormalizeUnit(string? unit) => string.IsNullOrWhiteSpace(unit) ? null : unit.Trim().ToLowerInvariant();
}

