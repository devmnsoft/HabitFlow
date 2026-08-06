namespace HabitFlow.Domain;

public sealed record UserMfaSetting(Guid UserId, Guid? ClientId, string ProtectedSecret, bool IsEnabled,
    long? LastAcceptedTimeStep, DateTime CreatedAt, DateTime? EnabledAt);

public sealed record UserMfaChallenge(Guid Id, Guid UserId, Guid? ClientId, int FailedAttempts,
    DateTime ExpiresAt, DateTime? VerifiedAt);

