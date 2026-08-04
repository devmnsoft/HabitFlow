namespace HabitFlow.Domain;

public interface IUserSessionRepository
{
    Task CreateAsync(UserSession session, CancellationToken ct = default);
    Task<UserSession?> GetOwnedAsync(Guid id, Guid userId, Guid? clientId, CancellationToken ct = default);
    Task<IReadOnlyList<UserSession>> ListActiveAsync(Guid userId, Guid? clientId, CancellationToken ct = default);
    Task TouchAsync(Guid id, Guid userId, DateTime occurredAt, CancellationToken ct = default);
    Task RevokeAsync(Guid id, Guid userId, string reason, CancellationToken ct = default);
    Task RevokeAllAsync(Guid userId, Guid? exceptSessionId, string reason, CancellationToken ct = default);
    Task IncrementSessionVersionAsync(Guid userId, CancellationToken ct = default);
}
