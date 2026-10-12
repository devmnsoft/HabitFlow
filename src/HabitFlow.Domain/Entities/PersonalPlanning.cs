namespace HabitFlow.Domain;

public static class MonthlyReviewStatuses
{
    public const string Draft = "Draft";
    public const string Completed = "Completed";
}

public sealed record PersonalPriority(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    Guid GoalId,
    int PriorityOrder,
    int? WeeklyAvailableMinutes,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveUntil,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record PersonalPriorityDetails(
    PersonalPriority Priority,
    UserGoal Goal,
    IReadOnlyList<Habit> LinkedHabits);

public sealed record DismissedNextStep(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    string SuggestionKey,
    DateOnly DismissedForDate,
    DateTime CreatedAt);

public sealed record MonthlyReview(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string Status,
    string? Reflection,
    string? SelectedNextStep,
    string? ConfirmedPlanCode,
    string IdempotencyKey,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt);
