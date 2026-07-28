using System.Globalization;

namespace HabitFlow.Application;

public sealed class ProgressCalendarService(
    IProgressCalendarRepository repository,
    HabitOccurrenceService occurrences,
    ConsistencyService consistency,
    UserTimeZoneService timeZones,
    PlanEntitlementService entitlements)
{
    public async Task<ProgressCalendarViewModel> BuildMonthAsync(Guid clientId, Guid userId, int year, int month, CancellationToken ct = default)
    {
        ValidatePeriod(year, month);
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        var today = timeZones.Today();
        var access = await entitlements.GetAccessSnapshotAsync(clientId, ct);
        DateOnly? historyStart = access.HistoryDaysLimit < 0
            ? null
            : today.AddDays(-(Math.Max(1, access.HistoryDaysLimit) - 1));

        if (historyStart.HasValue && end < historyStart.Value)
            throw new ProgressPeriodAccessException(historyStart.Value);

        // The bounded access window supplies streak context across month/year boundaries.
        var consistencyStart = historyStart ?? start.AddYears(-10);
        var loadStart = consistencyStart < start ? consistencyStart : start;
        var data = await repository.GetProgressDataAsync(clientId, userId, loadStart, end, ct);
        var zone = timeZones.Resolve();
        var weekDays = data.WeekDays.GroupBy(x => x.HabitId)
            .ToDictionary(x => x.Key, x => (IReadOnlySet<int>)x.Select(y => y.DayOfWeek).ToHashSet());
        var scheduled = await occurrences.ListScheduledForPeriodAsync(data.Habits, weekDays, loadStart, end, zone);
        var byDay = scheduled.GroupBy(x => x.Date).ToDictionary(x => x.Key, x => x.ToList());
        var completionSet = data.Completions.Select(x => (x.HabitId, x.CompletedDate)).ToHashSet();
        var allSummaries = BuildDailySummaries(loadStart, end, byDay, completionSet);
        var monthSummaries = allSummaries.Where(x => x.Date >= start).ToList();
        var historicalMonth = monthSummaries.Where(x => x.Date <= today).ToList();
        var summary = BuildSummary(historicalMonth, today);
        var streak = consistency.Calculate(allSummaries, today);
        var cells = BuildCalendarCells(start, end, month, today, monthSummaries);
        var previous = start.AddMonths(-1);
        var next = start.AddMonths(1);
        var fullHistory = access.HistoryDaysLimit < 0;
        var canGoPrevious =
            fullHistory ||
            (historyStart.HasValue && previous.AddMonths(1).AddDays(-1) >= historyStart.Value);
        var canGoNext = next <= new DateOnly(today.Year, today.Month, 1);

        return new ProgressCalendarViewModel
        {
            Year = year, Month = month,
            MonthName = CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetMonthName(month),
            PeriodStart = start, PeriodEnd = end,
            PreviousYear = previous.Year, PreviousMonth = previous.Month,
            NextYear = next.Year, NextMonth = next.Month,
            CanGoPrevious = canGoPrevious, CanGoNext = canGoNext, Today = today,
            CurrentStreak = streak.CurrentStreak, BestStreak = streak.BestStreak,
            ScheduledCount = summary.ScheduledCount, CompletedCount = summary.CompletedCount,
            PendingCount = summary.PendingCount, CompletionPercentage = summary.CompletionPercentage,
            ActiveDays = summary.ActiveDays, CompletedDays = summary.CompletedDays, PartialDays = summary.PartialDays,
            Days = cells, PlanCode = access.EffectivePlanCode, HasFullHistory = fullHistory,
            HistoryLimitStart = historyStart, ConsistencyPeriodStart = loadStart,
            ConsistencyPeriodEnd = end < today ? end : today, IsBestStreakLimitedByPlan = historyStart.HasValue,
            IsCurrentMonth = year == today.Year && month == today.Month,
            InsightMessage = BuildInsightMessage(summary)
        };
    }

    public async Task<ProgressDayDetailViewModel> BuildDayAsync(Guid clientId, Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var today = timeZones.Today();
        var access = await entitlements.GetAccessSnapshotAsync(clientId, ct);
        DateOnly? historyStart = access.HistoryDaysLimit < 0 ? null : today.AddDays(-(Math.Max(1, access.HistoryDaysLimit) - 1));
        if (historyStart.HasValue && date < historyStart.Value) throw new ProgressPeriodAccessException(historyStart.Value);
        var data = await repository.GetProgressDataAsync(clientId, userId, date, date, ct);
        var zone = timeZones.Resolve();
        var days = data.WeekDays.GroupBy(x => x.HabitId).ToDictionary(x => x.Key, x => (IReadOnlySet<int>)x.Select(y => y.DayOfWeek).ToHashSet());
        var planned = await occurrences.ListScheduledForDateAsync(data.Habits, days, date, zone);
        var completed = data.Completions.Select(x => (x.HabitId, x.CompletedDate)).ToHashSet();
        var habits = planned.Select(x => new ProgressHabitStatusViewModel(x.Habit.Id, x.Habit.Name, x.Habit.Category,
            x.Habit.ReminderTime, completed.Contains((x.Habit.Id, date)))).ToList();
        var done = habits.Count(x => x.Completed);
        return new(date, habits.Count, done, habits.Count - done, Percentage(done, habits.Count), Status(habits.Count, done, date > today), habits);
    }

    public ProgressSummaryViewModel BuildSummary(IReadOnlyList<(DateOnly Date, int Scheduled, int Completed)> days, DateOnly today)
    {
        var scheduled = days.Sum(x => x.Scheduled); var completed = days.Sum(x => x.Completed); var streak = consistency.Calculate(days, today);
        return new(scheduled, completed, Math.Max(0, scheduled - completed), Percentage(completed, scheduled),
            days.Count(x => x.Scheduled > 0), days.Count(x => x.Scheduled > 0 && x.Completed == x.Scheduled),
            days.Count(x => x.Completed > 0 && x.Completed < x.Scheduled), streak.CurrentStreak, streak.BestStreak);
    }

    public ProgressComparisonViewModel BuildComparison(ProgressSummaryViewModel current, ProgressSummaryViewModel previous) => new(current, previous, current.CompletionPercentage - previous.CompletionPercentage);
    private static void ValidatePeriod(int year, int month) { if (year is < 2000 or > 2100 || month is < 1 or > 12) throw new ArgumentOutOfRangeException(nameof(month), "O período informado é inválido."); }
    private static List<(DateOnly Date, int Scheduled, int Completed)> BuildDailySummaries(DateOnly start, DateOnly end, IReadOnlyDictionary<DateOnly, List<HabitOccurrence>> byDay, HashSet<(Guid, DateOnly)> completions) =>
        Enumerable.Range(0, end.DayNumber - start.DayNumber + 1).Select(i => { var date = start.AddDays(i); var planned = byDay.GetValueOrDefault(date) ?? []; return (date, planned.Count, planned.Count(x => completions.Contains((x.Habit.Id, date)))); }).ToList();
    private static IReadOnlyList<ProgressCalendarDayViewModel> BuildCalendarCells(DateOnly start, DateOnly end, int month, DateOnly today, IReadOnlyList<(DateOnly Date, int Scheduled, int Completed)> summaries)
    {
        var first = start.AddDays(-(int)start.DayOfWeek); var last = end.AddDays(6 - (int)end.DayOfWeek); var cells = new List<ProgressCalendarDayViewModel>();
        for (var date = first; date <= last; date = date.AddDays(1)) { var item = summaries.FirstOrDefault(x => x.Date == date); var status = Status(item.Scheduled, item.Completed, date > today); cells.Add(new(date, date.Day, date.Month == month, date == today, date > today, item.Scheduled > 0, item.Scheduled, item.Completed, Math.Max(0, item.Scheduled - item.Completed), Percentage(item.Completed, item.Scheduled), status, Accessibility(date, status, item.Completed, item.Scheduled), $"/progress/day/{date:yyyy-MM-dd}")); }
        return cells;
    }
    private static string BuildInsightMessage(ProgressSummaryViewModel summary) => summary.ScheduledCount == 0 ? "Seu calendário começa quando você inclui o primeiro hábito." : summary.CompletedCount == 0 ? "Seu mês está pronto para receber os primeiros passos." : summary.CompletionPercentage >= 80 ? "Seus pequenos passos estão formando uma rotina consistente." : "Cada conclusão conta. Continue no seu ritmo.";
    private static decimal Percentage(int completed, int scheduled) => scheduled == 0 ? 0 : Math.Clamp(Math.Round(completed * 100m / scheduled, 1), 0, 100);
    private static ProgressDayStatus Status(int scheduled, int completed, bool future) => scheduled == 0 ? ProgressDayStatus.NoSchedule : future ? ProgressDayStatus.Future : completed == 0 ? ProgressDayStatus.NotStarted : completed < scheduled ? ProgressDayStatus.Partial : ProgressDayStatus.Completed;
    private static string Accessibility(DateOnly date, ProgressDayStatus status, int completed, int scheduled) => status switch { ProgressDayStatus.NoSchedule => $"{date:dd/MM/yyyy}: sem hábitos previstos", ProgressDayStatus.Future => $"{date:dd/MM/yyyy}: {scheduled} hábitos previstos", ProgressDayStatus.Completed => $"{date:dd/MM/yyyy}: dia concluído, {completed} de {scheduled}", ProgressDayStatus.Partial => $"{date:dd/MM/yyyy}: progresso parcial, {completed} de {scheduled}", _ => $"{date:dd/MM/yyyy}: ainda não iniciado, 0 de {scheduled}" };
}
