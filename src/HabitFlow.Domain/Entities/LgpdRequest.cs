namespace HabitFlow.Domain;

public sealed record LgpdRequest(Guid Id, Guid UserId, string Protocol, LgpdRequestType Type, LgpdRequestStatus Status, string? Notes, string? RejectionReason, Guid? HandledBy, DateTime CreatedAt, DateTime UpdatedAt, DateTime? CompletedAt);
