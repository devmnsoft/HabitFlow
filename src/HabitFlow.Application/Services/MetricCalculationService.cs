using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record MetricOccurrence(Guid HabitId, DateOnly Date, bool Completed);

public sealed record CalculatedMetrics(
    double CompletionRate,
    int CurrentStreak,
    int BestStreak,
    int MissedCount,
    double ConsistencyScore);

/// <summary>
/// Canonical, deterministic metric engine. Callers must pass only authorized occurrences;
/// duplicate habit/date rows are deliberately collapsed before any calculation.
/// </summary>
public sealed class MetricCalculationService
{
    public CalculatedMetrics Calculate(IEnumerable<MetricOccurrence> source, DateOnly today)
    {
        var occurrences = source.Where(x => x.Date <= today)
            .GroupBy(x => (x.HabitId, x.Date))
            .Select(x => new MetricOccurrence(x.Key.HabitId, x.Key.Date, x.Any(y => y.Completed)))
            .ToList();
        var scheduled = occurrences.Count;
        var completed = occurrences.Count(x => x.Completed);
        var rate = scheduled == 0 ? 0 : Math.Round(completed * 100d / scheduled, 1);
        var daily = occurrences.GroupBy(x => x.Date)
            .ToDictionary(x => x.Key, x => x.All(y => y.Completed));
        var eligibleDays = daily.Keys.OrderBy(x => x).ToList();
        var best = 0;
        var run = 0;
        foreach (var date in eligibleDays)
        {
            if (daily[date]) { run++; best = Math.Max(best, run); }
            else run = 0;
        }
        var current = 0;
        foreach (var date in eligibleDays.OrderByDescending(x => x))
        {
            if (!daily[date]) break;
            current++;
        }
        return new(rate, current, best, scheduled - completed, rate);
    }

    public static bool IsScheduled(Habit habit, IReadOnlySet<int> customWeekDays, DateOnly date)
    {
        if (habit.IsArchived || habit.IsPaused || date < DateOnly.FromDateTime(habit.CreatedAt) ||
            habit.StartDate.HasValue && date < habit.StartDate.Value)
            return false;
        var day = (int)date.DayOfWeek;
        return habit.FrequencyType switch
        {
            HabitFrequencyType.Daily => true,
            HabitFrequencyType.Weekdays => day is >= 1 and <= 5,
            HabitFrequencyType.Weekends => day is 0 or 6,
            HabitFrequencyType.CustomWeekly => customWeekDays.Contains(day),
            _ => false
        };
    }
}
