namespace HabitFlow.Domain;

public enum HabitScheduleExceptionType { Excused, Moved, Added }

public sealed record HabitScheduleException(
    Guid Id, Guid ClientId, Guid UserId, Guid HabitId, DateOnly LocalDate,
    HabitScheduleExceptionType Type, DateOnly? DestinationDate, string? Reason,
    int Version, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record DailyRoutineOverride(
    Guid Id, Guid ClientId, Guid UserId, Guid HabitId, DateOnly LocalDate,
    TimeOnly? PreferredTime, int SortOrder, int Version,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public sealed record WeeklyReview(
    Guid Id, Guid ClientId, Guid UserId, DateOnly PeriodStart, DateOnly PeriodEnd,
    string Status, string IdempotencyKey, int Version,
    DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);
