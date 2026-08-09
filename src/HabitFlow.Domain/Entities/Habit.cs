namespace HabitFlow.Domain;

public sealed record Habit(
    Guid Id,
    Guid UserId,
    string Name,
    string Color,
    string? Category,
    bool IsArchived,
    DateTime? ArchivedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    HabitFrequencyType FrequencyType = HabitFrequencyType.Daily,
    int? TargetPerWeek = null,
    TimeOnly? ReminderTime = null,
    string? Notes = null,
    int SortOrder = 0,
    Guid? ClientId = null,
    Guid? SourceTemplateId = null,
    Guid? SourceCollectionId = null,
    Guid? ObjectiveId = null,
    string? IconCode = null,
    HabitDifficulty? Difficulty = null,
    int? EstimatedTimeMinutes = null,
    DateOnly? StartDate = null,
    int? TemplateContentVersion = null,
    bool IsTemplateVariation = false,
    Guid? TemplateIdempotencyKey = null,
    bool IsPaused = false,
    DateTime? PausedAt = null)
{
    public bool BelongsTo(Guid userId) => UserId == userId;

    public bool HasValidWeeklyTarget() => TargetPerWeek is null or (>= 1 and <= 7);
}
