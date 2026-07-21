namespace HabitFlow.Domain;

public sealed record AdminUserNote(Guid Id, Guid UserId, Guid AdminUserId, string AdminEmail, string Note, DateTime CreatedAt);
