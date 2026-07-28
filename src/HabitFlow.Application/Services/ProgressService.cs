namespace HabitFlow.Application;

public sealed class ProgressService
{
    public int CurrentStreak(IEnumerable<DateOnly> dates, DateOnly today)
    {
        var set = dates.ToHashSet();
        var streak = 0;
        for (var day = today; set.Contains(day); day = day.AddDays(-1)) streak++;
        return streak;
    }

    public int BestStreak(IEnumerable<DateOnly> dates)
    {
        var ordered = dates.Distinct().Order().ToList();
        var best = 0;
        var current = 0;
        DateOnly? previous = null;
        foreach (var day in ordered)
        {
            current = previous.HasValue && day == previous.Value.AddDays(1) ? current + 1 : 1;
            best = Math.Max(best, current);
            previous = day;
        }
        return best;
    }

}
