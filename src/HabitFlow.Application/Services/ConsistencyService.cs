namespace HabitFlow.Application;

public sealed record ConsistencyResult(int CurrentStreak, int BestStreak, DateOnly? LastCompletedDay, int ActiveDays, int ConsistentWeeks, bool ResumedAfterPause);

public sealed class ConsistencyService
{
    public ConsistencyResult Calculate(IEnumerable<(DateOnly Date, int Scheduled, int Completed)> source, DateOnly today)
    {
        var days = source.Where(x => x.Date <= today && x.Scheduled > 0).OrderBy(x => x.Date).ToList();
        var best = 0; var run = 0; DateOnly? previous = null; DateOnly? last = null;
        foreach (var day in days)
        {
            if (day.Completed == day.Scheduled) { run = previous is null || day.Date == previous.Value.AddDays(1) ? run + 1 : 1; best = Math.Max(best, run); last = day.Date; }
            else run = 0;
            previous = day.Date;
        }
        var current = days.Count == 0 || days[^1].Completed != days[^1].Scheduled ? 0 : run;
        var weeks = days.GroupBy(x => (x.Date.Year, Week: System.Globalization.ISOWeek.GetWeekOfYear(x.Date.ToDateTime(TimeOnly.MinValue)))).Count(g => g.All(x => x.Completed == x.Scheduled));
        return new(current, best, last, days.Count, weeks, best > 0 && current > 0 && current < best);
    }
}
