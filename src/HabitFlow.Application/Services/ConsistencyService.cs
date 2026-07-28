namespace HabitFlow.Application;

public sealed record ConsistencyResult(int CurrentStreak, int BestStreak, DateOnly? LastCompletedDay, int ActiveDays, int ConsistentWeeks, bool ResumedAfterPause);

public sealed class ConsistencyService
{
    public ConsistencyResult Calculate(IEnumerable<(DateOnly Date, int Scheduled, int Completed)> source, DateOnly today)
    {
        var days = source.Where(x => x.Date <= today && x.Scheduled > 0).OrderBy(x => x.Date).ToList();
        var best = 0; var run = 0; DateOnly? last = null;
        foreach (var day in days)
        {
            // Unscheduled calendar days were filtered above and neither increment nor break a streak.
            if (day.Completed == day.Scheduled) { run++; best = Math.Max(best, run); last = day.Date; }
            // An incomplete current local day remains in progress and does not break yesterday's streak.
            else if (day.Date < today) run = 0;
        }
        var current = days.Count == 0 ? 0 : run;
        var weeks = days.GroupBy(x => (x.Date.Year, Week: System.Globalization.ISOWeek.GetWeekOfYear(x.Date.ToDateTime(TimeOnly.MinValue)))).Count(g => g.All(x => x.Completed == x.Scheduled));
        return new(current, best, last, days.Count, weeks, best > 0 && current > 0 && current < best);
    }
}
