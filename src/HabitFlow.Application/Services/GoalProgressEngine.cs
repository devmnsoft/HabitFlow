using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class GoalProgressCalculator
{
    public GoalProgressResult Calculate(UserGoal goal, GoalProgressContext context, bool undo)
    {
        if (!GoalTargetTypes.TryParse(goal.TargetType, out var targetType))
            throw new InvalidOperationException($"Tipo de objetivo inválido: {goal.TargetType}.");

        var value = targetType switch
        {
            GoalTargetType.HabitCompletions => context.ProgressSnapshot.HabitCompletions,
            GoalTargetType.ActiveDays => context.ProgressSnapshot.ActiveDays,
            GoalTargetType.StreakDays => context.ProgressSnapshot.CurrentStreak,
            GoalTargetType.WeeklyCompletions => context.ProgressSnapshot.WeeklyCompletions,
            GoalTargetType.Custom => goal.CurrentValue,
            _ => throw new ArgumentOutOfRangeException()
        };

        var completedNow = goal.Status == "Active" && goal.CompletedAt is null && value >= goal.TargetValue;
        var completedAt = goal.CompletedAt ?? (completedNow ? DateTime.UtcNow : null);
        var status = completedNow ? "Completed" : goal.Status;
        var percentage = goal.TargetValue <= 0 ? 0 : Math.Min(100m, Math.Round(value * 100m / goal.TargetValue, 1));
        var message = completedNow
            ? $"Objetivo concluído: {goal.Title}."
            : undo ? $"Progresso de {goal.Title} corrigido após desfazer a conclusão."
            : $"Progresso de {goal.Title} atualizado para {value} de {goal.TargetValue}.";
        return new(goal.Id, goal.Title, goal.CurrentValue, value, goal.TargetValue, percentage, status, completedNow, completedAt, message);
    }
}

public sealed class GoalHabitLinkService(IUserGoalRepository goals)
{
    public Task LinkAsync(Guid clientId, Guid userId, Guid goalId, Guid habitId, CancellationToken ct = default) =>
        goals.LinkHabitAsync(goalId, habitId, clientId, userId, ct);

    public Task UnlinkAsync(Guid clientId, Guid userId, Guid goalId, Guid habitId, CancellationToken ct = default) =>
        goals.UnlinkHabitAsync(goalId, habitId, clientId, userId, ct);
}

public sealed class GoalProgressEngine(IGoalProgressRepository repository, GoalProgressCalculator calculator)
{
    public async Task<IReadOnlyList<GoalProgressResult>> RecalculateAsync(
        Guid clientId, Guid userId, Guid habitId, DateOnly localDate, Guid? completionId,
        string idempotencyKey, string correlationId, int canonicalCurrentStreak, bool undo,
        CancellationToken ct = default)
    {
        var results = new List<GoalProgressResult>();
        foreach (var goal in await repository.ListRelatedAsync(clientId, userId, habitId, ct))
        {
            if (goal.Status is "Paused" or "Cancelled") continue;
            if (!GoalTargetTypes.TryParse(goal.TargetType, out var type) || type == GoalTargetType.Custom) continue;

            var periodEnd = goal.EndDate ?? localDate;
            var snapshot = await repository.BuildSnapshotAsync(goal, habitId, localDate, canonicalCurrentStreak, ct);
            var scopedKey = $"{idempotencyKey}:{goal.Id:N}:{(undo ? "undo" : "complete")}";
            var context = new GoalProgressContext(clientId, userId, goal.Id, habitId, localDate,
                goal.StartDate, periodEnd, completionId, scopedKey, correlationId, snapshot);
            var result = calculator.Calculate(goal, context, undo);
            var progressEvent = new GoalProgressEvent(Guid.NewGuid(), clientId, userId, goal.Id,
                undo ? "CompletionUndone" : "CompletionCreated", result.PreviousValue, result.CurrentValue,
                localDate, completionId, scopedKey, correlationId, DateTime.UtcNow, "{}");
            if (await repository.ApplyAsync(goal, result, progressEvent, ct)) results.Add(result);
        }
        return results;
    }
}
