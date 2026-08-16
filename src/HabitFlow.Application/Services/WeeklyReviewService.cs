using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record WeeklyReviewHabitResult(Guid HabitId, string Name, string Category, int Scheduled, int Completed,
    int Percentage, string Insight, int? EstimatedTimeMinutes, bool HasReminder, int CurrentStreak);
public sealed record WeeklyReviewCategoryResult(string Name, int Scheduled, int Completed, int Percentage, string Trend);
public sealed record WeeklyReviewGoalResult(Guid GoalId, string Title, string Status, decimal Percentage, bool HasLinkedHabit);
public sealed record WeeklyReviewSuggestion(Guid HabitId, string Title, string Description, string Action);
public sealed record RecoverySuggestion(Guid HabitId, string Reason, string Recommendation);
public sealed record WeeklyReviewResult(DateOnly PeriodStart, DateOnly PeriodEnd, int Scheduled, int Completed, int Percentage,
    string? BestDay, string? WorstDay, string? BestHabit, string? MostForgottenHabit, string? MostActiveGoal, int DaysWithoutActivity,
    IReadOnlyList<WeeklyReviewHabitResult> Habits, IReadOnlyList<WeeklyReviewCategoryResult> Categories,
    IReadOnlyList<WeeklyReviewGoalResult> Goals, IReadOnlyList<RoutineRecommendation> Recommendations,
    IReadOnlyList<WeeklyReviewSuggestion> Suggestions, IReadOnlyList<RecoverySuggestion> Recovery,
    bool IsCompleted, string IdempotencyKey);

public sealed class WeeklyReviewService(IHabitRepository habits, IHabitWeekDayRepository weekDays,
    IHabitCompletionRepository completions, IHabitScheduleExceptionRepository exceptions, IWeeklyReviewRepository reviews,
    IUserGoalRepository goals, IHabitReminderRepository reminders, RoutineRecommendationService recommendations,
    HabitOccurrenceService occurrence, UserTimeZoneService timeZone)
{
    private static readonly string[] CategoryOrder = ["Saúde", "Estudo", "Movimento", "Sono", "Trabalho", "Finanças", "Bem-estar", "Outras"];

    public async Task<WeeklyReviewResult> BuildAsync(Guid clientId, Guid userId, DateOnly periodStart, CancellationToken ct = default)
    {
        if (clientId == Guid.Empty || userId == Guid.Empty) throw new ArgumentException("Conta e pessoa são obrigatórias.");
        var end = periodStart.AddDays(6);
        var source = await habits.ListActiveAsync(clientId, userId, ct);
        var days = await weekDays.ListByHabitsAsync(source.Select(x => x.Id), ct);
        var map = days.ToDictionary(x => x.Key, x => (IReadOnlySet<int>)x.Value.Select(d => d.DayOfWeek).ToHashSet());
        var rows = source.Select(ToProgressRow).ToList();
        var planned = (await occurrence.ListScheduledForPeriodAsync(rows, map, periodStart, end, timeZone.Resolve())).ToList();
        var exceptionList = await exceptions.ListAsync(clientId, userId, periodStart, end, ct);
        planned.RemoveAll(x => exceptionList.Any(e => e.HabitId == x.Habit.Id && e.LocalDate == x.Date && e.Type is HabitScheduleExceptionType.Excused or HabitScheduleExceptionType.Moved));
        foreach (var e in exceptionList.Where(x => x.Type is HabitScheduleExceptionType.Added or HabitScheduleExceptionType.Moved))
        {
            var date = e.Type == HabitScheduleExceptionType.Moved ? e.DestinationDate : e.LocalDate;
            var row = rows.FirstOrDefault(x => x.Id == e.HabitId);
            if (date.HasValue && row is not null && date >= periodStart && date <= end && !planned.Any(x => x.Habit.Id == row.Id && x.Date == date)) planned.Add(new(row, date.Value));
        }
        var completed = (await completions.ListAsync(clientId, userId, periodStart, end, ct)).ToList();
        var reminderHabitIds = (await reminders.ListAsync(clientId, userId, ct: ct)).Where(x => x.IsActive).Select(x => x.HabitId).ToHashSet();
        var habitResults = source.Select(h =>
        {
            var total = planned.Count(x => x.Habit.Id == h.Id);
            var completionDates = completed.Where(x => x.HabitId == h.Id).Select(x => x.CompletedDate).ToHashSet();
            var done = planned.Count(x => x.Habit.Id == h.Id && completionDates.Contains(x.Date));
            var percentage = total == 0 ? 0 : (int)Math.Round(done * 100d / total);
            var streak = 0; var best = 0;
            foreach (var item in planned.Where(x => x.Habit.Id == h.Id).OrderBy(x => x.Date)) { streak = completionDates.Contains(item.Date) ? streak + 1 : 0; best = Math.Max(best, streak); }
            return new WeeklyReviewHabitResult(h.Id, h.Name, NormalizeCategory(h.Category), total, done, percentage,
                percentage >= 70 ? "Seu ritmo esteve consistente." : total >= 3 ? "Um ajuste gentil pode deixar este hábito mais leve." : "Continue observando seu ritmo.",
                h.EstimatedTimeMinutes, reminderHabitIds.Contains(h.Id), best);
        }).Where(x => x.Scheduled > 0).OrderByDescending(x => x.Percentage).ThenBy(x => x.Name).ToList();

        var categoryResults = CategoryOrder.Select(category =>
        {
            var items = habitResults.Where(x => x.Category == category).ToList(); var scheduled = items.Sum(x => x.Scheduled); var done = items.Sum(x => x.Completed);
            var percentage = scheduled == 0 ? 0 : (int)Math.Round(done * 100d / scheduled);
            return new WeeklyReviewCategoryResult(category, scheduled, done, percentage, scheduled == 0 ? "Sem agenda" : percentage >= 70 ? "Ritmo estável" : percentage >= 40 ? "Em construção" : "Pode ficar mais leve");
        }).ToList();
        var goalResults = new List<WeeklyReviewGoalResult>();
        foreach (var goal in await goals.ListAsync(clientId, userId, ct))
        {
            var linked = await goals.ListLinkedHabitsAsync(goal.Id, clientId, userId, ct);
            var percentage = goal.TargetValue <= 0 ? 0 : Math.Round(Math.Clamp(goal.CurrentValue * 100m / goal.TargetValue, 0, 100), 1);
            goalResults.Add(new(goal.Id, goal.Title, goal.Status, percentage, linked.Count > 0));
        }
        var totalPlanned = planned.Count;
        var totalDone = completed.Count(x => planned.Any(p => p.Habit.Id == x.HabitId && p.Date == x.CompletedDate));
        var overall = totalPlanned == 0 ? 0 : (int)Math.Round(totalDone * 100d / totalPlanned);
        var byDay = Enumerable.Range(0, 7).Select(i => periodStart.AddDays(i)).Select(date => new { Date = date, Count = completed.Count(x => x.CompletedDate == date) }).ToList();
        var stored = await reviews.GetAsync(clientId, userId, periodStart, ct);
        var routineRecommendations = recommendations.Build(new(habitResults, goalResults, overall, source.Count));
        var suggestions = habitResults.Where(x => x.Scheduled >= 3 && x.Percentage < 50).Select(x => new WeeklyReviewSuggestion(x.HabitId, "Ajuste consciente", $"{x.Name} pode ficar mais simples na próxima semana.", "Reduzir frequência")).ToList();
        var recovery = habitResults.Where(x => x.Scheduled >= 5 && x.Percentage < 40).Select(x => new RecoverySuggestion(x.HabitId, "Há espaço para um apoio", "Experimente reduzir a frequência ou escolher um horário mais confortável.")).ToList();
        return new(periodStart, end, totalPlanned, totalDone, overall,
            byDay.OrderByDescending(x => x.Count).ThenBy(x => x.Date).FirstOrDefault(x => x.Count > 0)?.Date.ToString("dddd"),
            byDay.Where(x => planned.Any(p => p.Date == x.Date)).OrderBy(x => x.Count).ThenBy(x => x.Date).FirstOrDefault()?.Date.ToString("dddd"),
            habitResults.FirstOrDefault()?.Name, habitResults.OrderBy(x => x.Percentage).ThenByDescending(x => x.Scheduled).FirstOrDefault()?.Name,
            goalResults.OrderByDescending(x => x.Percentage).FirstOrDefault()?.Title, byDay.Count(x => x.Count == 0), habitResults, categoryResults,
            goalResults.OrderByDescending(x => x.Percentage).ToList(), routineRecommendations, suggestions, recovery,
            stored?.Status == "Completed", stored?.IdempotencyKey ?? Guid.NewGuid().ToString("N"));
    }

    private static string NormalizeCategory(string? category) => CategoryOrder.FirstOrDefault(x => string.Equals(x, category?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? "Outras";
    private static ProgressHabitRow ToProgressRow(Habit habit) => new() { Id = habit.Id, Name = habit.Name, Category = habit.Category,
        CreatedAt = habit.StartDate?.ToDateTime(TimeOnly.MinValue) ?? habit.CreatedAt, ArchivedAt = habit.ArchivedAt,
        IsArchived = habit.IsArchived, FrequencyTypeCode = habit.FrequencyType.ToString(), ReminderTime = habit.ReminderTime };
}

public sealed class CompleteWeeklyReviewUseCase(IWeeklyReviewRepository reviews, TimeProvider clock, AuditService audit)
{
    public async Task<WeeklyReview> ExecuteAsync(Guid clientId, Guid userId, DateOnly start, string idempotencyKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("Chave de idempotência obrigatória.");
        var now = clock.GetUtcNow();
        var result = await reviews.CompleteAsync(new(Guid.NewGuid(), clientId, userId, start, start.AddDays(6), "Completed", idempotencyKey, 1, now, now), ct);
        await audit.LogAsync("weekly_review.completed", "Revisão semanal concluída", AuditSeverity.Info, userId, metadata: new { clientId, periodStart = start, idempotencyKey }, ct: ct);
        return result;
    }
}
