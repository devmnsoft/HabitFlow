using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed class AdaptiveHabitService(IHabitRepository habits, IHabitWeekDayRepository weekDays, AuditService audit)
{
    public async Task<Result> AdjustFrequencyAsync(Guid clientId, Guid userId, string? email, Guid habitId,
        string mode, IReadOnlyCollection<int>? selectedDays, CancellationToken ct = default)
    {
        var habit = await habits.GetAsync(clientId, userId, habitId, ct);
        if (habit is null) return Result.Failure("habit.not_found", "Hábito não encontrado.");
        var normalizedMode = mode?.Trim().ToLowerInvariant();
        var frequency = normalizedMode == "weekdays" ? HabitFrequencyType.Weekdays : HabitFrequencyType.CustomWeekly;
        var days = normalizedMode switch
        {
            "weekdays" => Array.Empty<int>(),
            "three-per-week" => new[] { 1, 3, 5 },
            "custom" => (selectedDays ?? []).Distinct().Order().ToArray(),
            _ => Array.Empty<int>()
        };
        if (normalizedMode is not ("weekdays" or "three-per-week" or "custom") ||
            (frequency == HabitFrequencyType.CustomWeekly && (days.Length is < 1 or > 6 || days.Any(x => x is < 0 or > 6))))
            return Result.Failure("habit.frequency_invalid", "Escolha uma frequência válida para continuar.");
        var updated = habit with { FrequencyType = frequency, TargetPerWeek = frequency == HabitFrequencyType.CustomWeekly ? days.Length : null, UpdatedAt = DateTime.UtcNow };
        if (!await habits.UpdateAsync(clientId, userId, updated, ct)) return Result.Failure("habit.concurrent_update", "O hábito mudou. Atualize a página e tente novamente.");
        await weekDays.ReplaceAsync(habitId, frequency == HabitFrequencyType.CustomWeekly ? days : [], ct);
        await audit.LogAsync("habit.frequency_adjusted", "Frequência do hábito ajustada sem alterar o histórico", AuditSeverity.Info,
            userId, email, new { clientId, habitId, previous = habit.FrequencyType.ToString(), current = frequency.ToString(), days }, ct);
        return Result.Success();
    }

    public async Task<Result> AdjustDurationAsync(Guid clientId, Guid userId, string? email, Guid habitId, int minutes, CancellationToken ct = default)
    {
        if (minutes is < 1 or > 1440) return Result.Failure("habit.duration", "Escolha uma duração entre 1 e 1440 minutos.");
        var habit = await habits.GetAsync(clientId, userId, habitId, ct);
        if (habit is null) return Result.Failure("habit.not_found", "Hábito não encontrado.");
        if (!await habits.UpdateAsync(clientId, userId, habit with { EstimatedTimeMinutes = minutes, UpdatedAt = DateTime.UtcNow }, ct))
            return Result.Failure("habit.concurrent_update", "O hábito mudou. Atualize a página e tente novamente.");
        await audit.LogAsync("habit.duration_adjusted", "Duração do hábito ajustada sem alterar o histórico", AuditSeverity.Info,
            userId, email, new { clientId, habitId, previousMinutes = habit.EstimatedTimeMinutes, minutes }, ct);
        return Result.Success();
    }
}
