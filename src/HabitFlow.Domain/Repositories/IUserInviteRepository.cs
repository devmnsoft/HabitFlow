namespace HabitFlow.Domain;

public interface IUserInviteRepository
{
    Task CreateAsync(UserInvite invite, CancellationToken ct = default);
    Task<UserInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<IReadOnlyList<UserInvite>> GetByClientAsync(Guid clientId, CancellationToken ct = default);
    Task MarkAcceptedAsync(Guid inviteId, Guid acceptedByUserId, CancellationToken ct = default);
    Task MarkExpiredAsync(DateTime utcNow, CancellationToken ct = default);
}
