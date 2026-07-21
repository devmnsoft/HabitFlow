namespace HabitFlow.Domain;

public interface IAuditRepository
{
    Task AddSystemAsync(SystemAuditLog log, CancellationToken ct = default);
    Task<IReadOnlyList<SystemAuditLog>> RecentAsync(CancellationToken ct = default);
}
