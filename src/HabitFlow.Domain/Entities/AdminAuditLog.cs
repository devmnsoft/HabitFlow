namespace HabitFlow.Domain;

public sealed record AdminAuditLog(Guid Id, Guid? AdminUserId, string? AdminEmail, string Action, Guid? TargetUserId, string? TargetUserEmail, string? Reason, string? Metadata, DateTime CreatedAt);
