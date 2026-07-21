namespace HabitFlow.Domain;

public sealed record Notification(Guid Id, Guid UserId, string Type, string Title, string Message, bool IsRead, string? RelatedEntityType, Guid? RelatedEntityId, DateTime CreatedAt, DateTime? ReadAt);
