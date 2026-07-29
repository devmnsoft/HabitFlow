using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed record HabitCompletionCommand(Guid ClientId, Guid UserId, Guid HabitId, DateOnly LocalDate, string IdempotencyKey, string Source, string CorrelationId);
public sealed record DailySummary(int Scheduled, int Completed, int Pending, int Percentage);
public sealed record NextHabitSnapshot(Guid Id, string Name);
public sealed record GoalProgressUpdate(Guid GoalId, string Title, decimal PreviousValue, decimal CurrentValue, decimal TargetValue, decimal Percentage, bool CompletedNow);
public sealed record MilestoneNotification(Guid MilestoneId, string Title, string Message);
public sealed record HabitCompletionResult(Guid HabitId, DateOnly Date, bool Completed, DailySummary DailySummary, int CurrentStreak, int BestStreak, NextHabitSnapshot? NextHabit, IReadOnlyList<GoalProgressUpdate> GoalUpdates, IReadOnlyList<MilestoneNotification> NewMilestones);
public sealed record ProgressSnapshot(DateOnly Date, DailySummary Daily, int CurrentStreak, int BestStreak, NextHabitSnapshot? NextHabit, IReadOnlyList<HabitDto> Habits);

public sealed class ProgressSnapshotService(IHabitRepository habits, IHabitCompletionRepository completions, IHabitWeekDayRepository weekDays, UserTimeZoneService timeZone)
{
    public async Task<ProgressSnapshot> BuildDayAsync(Guid clientId, Guid userId, DateOnly date, CancellationToken ct = default)
    {
        var all = await habits.ListByUserAsync(userId, ct);
        var days = await weekDays.ListByHabitsAsync(all.Select(x => x.Id), ct);
        var scheduled = all.Where(h => !h.IsArchived && IsScheduled(h, days.GetValueOrDefault(h.Id) ?? [], date)).ToList();
        var history = await completions.ListByUserAsync(userId, date.AddYears(-2), ct);
        var completedIds = history.Where(x => x.CompletedDate == date).Select(x => x.HabitId).ToHashSet();
        var done = scheduled.Count(x => completedIds.Contains(x.Id));
        var summary = new DailySummary(scheduled.Count, done, scheduled.Count - done, scheduled.Count == 0 ? 0 : (int)Math.Round(done * 100d / scheduled.Count));
        var completedDates = history.Select(x => x.CompletedDate).Distinct().Order().ToHashSet();
        var current = 0;
        for (var cursor = date; completedDates.Contains(cursor); cursor = cursor.AddDays(-1)) current++;
        var best = 0; var run = 0; DateOnly? previous = null;
        foreach (var day in completedDates.Order()) { run = previous.HasValue && day == previous.Value.AddDays(1) ? run + 1 : 1; best = Math.Max(best, run); previous = day; }
        var next = scheduled.FirstOrDefault(x => !completedIds.Contains(x.Id));
        return new(date, summary, current, best, next is null ? null : new(next.Id, next.Name), scheduled.Select(x => new HabitDto(x.Id, x.Name, x.Color, x.Category, completedIds.Contains(x.Id), x.IsArchived)).ToList());
    }
    public Task<ProgressSnapshot> BuildDashboardAsync(Guid clientId, Guid userId, CancellationToken ct = default) => BuildDayAsync(clientId, userId, timeZone.Today(), ct);
    private static bool IsScheduled(Habit h, IReadOnlyList<HabitWeekDay> days, DateOnly date) => h.FrequencyType switch
    {
        HabitFrequencyType.Daily => true,
        HabitFrequencyType.Weekdays => date.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
        HabitFrequencyType.Weekends => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
        HabitFrequencyType.CustomWeekly => days.Any(x => x.DayOfWeek == (int)date.DayOfWeek),
        _ => false
    };
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
            await completions.AddAsync(new(Guid.NewGuid(), habit.Id, user.Id, command.LocalDate, DateTime.UtcNow), ct);
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
            await completions.DeleteAsync(habit.Id, user.Id, command.LocalDate, ct);
            var snapshot = await snapshots.BuildDayAsync(command.ClientId, command.UserId, command.LocalDate, ct);
            await unitOfWork.CommitAsync(ct);
            return Result<HabitCompletionResult>.Success(CompleteHabitUseCase.ToResult(command.HabitId, false, snapshot));
        }
        catch { await unitOfWork.RollbackAsync(ct); throw; }
    }
}

public sealed record TodayDashboardViewModel(string Name, string LocalDate, string EffectivePlan, string PlanUsage, ProgressSnapshot Progress);
public sealed class TodayDashboardService(ProgressSnapshotService snapshots, IUserRepository users)
{
    public async Task<TodayDashboardViewModel?> BuildAsync(Guid clientId, Guid userId, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || user.ClientId != clientId) return null;
        var progress = await snapshots.BuildDashboardAsync(clientId, user.Id, ct);
        return new(user.Name, progress.Date.ToString("dd/MM/yyyy"), user.Plan.ToString(), $"{progress.Daily.Scheduled} hábitos previstos", progress);
    }
}
