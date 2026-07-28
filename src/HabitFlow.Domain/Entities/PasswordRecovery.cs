namespace HabitFlow.Domain;

public sealed record PasswordResetToken(Guid Id, Guid UserId, string TokenHash, DateTime ExpiresAt,
    DateTime? UsedAt, DateTime? RevokedAt, DateTime CreatedAt);

public sealed record PasswordResetRequest(string Email, string IpHash, string? UserAgentHash, string CorrelationId);
public sealed record PasswordResetTokenValidation(bool IsValid, Guid? UserId = null);
public sealed record PasswordResetResult(bool Succeeded, string? Error = null);

public enum TransactionalEmailStatus { Pending, Processing, Sent, Failed, DeadLetter }

public sealed record TransactionalEmailMessage(Guid Id, Guid? ClientId, Guid? UserId, string TemplateCode,
    string Recipient, string Subject, string PayloadJson, TransactionalEmailStatus Status,
    string IdempotencyKey, int Attempts, DateTime NextAttemptAt, DateTime CreatedAt);
