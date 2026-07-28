using System.Globalization;

namespace HabitFlow.Application;

public sealed class ProgressCalendarService(IProgressCalendarRepository repository, HabitOccurrenceService occurrences,
    ConsistencyService consistency, UserTimeZoneService timeZones)
{
    public async Task<ProgressCalendarViewModel> BuildMonthAsync(Guid clientId, Guid userId, int year, int month, CancellationToken ct = default)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month), "O período informado é inválido.");
        var start = new DateOnly(year, month, 1); var end = start.AddMonths(1).AddDays(-1); var today = timeZones.Today();
        var data = await repository.GetProgressDataAsync(clientId, userId, start, end, ct);
        var zone = timeZones.Resolve();
        var weekDays = data.WeekDays.GroupBy(x => x.HabitId).ToDictionary(x => x.Key, x => (IReadOnlySet<int>)x.Select(y => y.DayOfWeek).ToHashSet());
        var scheduled = await occurrences.ListScheduledForPeriodAsync(data.Habits, weekDays, start, end, zone);
        var byDay = scheduled.GroupBy(x => x.Date).ToDictionary(x => x.Key, x => x.ToList());
        var completionSet = data.Completions.Select(x => (x.HabitId, x.CompletedDate)).ToHashSet();
        var summaries = Enumerable.Range(0, end.Day).Select(i =>
        {
            var date = start.AddDays(i); var planned = byDay.GetValueOrDefault(date) ?? [];
            var completed = planned.Count(x => completionSet.Contains((x.Habit.Id, date)));
            return (Date: date, Scheduled: planned.Count, Completed: completed);
        }).ToList();
        var historical = summaries.Where(x => x.Date <= today).ToList();
        var summary = BuildSummary(historical, today);
        var firstGrid = start.AddDays(-(int)start.DayOfWeek); var lastGrid = end.AddDays(6 - (int)end.DayOfWeek);
        var cells = new List<ProgressCalendarDayViewModel>();
        for (var date = firstGrid; date <= lastGrid; date = date.AddDays(1))
        {
            var item = summaries.FirstOrDefault(x => x.Date == date); var count = item.Scheduled; var done = item.Completed;
            var future = date > today; var status = Status(count, done, future); var percent = Percentage(done, count);
            cells.Add(new(date, date.Day, date.Month == month, date == today, future, count > 0, count, done,
                Math.Max(0, count - done), percent, status, Accessibility(date, status, done, count), $"/progress/day/{date:yyyy-MM-dd}"));
        }
        var previous = start.AddMonths(-1); var next = start.AddMonths(1); var fullHistory = !data.PlanCode.Equals("Free", StringComparison.OrdinalIgnoreCase);
        var historyStart = fullHistory ? null : today.AddDays(-89);
        var message = summary.ScheduledCount == 0 ? "Seu calendário começa quando você inclui o primeiro hábito."
            : summary.CompletedCount == 0 ? "Seu mês está pronto para receber os primeiros passos."
            : summary.CompletionPercentage >= 80 ? "Seus pequenos passos estão formando uma rotina consistente." : "Cada conclusão conta. Continue no seu ritmo.";
        return new(year, month, CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetMonthName(month), start, end,
            previous.Year, previous.Month, next.Year, next.Month, fullHistory || previous.AddMonths(1).AddDays(-1) >= historyStart,
            next <= new DateOnly(today.Year, today.Month, 1), today, summary.CurrentStreak, summary.BestStreak,
            summary.ScheduledCount, summary.CompletedCount, summary.PendingCount, summary.CompletionPercentage, summary.ActiveDays,
            summary.CompletedDays, summary.PartialDays, cells, data.PlanCode, fullHistory, historyStart,
            year == today.Year && month == today.Month, message);
    }

    public async Task<ProgressDayDetailViewModel> BuildDayAsync(Guid clientId, Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var data = await repository.GetProgressDataAsync(clientId, userId, date, date, ct); var zone = timeZones.Resolve();
        var days = data.WeekDays.GroupBy(x => x.HabitId).ToDictionary(x => x.Key, x => (IReadOnlySet<int>)x.Select(y => y.DayOfWeek).ToHashSet());
        var planned = await occurrences.ListScheduledForDateAsync(data.Habits, days, date, zone);
        var completed = data.Completions.Select(x => (x.HabitId, x.CompletedDate)).ToHashSet();
        var habits = planned.Select(x => new ProgressHabitStatusViewModel(x.Habit.Id, x.Habit.Name, x.Habit.Category,
            x.Habit.ReminderTime, completed.Contains((x.Habit.Id, date)))).ToList();
        var done = habits.Count(x => x.Completed);
        return new(date, habits.Count, done, habits.Count - done, Percentage(done, habits.Count), Status(habits.Count, done, date > timeZones.Today()), habits);
    }

    public ProgressSummaryViewModel BuildSummary(IReadOnlyList<(DateOnly Date, int Scheduled, int Completed)> days, DateOnly today)
    {
        var scheduled = days.Sum(x => x.Scheduled); var completed = days.Sum(x => x.Completed); var streak = consistency.Calculate(days, today);
        return new(scheduled, completed, Math.Max(0, scheduled - completed), Percentage(completed, scheduled),
            days.Count(x => x.Scheduled > 0), days.Count(x => x.Scheduled > 0 && x.Completed == x.Scheduled),
            days.Count(x => x.Completed > 0 && x.Completed < x.Scheduled), streak.CurrentStreak, streak.BestStreak);
    }
    public ProgressComparisonViewModel BuildComparison(ProgressSummaryViewModel current, ProgressSummaryViewModel previous) => new(current, previous, current.CompletionPercentage - previous.CompletionPercentage);
    private static decimal Percentage(int completed, int scheduled) => scheduled == 0 ? 0 : Math.Clamp(Math.Round(completed * 100m / scheduled, 1), 0, 100);
    private static ProgressDayStatus Status(int scheduled, int completed, bool future) => scheduled == 0 ? ProgressDayStatus.NoSchedule : future ? ProgressDayStatus.Future : completed == 0 ? ProgressDayStatus.NotStarted : completed < scheduled ? ProgressDayStatus.Partial : ProgressDayStatus.Completed;
    private static string Accessibility(DateOnly date, ProgressDayStatus status, int completed, int scheduled) => status switch
    {
        ProgressDayStatus.NoSchedule => $"{date:dd/MM/yyyy}: sem hábitos previstos",
        ProgressDayStatus.Future => $"{date:dd/MM/yyyy}: {scheduled} hábitos previstos",
        ProgressDayStatus.Completed => $"{date:dd/MM/yyyy}: dia concluído, {completed} de {scheduled}",
        ProgressDayStatus.Partial => $"{date:dd/MM/yyyy}: progresso parcial, {completed} de {scheduled}",
        _ => $"{date:dd/MM/yyyy}: ainda não iniciado, 0 de {scheduled}"
    };
}
