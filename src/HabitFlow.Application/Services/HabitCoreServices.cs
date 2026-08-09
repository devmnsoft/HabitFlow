using System.Text.RegularExpressions;
using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed record HabitListQuery(string? Search = null, string? Category = null, string Status = "active", string Sort = "newest", int Page = 1, int PageSize = 12);
public sealed record HabitListItem(Habit Habit, int CompletedCount, int ScheduledCount, decimal Consistency);
public sealed record HabitListViewModel(IReadOnlyList<HabitListItem> Items, IReadOnlyList<string> Categories, HabitListQuery Query, int Total, int TotalPages);
public sealed record HabitEditorViewModel(Guid? Id, string Name, string Color, string? Category, string IconCode,
    HabitFrequencyType FrequencyType, int? TargetPerWeek, TimeOnly? ReminderTime, string? Notes,
    IReadOnlyList<int> SelectedDays, Guid? ObjectiveId, int? EstimatedTimeMinutes, HabitDifficulty? Difficulty);
public sealed record HabitCalendarDay(DateOnly Date, bool Scheduled, bool Completed);
public sealed record HabitTimelineItem(DateTime At, string Title, string Description);
public sealed record HabitDetailsViewModel(Habit Habit, int CompletedCount, int ScheduledCount, decimal Consistency,
    int CurrentStreak, int BestStreak, IReadOnlyList<HabitCalendarDay> Calendar, IReadOnlyList<HabitTimelineItem> Timeline);

public sealed class HabitQueryService(IHabitRepository habits, IHabitCompletionRepository completions, IHabitWeekDayRepository weekDays,
    HabitOccurrenceService occurrences, UserTimeZoneService clock)
{
    public async Task<HabitListViewModel> SearchAsync(Guid clientId, Guid userId, HabitListQuery query, CancellationToken ct = default)
    {
        var all = await habits.ListAsync(clientId, userId, ct);
        var scoped = all.Where(x => MatchesStatus(x, query.Status));
        if (!string.IsNullOrWhiteSpace(query.Search)) scoped = scoped.Where(x => x.Name.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase) || (x.Category?.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase) ?? false));
        if (!string.IsNullOrWhiteSpace(query.Category)) scoped = scoped.Where(x => string.Equals(x.Category, query.Category, StringComparison.OrdinalIgnoreCase));
        scoped = query.Sort switch { "name" => scoped.OrderBy(x => x.Name), "oldest" => scoped.OrderBy(x => x.CreatedAt), "updated" => scoped.OrderByDescending(x => x.UpdatedAt), _ => scoped.OrderByDescending(x => x.CreatedAt) };
        var total = scoped.Count(); var pageSize = Math.Clamp(query.PageSize, 6, 48); var page = Math.Max(1, query.Page);
        var selected = scoped.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var from = clock.Today().AddDays(-27); var to = clock.Today();
        var done = await completions.ListAsync(clientId, userId, from, to, ct);
        var dayMap = await weekDays.ListByHabitsAsync(selected.Select(x => x.Id).ToArray(), ct);
        var configured = dayMap.ToDictionary(x => x.Key, x => (IReadOnlySet<int>)x.Value.Select(y => y.DayOfWeek).ToHashSet());
        var planned = await occurrences.ListScheduledForPeriodAsync(selected, configured, from, to, clock.Resolve());
        var items = selected.Select(h => { var scheduled = planned.Count(x => x.Habit.Id == h.Id); var completed = done.Count(x => x.HabitId == h.Id); return new HabitListItem(h, completed, scheduled, scheduled == 0 ? 0 : Math.Round(completed * 100m / scheduled, 1)); }).ToList();
        return new(items, all.Select(x => x.Category).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Order().Cast<string>().ToList(), query with { Page = page, PageSize = pageSize }, total, (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<HabitDetailsViewModel?> DetailAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default)
    {
        var habit = await habits.GetAsync(clientId, userId, habitId, ct); if (habit is null) return null;
        var from = clock.Today().AddDays(-83); var to = clock.Today(); var done = await completions.ListAsync(clientId, userId, from, to, ct);
        var dayMap = await weekDays.ListByHabitsAsync([habit.Id], ct);
        var configured = (dayMap.TryGetValue(habit.Id, out var configuredDays) ? configuredDays : Array.Empty<HabitWeekDay>()).Select(x => x.DayOfWeek).ToHashSet();
        var planned = await occurrences.ListScheduledForPeriodAsync([habit], new Dictionary<Guid, IReadOnlySet<int>> { [habit.Id] = configured }, from, to, clock.Resolve());
        var completedDates = done.Where(x => x.HabitId == habit.Id).Select(x => x.CompletedDate).ToHashSet();
        var calendar = Enumerable.Range(0, 84).Select(i => from.AddDays(i)).Select(d => new HabitCalendarDay(d, planned.Any(x => x.Date == d), completedDates.Contains(d))).ToList();
        var streaks = CalculateStreak(calendar, to);
        var timeline = done.Where(x => x.HabitId == habit.Id).OrderByDescending(x => x.CompletedDate).Take(20).Select(x => new HabitTimelineItem(x.CreatedAt, "Hábito concluído", x.CompletedDate.ToString("dd/MM/yyyy"))).ToList();
        timeline.Add(new(habit.CreatedAt, "Hábito criado", "A jornada começou aqui."));
        return new(habit, completedDates.Count, planned.Count, planned.Count == 0 ? 0 : Math.Round(completedDates.Count * 100m / planned.Count, 1), streaks.Current, streaks.Best, calendar, timeline.OrderByDescending(x => x.At).ToList());
    }
    private static bool MatchesStatus(Habit h, string status) => status switch { "archived" => h.IsArchived, "paused" => h.IsPaused && !h.IsArchived, "all" => true, _ => !h.IsArchived && !h.IsPaused };
    private static (int Current, int Best) CalculateStreak(IReadOnlyList<HabitCalendarDay> days, DateOnly today) { var current = 0; var best = 0; var run = 0; foreach (var d in days.Where(x => x.Scheduled)) { run = d.Completed ? run + 1 : 0; best = Math.Max(best, run); if (d.Date <= today) current = run; } return (current, best); }
}

public sealed class HabitLifecycleService(IHabitRepository habits, AuditService audit)
{
    public Task<Result> PauseAsync(Guid clientId, Guid userId, string? email, Guid id, CancellationToken ct = default) => ChangeAsync(clientId, userId, email, id, "pause", ct);
    public Task<Result> ResumeAsync(Guid clientId, Guid userId, string? email, Guid id, CancellationToken ct = default) => ChangeAsync(clientId, userId, email, id, "resume", ct);
    public Task<Result> ArchiveAsync(Guid clientId, Guid userId, string? email, Guid id, CancellationToken ct = default) => ChangeAsync(clientId, userId, email, id, "archive", ct);
    public Task<Result> RestoreAsync(Guid clientId, Guid userId, string? email, Guid id, CancellationToken ct = default) => ChangeAsync(clientId, userId, email, id, "restore", ct);
    private async Task<Result> ChangeAsync(Guid clientId, Guid userId, string? email, Guid id, string action, CancellationToken ct)
    {
        var habit = await habits.GetAsync(clientId, userId, id, ct); if (habit is null) return Result.Failure("habit.not_found", "Hábito não encontrado.");
        var now = DateTime.UtcNow; var changed = action switch { "pause" when !habit.IsArchived => habit with { IsPaused = true, PausedAt = now, UpdatedAt = now }, "resume" when !habit.IsArchived => habit with { IsPaused = false, PausedAt = null, UpdatedAt = now }, "archive" => habit with { IsArchived = true, ArchivedAt = now, IsPaused = false, PausedAt = null, UpdatedAt = now }, "restore" => habit with { IsArchived = false, ArchivedAt = null, UpdatedAt = now }, _ => habit };
        if (!await habits.UpdateAsync(clientId, userId, changed, ct)) return Result.Failure("habit.concurrent_update", "O hábito mudou durante a operação. Atualize a página.");
        await audit.LogAsync($"habit.{action}", $"Hábito: {action}", AuditSeverity.Info, userId, email, new { habitId = id }, ct); return Result.Success();
    }
}

public sealed class HabitEditorService(IHabitRepository habits, IHabitWeekDayRepository weekDays, HabitScheduleService schedule, HabitPolicy policy, AuditService audit)
{
    private static readonly Regex HexColor = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
    public async Task<HabitEditorViewModel?> LoadAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default)
    {
        var h = await habits.GetAsync(clientId, userId, id, ct); if (h is null) return null;
        var days = await weekDays.ListByHabitAsync(id, ct);
        return new(h.Id, h.Name, h.Color, h.Category, h.IconCode ?? "check-circle", h.FrequencyType, h.TargetPerWeek, h.ReminderTime, h.Notes, days.Select(x => x.DayOfWeek).ToList(), h.ObjectiveId, h.EstimatedTimeMinutes, h.Difficulty);
    }
    public async Task<Result<Habit>> SaveAsync(User user, HabitEditorViewModel input, CancellationToken ct = default)
    {
        if (!user.ClientId.HasValue) return Result<Habit>.Failure("client.required", "Conta inválida.");
        if (string.IsNullOrWhiteSpace(input.Name) || input.Name.Trim().Length > 120) return Result<Habit>.Failure("habit.name", "Informe um nome com até 120 caracteres.");
        if (!HexColor.IsMatch(input.Color ?? "")) return Result<Habit>.Failure("habit.color", "Selecione uma cor válida.");
        if (input.EstimatedTimeMinutes is < 1 or > 1440) return Result<Habit>.Failure("habit.duration", "A duração deve ficar entre 1 e 1440 minutos.");
        var frequency = schedule.ValidateFrequency(input.FrequencyType, input.TargetPerWeek, input.SelectedDays); if (frequency.IsFailure) return Result<Habit>.Failure(frequency.Error.Code, frequency.Error.Message);
        var current = input.Id.HasValue ? await habits.GetAsync(user.ClientId.Value, user.Id, input.Id.Value, ct) : null;
        if (input.Id.HasValue && current is null) return Result<Habit>.Failure("habit.not_found", "Hábito não encontrado.");
        if (current is null) { var count = await habits.CountActiveAsync(user.ClientId.Value, user.Id, ct); var allowed = policy.CanCreate(user, count); if (allowed.IsFailure) return Result<Habit>.Failure(allowed.Error.Code, allowed.Error.Message); }
        var now = DateTime.UtcNow; var habit = (current ?? new Habit(Guid.NewGuid(), user.Id, input.Name.Trim(), input.Color, input.Category, false, null, now, now, ClientId: user.ClientId)) with { Name = input.Name.Trim(), Color = input.Color, Category = Clean(input.Category, 80), IconCode = Clean(input.IconCode, 40) ?? "check-circle", FrequencyType = input.FrequencyType, TargetPerWeek = input.TargetPerWeek, ReminderTime = input.ReminderTime, Notes = Clean(input.Notes, 2000), ObjectiveId = input.ObjectiveId, EstimatedTimeMinutes = input.EstimatedTimeMinutes, Difficulty = input.Difficulty, UpdatedAt = now };
        if (current is null) await habits.CreateAsync(habit, ct); else if (!await habits.UpdateAsync(user.ClientId.Value, user.Id, habit, ct)) return Result<Habit>.Failure("habit.concurrent_update", "Não foi possível salvar a alteração.");
        await weekDays.ReplaceAsync(habit.Id, input.FrequencyType == HabitFrequencyType.CustomWeekly ? input.SelectedDays : [], ct);
        await audit.LogAsync(current is null ? "habit_created" : "habit_updated", current is null ? "Hábito criado" : "Hábito atualizado", AuditSeverity.Info, user.Id, user.Email, new { habitId = habit.Id }, ct);
        return Result<Habit>.Success(habit);
    }
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
