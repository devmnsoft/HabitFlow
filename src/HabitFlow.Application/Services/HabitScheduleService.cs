using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class HabitScheduleService(IHabitRepository habits, IHabitWeekDayRepository weekDays, ILogger<HabitScheduleService> logger, HabitScheduleNormalizer normalizer)
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

    public Result ValidateFrequency(HabitFrequencyType frequencyType, int? targetPerWeek, IReadOnlyCollection<int>? selectedDays)
    {
        var result = normalizer.Normalize(new(frequencyType, targetPerWeek, selectedDays));
        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error.Code, result.Error.Message);
    }

    public Task<IReadOnlyList<HabitWeekDay>> GetWeekDaysForHabit(Guid habitId, CancellationToken ct = default) => weekDays.ListByHabitAsync(habitId, ct);
}
