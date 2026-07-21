using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class HabitScheduleService(IHabitRepository habits, IHabitWeekDayRepository weekDays, ILogger<HabitScheduleService> logger)
{
    public bool IsHabitDueOnDate(Habit habit, DateOnly date, IReadOnlyCollection<HabitWeekDay> selectedDays)
    {
        if (habit.IsArchived) return false;
        var day = (int)date.DayOfWeek;
        return habit.FrequencyType switch
        {
            HabitFrequencyType.Daily => true,
            HabitFrequencyType.Weekdays => day is >= 1 and <= 5,
            HabitFrequencyType.Weekends => day is 0 or 6,
            HabitFrequencyType.CustomWeekly => selectedDays.Any(x => x.DayOfWeek == day),
            _ => false
        };
    }

    public async Task<IReadOnlyList<Habit>> GetDueHabitsForDate(Guid userId, DateOnly date, CancellationToken ct = default)
    {
        try
        {
            var list = await habits.ListByUserAsync(userId, ct);
            var map = await weekDays.ListByHabitsAsync(list.Select(x => x.Id), ct);
            return list.Where(h => IsHabitDueOnDate(h, date, map.GetValueOrDefault(h.Id, Array.Empty<HabitWeekDay>()))).ToList();
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar hábitos devidos para {UserId}", userId); return Array.Empty<Habit>(); }
    }

    public Result ValidateFrequency(HabitFrequencyType frequencyType, int? targetPerWeek, IReadOnlyCollection<int> selectedDays)
    {
        if (targetPerWeek is < 1 or > 7) return Result.Failure("habit.target_invalid", "A meta semanal deve estar entre 1 e 7.");
        if (selectedDays.Any(x => x is < 0 or > 6)) return Result.Failure("habit.weekday_invalid", "Selecione dias da semana válidos.");
        if (frequencyType == HabitFrequencyType.CustomWeekly && selectedDays.Count == 0) return Result.Failure("habit.custom_days_required", "Selecione pelo menos um dia personalizado.");
        return Result.Success();
    }

    public Task<IReadOnlyList<HabitWeekDay>> GetWeekDaysForHabit(Guid habitId, CancellationToken ct = default) => weekDays.ListByHabitAsync(habitId, ct);
}
