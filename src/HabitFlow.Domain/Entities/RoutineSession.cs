namespace HabitFlow.Domain;

public static class RoutineSessionStatuses
{
    public const string Prepared = "Prepared";
    public const string InProgress = "InProgress";
    public const string Paused = "Paused";
    public const string Closed = "Closed";
    public const string Canceled = "Canceled";

    public static readonly IReadOnlySet<string> Terminal = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Closed, Canceled
    };
}

public static class RoutineSessionItemStatuses
{
    public const string Pending = "Pending";
    public const string MinimumReached = "MinimumReached";
    public const string Completed = "Completed";
    public const string Skipped = "Skipped";
}

public sealed record RoutineSession(
    Guid Id,
    Guid ClientId,
    Guid UserId,
    string Name,
    Guid? SourceRoutineId,
    string SourceRoutineType,
    string Status,
    DateOnly LocalDate,
    int? AvailableMinutes,
    string? ActiveAccountSessionId,
    string? Reflection,
    string? ClosureSummary,
    string IdempotencyKey,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? StartedAt,
    DateTime? PausedAt,
    DateTime? ResumedAt,
    DateTime? ClosedAt,
    DateTime? CanceledAt);

public sealed record RoutineSessionItem(
    Guid Id,
    Guid SessionId,
    Guid HabitId,
    string HabitNameSnapshot,
    string? HabitCategorySnapshot,
    int SortOrder,
    string Status,
    decimal? PlannedQuantity,
    string? PlannedUnit,
    decimal? MinimumQuantity,
    string? MinimumName,
    decimal? RecordedQuantity,
    string? RecordedUnit,
    int? EstimatedMinutes,
    int? EffectiveMinutes,
    bool Skipped,
    string? SkipReason,
    Guid? CompletionId,
    DateTime? CompletedAt,
    DateTime? UpdatedAt);

public sealed record RoutineSessionTransition(
    Guid Id,
    Guid SessionId,
    string Command,
    string FromStatus,
    string ToStatus,
    string IdempotencyKey,
    string CorrelationId,
    DateTime CreatedAt);

public sealed record RoutineSessionDetails(RoutineSession Session, IReadOnlyList<RoutineSessionItem> Items);

public sealed record RoutineSessionSummary(
    Guid Id,
    string Name,
    string Status,
    DateOnly LocalDate,
    int TotalItems,
    int CompletedItems,
    int MinimumItems,
    int SkippedItems,
    int PendingItems,
    int? AvailableMinutes,
    int? EffectiveMinutes,
    DateTime UpdatedAt);
