namespace HabitFlow.Domain;

public sealed record Habit(Guid Id, Guid UserId, string Name, string Color, string? Category, bool IsArchived, DateTime? ArchivedAt, DateTime CreatedAt, DateTime UpdatedAt)
{
    public bool BelongsTo(Guid userId) => UserId == userId;
}
