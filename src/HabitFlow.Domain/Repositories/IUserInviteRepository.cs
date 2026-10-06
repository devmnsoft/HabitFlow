namespace HabitFlow.Domain;

public interface IUserInviteRepository
{
    Task LockClientAsync(Guid clientId, CancellationToken ct = default);
    Task CreateAsync(UserInvite invite, CancellationToken ct = default);
    Task<UserInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<UserInvite?> GetByIdAsync(Guid inviteId, CancellationToken ct = default);
    Task<UserInvite?> GetPendingByClientAndEmailAsync(Guid clientId, string normalizedEmail, CancellationToken ct = default);
    Task<int> GetOccupiedSlotsAsync(Guid clientId, DateTime utcNow, CancellationToken ct = default);
    Task<IReadOnlyList<UserInvite>> GetByClientAsync(Guid clientId, CancellationToken ct = default);
    Task<IReadOnlyList<ClientInviteSummary>> GetSummariesByClientAsync(Guid clientId, CancellationToken ct = default);
    Task<bool> MarkAcceptedAsync(Guid inviteId, Guid acceptedByUserId, DateTime utcNow, CancellationToken ct = default);
    Task<bool> MarkCanceledAsync(Guid inviteId, DateTime utcNow, CancellationToken ct = default);
    Task<bool> RotateTokenAsync(Guid inviteId, string tokenHash, DateTime expiresAt, DateTime utcNow, CancellationToken ct = default);
    Task MarkExpiredAsync(Guid clientId, DateTime utcNow, CancellationToken ct = default);
}

public sealed record ClientInviteSummary(Guid Id, string Email, string Role, string Status, string? InviterName, DateTime CreatedAt, DateTime ExpiresAt);
