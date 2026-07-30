using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed record HabitCompletionCommand(Guid ClientId, Guid UserId, Guid HabitId, DateOnly LocalDate, string IdempotencyKey, string Source, string CorrelationId);
public sealed record DailySummary(int Scheduled, int Completed, int Pending, int Percentage);
public sealed record NextHabitSnapshot(Guid Id, string Name);
public sealed record ProgressStreakSnapshot(int Current, int Best);
public sealed record ProgressHabitSnapshot(Guid HabitId, string Name, string Category, int Scheduled, int Completed, int Pending, decimal Percentage, bool CompletedToday = false);
public sealed record ProgressDaySnapshot(DateOnly Date, int Scheduled, int Completed, int Pending, decimal Percentage,
    int CurrentStreak, int BestStreak, NextHabitSnapshot? NextHabit, IReadOnlyList<ProgressHabitSnapshot> Habits);
public sealed record ProgressPeriodSnapshot(DateOnly PeriodStart, DateOnly PeriodEnd, int Scheduled, int Completed, int Pending,
    decimal Percentage, int ActiveDays, int CompletedDays, int PartialDays, int CurrentStreak, int BestStreak,
    IReadOnlyList<ProgressDaySnapshot> DailySummaries, IReadOnlyList<ProgressHabitSnapshot> HabitSummaries);
public sealed record GoalProgressUpdate(Guid GoalId, string Title, decimal PreviousValue, decimal CurrentValue, decimal TargetValue, decimal Percentage, bool CompletedNow);
public sealed record MilestoneNotification(Guid MilestoneId, string Title, string Message);
public sealed record HabitCompletionResult(Guid HabitId, DateOnly Date, bool Completed, DailySummary DailySummary, int CurrentStreak, int BestStreak, NextHabitSnapshot? NextHabit, IReadOnlyList<GoalProgressUpdate> GoalUpdates, IReadOnlyList<MilestoneNotification> NewMilestones);
public sealed record ProgressSnapshot(DateOnly Date, DailySummary Daily, int CurrentStreak, int BestStreak, NextHabitSnapshot? NextHabit, IReadOnlyList<HabitDto> Habits);

public sealed class ProgressSnapshotService(IProgressCalendarRepository repository, HabitOccurrenceService occurrences,
    ConsistencyService consistency, UserTimeZoneService timeZone, PlanEntitlementService entitlements)
{
    public async Task<ProgressSnapshot> BuildDayAsync(Guid clientId, Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var period = await BuildPeriodAsync(clientId, userId, date, date, ct);
        var day = period.DailySummaries.Single();
        var summary = new DailySummary(day.Scheduled, day.Completed, day.Pending, (int)Math.Round(day.Percentage));
        return new(date, summary, day.CurrentStreak, day.BestStreak, day.NextHabit,
            day.Habits.Select(x => new HabitDto(x.HabitId, x.Name, "", x.Category, x.CompletedToday, false)).ToList());
    }
    public async Task<ProgressPeriodSnapshot> BuildPeriodAsync(Guid clientId, Guid userId, DateOnly start, DateOnly end, CancellationToken ct = default)
    {
        if (start > end) throw new ArgumentException("O início deve ser anterior ao fim do período.");
        var today = timeZone.Today();
        var access = await entitlements.GetAccessSnapshotAsync(clientId, ct);
        var contextStart = access.HistoryDaysLimit < 0 ? start : Max(start, today.AddDays(-(Math.Max(1, access.HistoryDaysLimit) - 1)));
        var data = await repository.GetProgressDataAsync(clientId, userId, contextStart, end, ct);
        var configured = data.WeekDays.GroupBy(x => x.HabitId).ToDictionary(x => x.Key, x => (IReadOnlySet<int>)x.Select(y => y.DayOfWeek).ToHashSet());
        var planned = await occurrences.ListScheduledForPeriodAsync(data.Habits, configured, contextStart, end, timeZone.Resolve());
        var completed = data.Completions.Select(x => (x.HabitId, x.CompletedDate)).ToHashSet();
        var raw = Enumerable.Range(0, end.DayNumber - contextStart.DayNumber + 1).Select(offset => { var date = contextStart.AddDays(offset); var items = planned.Where(x => x.Date == date).ToList(); return (Date: date, Scheduled: items.Count, Completed: items.Count(x => completed.Contains((x.Habit.Id, date))), Items: items); }).ToList();
        var streak = consistency.Calculate(raw.Select(x => (x.Date, x.Scheduled, x.Completed)), today);
        var visible = raw.Where(x => x.Date >= start).ToList();
        var days = visible.Select(x => { var habits = x.Items.Select(i => new ProgressHabitSnapshot(i.Habit.Id, i.Habit.Name, i.Habit.Category, 1, completed.Contains((i.Habit.Id, x.Date)) ? 1 : 0, completed.Contains((i.Habit.Id, x.Date)) ? 0 : 1, completed.Contains((i.Habit.Id, x.Date)) ? 100 : 0, completed.Contains((i.Habit.Id, x.Date)))).ToList(); var next = habits.FirstOrDefault(h => !h.CompletedToday); return new ProgressDaySnapshot(x.Date, x.Scheduled, x.Completed, Math.Max(0, x.Scheduled - x.Completed), Percent(x.Completed, x.Scheduled), streak.CurrentStreak, streak.BestStreak, next is null ? null : new(next.HabitId, next.Name), habits); }).ToList();
        var habitSummaries = visible.SelectMany(x => x.Items.Select(i => (i.Habit, x.Date))).GroupBy(x => x.Habit.Id).Select(g => { var count = g.Count(); var done = g.Count(x => completed.Contains((x.Habit.Id, x.Date))); var h = g.First().Habit; return new ProgressHabitSnapshot(h.Id, h.Name, h.Category, count, done, count - done, Percent(done, count)); }).ToList();
        var scheduled = visible.Sum(x => x.Scheduled); var done = visible.Sum(x => x.Completed);
        return new(start, end, scheduled, done, Math.Max(0, scheduled - done), Percent(done, scheduled), visible.Count(x => x.Scheduled > 0), visible.Count(x => x.Scheduled > 0 && x.Completed == x.Scheduled), visible.Count(x => x.Completed > 0 && x.Completed < x.Scheduled), streak.CurrentStreak, streak.BestStreak, days, habitSummaries);
    }
    public Task<ProgressSnapshot> BuildDashboardAsync(Guid clientId, Guid userId, CancellationToken ct = default) => BuildDayAsync(clientId, userId, timeZone.Today(), ct);
    private static decimal Percent(int completed, int scheduled) => scheduled == 0 ? 0 : Math.Round(completed * 100m / scheduled, 1);
    private static DateOnly Max(DateOnly left, DateOnly right) => left > right ? left : right;
}

public sealed class CompleteHabitUseCase(IUserRepository users, IHabitRepository habits, IHabitCompletionRepository completions, IUnitOfWork unitOfWork, ProgressSnapshotService snapshots, AuditService audit, UserTimeZoneService clock)
{
    public async Task<Result<HabitCompletionResult>> ExecuteAsync(HabitCompletionCommand command, CancellationToken ct = default)
    {
        if (command.LocalDate > clock.Today()) return Result<HabitCompletionResult>.Failure("habit.future_date", "Não é possível concluir um hábito em uma data futura.");
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var user = await users.GetByIdAsync(command.UserId, ct);
            var habit = await habits.GetAsync(command.HabitId, ct);
            if (user is null || user.ClientId != command.ClientId || habit is null || !habit.BelongsTo(command.UserId)) { await unitOfWork.RollbackAsync(ct); return Result<HabitCompletionResult>.Failure("habit.not_found", "Este hábito não foi encontrado."); }
            if (habit.IsArchived) { await unitOfWork.RollbackAsync(ct); return Result<HabitCompletionResult>.Failure("habit.archived", "Um hábito arquivado não pode ser concluído."); }
            var mutation = await completions.AddIfMissingAsync(command.ClientId, user.Id, habit.Id,
                command.LocalDate, Guid.NewGuid(), ct);
            if (mutation.Created)
                await audit.LogAsync("habit.completed", "Hábito concluído", AuditSeverity.Info, user.Id, user.Email, new { habitId = habit.Id, command.Source, command.CorrelationId, command.IdempotencyKey }, ct);
            var snapshot = await snapshots.BuildDayAsync(command.ClientId, command.UserId, command.LocalDate, ct);
            await unitOfWork.CommitAsync(ct);
            return Result<HabitCompletionResult>.Success(ToResult(command.HabitId, true, snapshot));
        }
        catch { await unitOfWork.RollbackAsync(ct); throw; }
    }
    internal static HabitCompletionResult ToResult(Guid habitId, bool completed, ProgressSnapshot s) => new(habitId, s.Date, completed, s.Daily, s.CurrentStreak, s.BestStreak, s.NextHabit, [], []);
}

public sealed class UndoHabitCompletionUseCase(IUserRepository users, IHabitRepository habits, IHabitCompletionRepository completions, IUnitOfWork unitOfWork, ProgressSnapshotService snapshots, UserTimeZoneService clock)
{
    public async Task<Result<HabitCompletionResult>> ExecuteAsync(HabitCompletionCommand command, CancellationToken ct = default)
    {
        if (command.LocalDate > clock.Today()) return Result<HabitCompletionResult>.Failure("habit.future_date", "Data futura inválida.");
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var user = await users.GetByIdAsync(command.UserId, ct); var habit = await habits.GetAsync(command.HabitId, ct);
            if (user is null || user.ClientId != command.ClientId || habit is null || !habit.BelongsTo(command.UserId)) { await unitOfWork.RollbackAsync(ct); return Result<HabitCompletionResult>.Failure("habit.not_found", "Este hábito não foi encontrado."); }
            await completions.DeleteIfExistsAsync(command.ClientId, user.Id, habit.Id, command.LocalDate, ct);
            var snapshot = await snapshots.BuildDayAsync(command.ClientId, command.UserId, command.LocalDate, ct);
            await unitOfWork.CommitAsync(ct);
            return Result<HabitCompletionResult>.Success(CompleteHabitUseCase.ToResult(command.HabitId, false, snapshot));
        }
        catch { await unitOfWork.RollbackAsync(ct); throw; }
    }
}

public sealed record TodayDashboardViewModel(string Name, string LocalDate, string EffectivePlan, string PlanUsage, ProgressSnapshot Progress);
public sealed class TodayDashboardService(ProgressSnapshotService snapshots, IUserRepository users, PlanEntitlementService entitlements)
{
    public async Task<TodayDashboardViewModel?> BuildAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || user.ClientId != clientId) return null;
        var progress = await snapshots.BuildDashboardAsync(clientId, user.Id, ct);
        var access = await entitlements.GetAccessSnapshotAsync(clientId, ct);
        return new(user.Name, progress.Date.ToString("dd/MM/yyyy"), access.EffectivePlanCode,
            $"{progress.Daily.Completed} de {progress.Daily.Scheduled} hábitos previstos concluídos", progress);
    }
}
