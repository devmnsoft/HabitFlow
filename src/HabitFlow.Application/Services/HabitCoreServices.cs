using System.Text.RegularExpressions;
using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed record HabitListQuery(string? Search = null, string? Category = null, string Status = "active", string Sort = "newest", int Page = 1, int PageSize = 12);
public sealed record HabitListItem(Habit Habit, int CompletedCount, int ScheduledCount, decimal Consistency);
public sealed record HabitListViewModel(IReadOnlyList<HabitListItem> Items, IReadOnlyList<string> Categories, HabitListQuery Query, int Total, int TotalPages);
public sealed record HabitEditorViewModel(Guid? Id, string Name, string Color, string? Category, string IconCode,
    HabitFrequencyType FrequencyType, int? TargetPerWeek, TimeOnly? ReminderTime, string? Notes,
    IReadOnlyList<int>? SelectedDays, Guid? ObjectiveId, int? EstimatedTimeMinutes, HabitDifficulty? Difficulty,
    DateOnly? StartDate = null);
public sealed record GoalOptionViewModel(Guid Id, string Title, string Status, string? Description, decimal ProgressPercentage);
public sealed record HabitEditorPageViewModel(HabitEditorViewModel Editor, IReadOnlyList<string> CategorySuggestions,
    IReadOnlyList<GoalOptionViewModel> GoalOptions);
public sealed record HabitCalendarDay(DateOnly Date, bool Scheduled, bool Completed);
public sealed record HabitTimelineItem(DateTime At, string Title, string Description);
public sealed record HabitDetailsViewModel(Habit Habit, int CompletedCount, int ScheduledCount, decimal Consistency,
    int CurrentStreak, int BestStreak, IReadOnlyList<HabitCalendarDay> Calendar, IReadOnlyList<HabitTimelineItem> Timeline);

public static class HabitListPolicy
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase) { "active", "paused", "archived", "all" };
    private static readonly HashSet<string> AllowedSorts = new(StringComparer.OrdinalIgnoreCase) { "newest", "oldest", "updated", "name" };

    public static HabitListQuery Normalize(HabitListQuery query)
    {
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var category = string.IsNullOrWhiteSpace(query.Category) ? null : query.Category.Trim();
        return query with
        {
            Search = search?[..Math.Min(search.Length, 120)],
            Category = category?[..Math.Min(category.Length, 80)],
            Status = AllowedStatuses.Contains(query.Status ?? "") ? query.Status.ToLowerInvariant() : "active",
            Sort = AllowedSorts.Contains(query.Sort ?? "") ? query.Sort.ToLowerInvariant() : "newest",
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 6, 48)
        };
    }

    public static decimal Consistency(int completed, int scheduled) => scheduled <= 0
        ? 0
        : Math.Round(Math.Clamp(completed * 100m / scheduled, 0, 100), 1);
}

public sealed class HabitQueryService(IHabitRepository habits, IHabitCompletionRepository completions, IHabitWeekDayRepository weekDays,
    HabitOccurrenceService occurrences, UserTimeZoneService clock)
{
    public async Task<HabitListViewModel> SearchAsync(Guid clientId, Guid userId, HabitListQuery query, CancellationToken ct = default)
    {
        query = HabitListPolicy.Normalize(query);
        var all = await habits.ListAsync(clientId, userId, ct);
        var scoped = all.Where(x => MatchesStatus(x, query.Status));
        if (!string.IsNullOrWhiteSpace(query.Search)) scoped = scoped.Where(x => x.Name.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase) || (x.Category?.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase) ?? false));
        if (!string.IsNullOrWhiteSpace(query.Category)) scoped = scoped.Where(x => string.Equals(x.Category, query.Category, StringComparison.OrdinalIgnoreCase));
        scoped = query.Sort switch { "name" => scoped.OrderBy(x => x.Name), "oldest" => scoped.OrderBy(x => x.CreatedAt), "updated" => scoped.OrderByDescending(x => x.UpdatedAt), _ => scoped.OrderByDescending(x => x.CreatedAt) };
        var total = scoped.Count(); var pageSize = query.PageSize; var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        var page = totalPages == 0 ? 1 : Math.Min(query.Page, totalPages);
        var selected = scoped.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var from = clock.Today().AddDays(-27); var to = clock.Today();
        var done = await completions.ListAsync(clientId, userId, from, to, ct);
        var dayMap = await weekDays.ListByHabitsAsync(selected.Select(x => x.Id).ToArray(), ct);
        var configured = dayMap.ToDictionary(x => x.Key, x => (IReadOnlySet<int>)x.Value.Select(y => y.DayOfWeek).ToHashSet());
        var progressRows = selected.Select(ToProgressRow).ToList();
        var planned = await occurrences.ListScheduledForPeriodAsync(progressRows, configured, from, to, clock.Resolve());
        var scheduledDates = planned.GroupBy(x => x.Habit.Id).ToDictionary(x => x.Key, x => x.Select(y => y.Date).ToHashSet());
        var items = selected.Select(h =>
        {
            var dates = scheduledDates.GetValueOrDefault(h.Id) ?? [];
            var completed = done.Count(x => x.HabitId == h.Id && dates.Contains(x.CompletedDate));
            return new HabitListItem(h, completed, dates.Count, HabitListPolicy.Consistency(completed, dates.Count));
        }).ToList();
        return new(items, all.Select(x => x.Category).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).Order().Cast<string>().ToList(), query with { Page = page }, total, totalPages);
    }

    public async Task<HabitDetailsViewModel?> DetailAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default)
    {
        var habit = await habits.GetAsync(clientId, userId, habitId, ct); if (habit is null) return null;
        var from = clock.Today().AddDays(-83); var to = clock.Today(); var done = await completions.ListAsync(clientId, userId, from, to, ct);
        var dayMap = await weekDays.ListByHabitsAsync([habit.Id], ct);
        var configured = (dayMap.TryGetValue(habit.Id, out var configuredDays) ? configuredDays : Array.Empty<HabitWeekDay>()).Select(x => x.DayOfWeek).ToHashSet();
        var planned = await occurrences.ListScheduledForPeriodAsync([ToProgressRow(habit)], new Dictionary<Guid, IReadOnlySet<int>> { [habit.Id] = configured }, from, to, clock.Resolve());
        var completedDates = done.Where(x => x.HabitId == habit.Id).Select(x => x.CompletedDate).ToHashSet();
        var calendar = Enumerable.Range(0, 84).Select(i => from.AddDays(i)).Select(d => new HabitCalendarDay(d, planned.Any(x => x.Date == d), completedDates.Contains(d))).ToList();
        var streaks = CalculateStreak(calendar, to);
        var timeline = done.Where(x => x.HabitId == habit.Id).OrderByDescending(x => x.CompletedDate).Take(20).Select(x => new HabitTimelineItem(x.CreatedAt, "Hábito concluído", x.CompletedDate.ToString("dd/MM/yyyy"))).ToList();
        timeline.Add(new(habit.CreatedAt, "Hábito criado", "A jornada começou aqui."));
        return new(habit, completedDates.Count, planned.Count, planned.Count == 0 ? 0 : Math.Round(completedDates.Count * 100m / planned.Count, 1), streaks.Current, streaks.Best, calendar, timeline.OrderByDescending(x => x.At).ToList());
    }
    private static ProgressHabitRow ToProgressRow(Habit habit) => new()
    {
        Id = habit.Id,
        Name = habit.Name,
        Category = habit.Category,
        IsArchived = habit.IsArchived,
        ArchivedAt = habit.ArchivedAt,
        CreatedAt = habit.StartDate?.ToDateTime(TimeOnly.MinValue) ?? habit.CreatedAt,
        FrequencyTypeCode = habit.FrequencyType.ToString(),
        ReminderTime = habit.ReminderTime
    };
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

public sealed class HabitEditorService(IHabitRepository habits, IHabitWeekDayRepository weekDays, HabitScheduleNormalizer scheduleNormalizer, PlanEntitlementService entitlements, AuditService audit, IUserGoalRepository goals, UserTimeZoneService clock, IUnitOfWork unitOfWork)
{
    private static readonly string[] DefaultCategories = ["Saúde", "Movimento", "Estudo", "Trabalho", "Casa", "Finanças", "Sono", "Alimentação", "Leitura", "Espiritualidade", "Bem-estar"];
    private static readonly Regex HexColor = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);
    public async Task<HabitEditorViewModel?> LoadAsync(Guid clientId, Guid userId, Guid id, CancellationToken ct = default)
    {
        var h = await habits.GetAsync(clientId, userId, id, ct); if (h is null) return null;
        var days = await weekDays.ListByHabitAsync(id, ct);
        return new(h.Id, h.Name, h.Color, h.Category, h.IconCode ?? "check-circle", h.FrequencyType, h.TargetPerWeek, h.ReminderTime, h.Notes, days.Select(x => x.DayOfWeek).Distinct().Order().ToList(), h.ObjectiveId, h.EstimatedTimeMinutes, h.Difficulty, h.StartDate);
    }
    public async Task<HabitEditorPageViewModel> PreparePageAsync(Guid clientId, Guid userId, HabitEditorViewModel editor, CancellationToken ct = default)
    {
        var categories = (await habits.ListAsync(clientId, userId, ct)).Select(x => x.Category).Concat(DefaultCategories)
            .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x!.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).Take(30).ToList();
        var options = (await goals.ListAsync(clientId, userId, ct)).Where(x => x.Status is "Active" or "Paused" || x.Id == editor.ObjectiveId)
            .OrderBy(x => x.Status == "Active" ? 0 : 1).ThenByDescending(x => x.CreatedAt).ThenBy(x => x.Title)
            .Select(x => new GoalOptionViewModel(x.Id, x.Title, x.Status, x.Description,
                x.TargetValue <= 0 ? 0 : Math.Round(Math.Clamp(x.CurrentValue * 100m / x.TargetValue, 0, 100), 1))).ToList();
        return new(editor, categories, options);
    }
    public async Task<Result<Habit>> SaveAsync(User user, HabitEditorViewModel input, CancellationToken ct = default)
    {
        if (!user.ClientId.HasValue) return Result<Habit>.Failure("client.required", "Conta inválida.");
        var name = input.Name?.Trim() ?? "";
        var color = string.IsNullOrWhiteSpace(input.Color) ? "#10B981" : input.Color.Trim();
        var icon = Clean(input.IconCode, 40) ?? "check-circle";
        if (name.Length is 0 or > 120) return Result<Habit>.Failure("habit.name", "Informe um nome com até 120 caracteres.");
        if (!HexColor.IsMatch(color)) return Result<Habit>.Failure("habit.color", "Selecione uma cor válida.");
        if (input.EstimatedTimeMinutes is < 1 or > 1440) return Result<Habit>.Failure("habit.duration", "A duração deve ficar entre 1 e 1440 minutos.");
        if (input.Difficulty.HasValue && !Enum.IsDefined(input.Difficulty.Value)) return Result<Habit>.Failure("habit.difficulty", "Escolha uma dificuldade válida.");
        if (input.ObjectiveId == Guid.Empty) return Result<Habit>.Failure("habit.objective_not_found", "Objetivo não encontrado.");
        var schedule = scheduleNormalizer.Normalize(new(input.FrequencyType, input.TargetPerWeek, input.SelectedDays));
        if (schedule.IsFailure) return Result<Habit>.Failure(schedule.Error.Code, schedule.Error.Message);
        var normalized = schedule.Value!;
        if (input.ObjectiveId.HasValue && await goals.GetAsync(input.ObjectiveId.Value, user.ClientId.Value, user.Id, ct) is null)
            return Result<Habit>.Failure("habit.objective_not_found", "Objetivo não encontrado.");
        var current = input.Id.HasValue ? await habits.GetAsync(user.ClientId.Value, user.Id, input.Id.Value, ct) : null;
        if (input.Id.HasValue && current is null) return Result<Habit>.Failure("habit.not_found", "Hábito não encontrado.");
        if (current is null)
        {
            var count = await habits.CountActiveAsync(user.ClientId.Value, user.Id, ct);
            var limit = await entitlements.GetIntegerFeatureAsync(user.Id, PlanFeatureCodes.ActiveHabitsLimit, ct);
            if (limit is >= 0 && count >= limit)
            {
                await audit.LogAsync("plan.limit_reached", "Limite de hábitos ativos atingido", AuditSeverity.Warning,
                    user.Id, user.Email, new { feature = PlanFeatureCodes.ActiveHabitsLimit, limit }, ct);
                return Result<Habit>.Failure("plan.habit_limit",
                    $"Você chegou ao limite de {limit} hábitos ativos do seu plano. Veja os planos para criar outro hábito.");
            }
        }
        var now = DateTime.UtcNow;
        var startDate = current?.StartDate ?? input.StartDate ?? clock.Today();
        var habit = (current ?? new Habit(Guid.NewGuid(), user.Id, name, color, Clean(input.Category, 80), false, null, now, now, ClientId: user.ClientId, StartDate: startDate)) with { Name = name, Color = color, Category = Clean(input.Category, 80), IconCode = icon, FrequencyType = normalized.FrequencyType, TargetPerWeek = normalized.TargetPerWeek, ReminderTime = input.ReminderTime, Notes = Clean(input.Notes, 2000), ObjectiveId = input.ObjectiveId, EstimatedTimeMinutes = input.EstimatedTimeMinutes, Difficulty = input.Difficulty, StartDate = startDate, UpdatedAt = now };
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            if (current is null) await habits.CreateAsync(habit, ct); else if (!await habits.UpdateAsync(user.ClientId.Value, user.Id, habit, ct)) { await unitOfWork.RollbackAsync(ct); return Result<Habit>.Failure("habit.concurrent_update", "Não foi possível salvar a alteração."); }
            await weekDays.ReplaceAsync(habit.Id, normalized.FrequencyType == HabitFrequencyType.CustomWeekly ? normalized.SelectedDays : [], ct);
            await audit.LogAsync(current is null ? "habit.created" : "habit.updated", current is null ? "Hábito criado" : "Hábito atualizado", AuditSeverity.Info, user.Id, user.Email, new { habitId = habit.Id }, ct);
            await unitOfWork.CommitAsync(ct);
            return Result<Habit>.Success(habit);
        }
        catch
        {
            await unitOfWork.RollbackAsync(ct);
            throw;
        }
    }
    private static string? Clean(string? value, int max) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, max)];
}
