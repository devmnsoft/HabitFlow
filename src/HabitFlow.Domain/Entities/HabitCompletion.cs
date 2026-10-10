namespace HabitFlow.Domain;

public sealed record HabitCompletion(Guid Id, Guid HabitId, Guid UserId, DateOnly CompletedDate, DateTime CreatedAt,
    decimal? RecordedQuantity = null, string? RecordedUnit = null, string CompletionMode = "Binary");
