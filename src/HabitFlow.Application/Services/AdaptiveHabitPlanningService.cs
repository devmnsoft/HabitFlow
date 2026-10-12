using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public enum HabitCompletionMode { Binary, Quantity }
public enum HabitCompletionState { NoRecord, Partial, MinimumReached, TargetReached, Invalid }

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
    HabitCompletionState State,
    bool IsCompleted,
    bool MinimumReached,
    decimal? RecordedQuantity,
    decimal? TargetQuantity,
    string? Unit,
    string Message);

public sealed record HabitUnitDefinition(string Code, string LabelPtBr, decimal MaxQuantity, int Precision, IReadOnlySet<string> Aliases);

public static class HabitUnitCatalog
{
    private static readonly HabitUnitDefinition[] Units =
    [
        new("min", "minutos", 1440, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "min", "minute", "minutes", "minuto", "minutos" }),
        new("pages", "páginas", 5000, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "page", "pages", "pagina", "página", "paginas", "páginas" }),
        new("steps", "passos", 100000, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "step", "steps", "passo", "passos" }),
        new("ml", "ml", 10000, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ml", "milliliter", "milliliters", "mililitro", "mililitros" }),
        new("liters", "litros", 20, 2, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "l", "lt", "liter", "liters", "litro", "litros" }),
        new("km", "km", 500, 2, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "km", "kilometer", "kilometers", "quilometro", "quilômetros", "quilometros" }),
        new("times", "vezes", 1000, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "time", "times", "vez", "vezes", "reps", "repetitions" }),
        new("sessions", "sessões", 100, 0, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "session", "sessions", "sessao", "sessão", "sessoes", "sessões" })
    ];

    public static IReadOnlyList<HabitUnitDefinition> All => Units;
    public static string? Normalize(string? unit)
    {
        if (string.IsNullOrWhiteSpace(unit)) return null;
        var token = unit.Trim();
        return Units.FirstOrDefault(x => x.Code.Equals(token, StringComparison.OrdinalIgnoreCase) || x.Aliases.Contains(token))?.Code;
    }
    public static HabitUnitDefinition? Find(string? unit)
    {
        var code = Normalize(unit);
        return code is null ? null : Units.FirstOrDefault(x => x.Code == code);
    }
}

public sealed class AdaptiveHabitPlanningService
{
    public Result ValidatePlanning(HabitPlanningInput input)
    {
        if (input.StartDate.HasValue && input.EndDate.HasValue && input.EndDate.Value < input.StartDate.Value)
            return Result.Failure("habit.validity_range", "A data final deve ser posterior ao início do hábito.");

        if (input.RetroactiveAdjustmentDays is < 0 or > 31)
            return Result.Failure("habit.retroactive_window", "A janela de ajuste retroativo deve ficar entre 0 e 31 dias.");

        var targetUnit = NormalizeUnit(input.TargetUnit);
        var hasTargetUnit = targetUnit is not null;
        if (!string.IsNullOrWhiteSpace(input.TargetUnit) && targetUnit is null)
            return Result.Failure("habit.unit_invalid", "A unidade escolhida nao e compatível com o planejamento de habitos.");

        if (input.TargetQuantity.HasValue != hasTargetUnit)
            return Result.Failure("habit.quantity_unit", "Informe quantidade e unidade juntas, ou deixe ambas vazias.");

        if (input.TargetQuantity is <= 0)
            return Result.Failure("habit.quantity_positive", "A quantidade alvo deve ser maior que zero.");

        if (input.TargetQuantity.HasValue && targetUnit is not null)
        {
            var definition = HabitUnitCatalog.Find(targetUnit)!;
            if (input.TargetQuantity.Value > definition.MaxQuantity || !HasAllowedPrecision(input.TargetQuantity.Value, definition.Precision))
                return Result.Failure("habit.quantity_invalid", "A quantidade alvo está fora dos limites aceitos para esta unidade.");
        }

        if (!string.IsNullOrWhiteSpace(input.MinimumVersionName) && input.TargetQuantity.HasValue && !input.MinimumVersionQuantity.HasValue)
            return Result.Failure("habit.minimum_quantity_required", "Informe a quantidade da versão mínima para hábitos quantitativos.");

        if (input.MinimumVersionQuantity.HasValue && !input.TargetQuantity.HasValue)
            return Result.Failure("habit.minimum_quantity_without_target", "Hábitos binários não usam quantidade na versão mínima.");

        if (input.MinimumVersionQuantity.HasValue && input.MinimumVersionQuantity <= 0)
            return Result.Failure("habit.minimum_quantity_positive", "A versão mínima deve ter quantidade maior que zero.");

        if (input.MinimumVersionQuantity.HasValue && input.TargetQuantity.HasValue && input.MinimumVersionQuantity > input.TargetQuantity)
            return Result.Failure("habit.minimum_quantity_too_high", "A versão mínima não pode ser maior que a meta principal.");

        if (input.MinimumVersionQuantity.HasValue && targetUnit is not null)
        {
            var definition = HabitUnitCatalog.Find(targetUnit)!;
            if (!HasAllowedPrecision(input.MinimumVersionQuantity.Value, definition.Precision))
                return Result.Failure("habit.minimum_quantity_precision", "A versão mínima usa precisão incompatível com a unidade.");
        }

        return Result.Success();
    }

    public bool CanAdjustDate(Habit habit, DateOnly targetDate, DateOnly today) =>
        targetDate <= today
        && targetDate >= today.AddDays(-Math.Max(0, habit.RetroactiveAdjustmentDays))
        && (habit.StartDate is null || targetDate >= habit.StartDate.Value)
        && (habit.EndDate is null || targetDate <= habit.EndDate.Value)
        && !habit.IsArchived
        && (!habit.IsPaused || habit.PausedAt is null || targetDate < DateOnly.FromDateTime(habit.PausedAt.Value));

    public HabitCompletionEvaluation EvaluateCompletion(Habit habit, decimal? recordedQuantity) =>
        EvaluateCompletion(habit, recordedQuantity, habit.TargetUnit);

    public HabitCompletionEvaluation EvaluateCompletion(Habit habit, decimal? recordedQuantity, string? recordedUnit)
    {
        if (habit.TargetQuantity is null)
        {
            if (recordedQuantity.HasValue)
                return new(HabitCompletionMode.Binary, HabitCompletionState.Invalid, false, false, recordedQuantity, null, null, "Hábitos binários não recebem quantidade arbitrária.");
            return new(HabitCompletionMode.Binary, HabitCompletionState.TargetReached, true, true, null, null, null, "Conclusao binaria registrada.");
        }

        if (recordedQuantity is null)
            return new(HabitCompletionMode.Quantity, HabitCompletionState.NoRecord, false, false, null, habit.TargetQuantity, NormalizeUnit(habit.TargetUnit), "Informe a quantidade realizada.");

        var unit = NormalizeUnit(habit.TargetUnit);
        var providedUnit = NormalizeUnit(recordedUnit);
        if (recordedQuantity <= 0 || recordedQuantity > (HabitUnitCatalog.Find(unit)?.MaxQuantity ?? 100000))
            return new(HabitCompletionMode.Quantity, HabitCompletionState.Invalid, false, false, recordedQuantity, habit.TargetQuantity, unit, "Quantidade registrada inválida.");

        if (unit is null || providedUnit is null || !string.Equals(unit, providedUnit, StringComparison.Ordinal))
            return new(HabitCompletionMode.Quantity, HabitCompletionState.Invalid, false, false, recordedQuantity, habit.TargetQuantity, unit, "A unidade registrada não corresponde ao alvo do hábito.");

        var completed = recordedQuantity.Value >= habit.TargetQuantity.Value;
        var minimumReached = habit.MinimumVersionQuantity.HasValue && recordedQuantity.Value >= habit.MinimumVersionQuantity.Value;
        var state = completed ? HabitCompletionState.TargetReached : minimumReached ? HabitCompletionState.MinimumReached : HabitCompletionState.Partial;
        return new(HabitCompletionMode.Quantity, state, completed, minimumReached || completed, recordedQuantity, habit.TargetQuantity, unit,
            completed ? "Quantidade suficiente para concluir o habito." :
            minimumReached ? "Versão mínima atingida, sem concluir silenciosamente o alvo principal." :
            "Quantidade registrada, mas abaixo da versão mínima e da meta definida.");
    }

    public string? NormalizeUnit(string? unit) => HabitUnitCatalog.Normalize(unit);

    private static bool HasAllowedPrecision(decimal quantity, int precision) =>
        decimal.Round(quantity, precision) == quantity;
}

