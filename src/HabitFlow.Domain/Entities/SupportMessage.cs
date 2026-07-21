namespace HabitFlow.Domain;

public sealed record SupportMessage(Guid Id, Guid TicketId, Guid? UserId, string Role, string Message, bool IsSensitiveBlocked, DateTime CreatedAt);
