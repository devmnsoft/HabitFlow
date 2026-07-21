namespace HabitFlow.Domain;

public sealed record SystemAuditLog(Guid Id, Guid? UserId, string? UserEmail, AuditSeverity Severity, string Source, string Action, string Message, string? Metadata, string? ErrorCode, string? ErrorFingerprint, DateTime CreatedAt, bool ReadByAdmin);
