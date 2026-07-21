namespace HabitFlow.Domain;

public sealed record SupportTicket(Guid Id, Guid UserId, string Protocol, string Type, TicketStatus Status, string Priority, string Title, string? Description, string? Source, DateTime CreatedAt, DateTime UpdatedAt, DateTime? ResolvedAt);
