namespace HabitFlow.Domain;

public enum UserChallengeStatus { Active, Completed, Abandoned, Expired }

public sealed record UserChallenge(Guid Id, Guid ClientId, Guid UserId, Guid HabitId,
    string Name, string Description, int DurationDays, DateOnly StartDate, DateOnly EndDate,
    UserChallengeStatus Status, DateTime CreatedAt, DateTime UpdatedAt, DateTime? CompletedAt,
    int ProgressDays = 0)
{
    public int ProgressPercent => Math.Min(100, ProgressDays * 100 / DurationDays);
}

public interface IUserChallengeRepository
{
    Task<IReadOnlyList<UserChallenge>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<UserChallenge?> GetActiveAsync(Guid clientId, Guid userId, Guid habitId, CancellationToken ct = default);
    Task<UserChallenge?> GetAsync(Guid clientId, Guid userId, Guid challengeId, CancellationToken ct = default);
    Task CreateAsync(UserChallenge challenge, CancellationToken ct = default);
    Task<bool> SetStatusAsync(Guid clientId, Guid userId, Guid challengeId, UserChallengeStatus status, DateTime now, CancellationToken ct = default);
}
