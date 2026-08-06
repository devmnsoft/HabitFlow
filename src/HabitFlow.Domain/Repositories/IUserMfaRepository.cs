namespace HabitFlow.Domain;

public interface IUserMfaRepository
{
    Task<UserMfaSetting?> GetAsync(Guid userId, Guid? clientId, CancellationToken ct = default);
    Task SavePendingAsync(Guid userId, Guid? clientId, string protectedSecret, DateTime createdAt, CancellationToken ct = default);
    Task<bool> EnableAsync(Guid userId, Guid? clientId, long timeStep, DateTime enabledAt, CancellationToken ct = default);
    Task<bool> AcceptTimeStepAsync(Guid userId, Guid? clientId, long timeStep, CancellationToken ct = default);
    Task DisableAsync(Guid userId, Guid? clientId, DateTime occurredAt, CancellationToken ct = default);
    Task ReplaceRecoveryCodesAsync(Guid userId, Guid? clientId, IReadOnlyCollection<string> hashes, DateTime createdAt, CancellationToken ct = default);
    Task<bool> ConsumeRecoveryCodeAsync(Guid userId, Guid? clientId, string hash, DateTime usedAt, CancellationToken ct = default);
    Task<UserMfaChallenge> CreateChallengeAsync(Guid userId, Guid? clientId, DateTime expiresAt, CancellationToken ct = default);
    Task<UserMfaChallenge?> GetChallengeAsync(Guid id, Guid userId, Guid? clientId, CancellationToken ct = default);
    Task RegisterChallengeFailureAsync(Guid id, Guid userId, Guid? clientId, CancellationToken ct = default);
    Task VerifyChallengeAsync(Guid id, Guid userId, Guid? clientId, DateTime verifiedAt, CancellationToken ct = default);
    Task AddSecurityEventAsync(Guid userId, Guid? clientId, string eventType, DateTime occurredAt, CancellationToken ct = default);
}
