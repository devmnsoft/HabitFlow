namespace HabitFlow.Domain;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByLoginAsync(string normalizedLogin, CancellationToken ct = default) => GetByEmailAsync(normalizedLogin, ct);
    Task<IReadOnlyList<User>> SearchAsync(string? term, CancellationToken ct = default);
    Task CreateAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task UpdatePasswordAndSessionVersionAsync(Guid userId, string passwordHash, CancellationToken ct = default);
    Task IncrementSessionVersionAsync(Guid userId, CancellationToken ct = default);
    Task AddLoginAttemptAsync(LoginAttempt attempt, CancellationToken ct = default);
    Task<int> CountRecentFailedLoginsAsync(string login, DateTime sinceUtc, CancellationToken ct = default) => Task.FromResult(0);
}
