namespace HabitFlow.Domain;

public interface IUserReportRepository
{
    Task CreateAsync(UserReport report, CancellationToken ct = default);
    Task<IReadOnlyList<UserReport>> ListByUserAsync(Guid clientId, Guid userId, CancellationToken ct = default);
}
