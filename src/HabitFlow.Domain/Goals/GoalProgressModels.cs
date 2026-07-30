namespace HabitFlow.Domain;

public sealed record GoalProgressContext(
    Guid ClientId,
    Guid UserId,
    Guid GoalId,
    Guid TriggerHabitId,
    DateOnly LocalDate,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    Guid? SourceCompletionId,
    string IdempotencyKey,
    string CorrelationId,
    GoalProgressSnapshot ProgressSnapshot);

public sealed record GoalProgressSnapshot(
    int HabitCompletions,
    int ActiveDays,
    int CurrentStreak,
    int WeeklyCompletions);

public sealed record GoalProgressResult(
    Guid GoalId,
    string Title,
    decimal PreviousValue,
    decimal CurrentValue,
    decimal TargetValue,
    decimal Percentage,
    string Status,
    bool CompletedNow,
    DateTime? CompletedAtUtc,
    string Message);

public sealed record GoalProgressEvent(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    Guid GoalId,
    string EventType,
    decimal PreviousValue,
    decimal NewValue,
    DateOnly LocalDate,
    Guid? SourceCompletionId,
    string IdempotencyKey,
    string CorrelationId,
    DateTime CreatedAtUtc,
    string MetadataJson);

public interface IGoalProgressRepository
{
    Task<IReadOnlyList<UserGoal>> ListRelatedAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default);
    Task<GoalProgressSnapshot> BuildSnapshotAsync(UserGoal goal, Guid triggerHabitId, DateOnly localDate, int currentStreak, CancellationToken ct = default);
    Task<bool> ApplyAsync(UserGoal goal, GoalProgressResult result, GoalProgressEvent progressEvent, CancellationToken ct = default);
}
