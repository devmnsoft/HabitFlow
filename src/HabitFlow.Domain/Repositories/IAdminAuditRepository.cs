namespace HabitFlow.Domain;

public interface IAdminAuditRepository
{
    Task AddAsync(AdminAuditLog log, CancellationToken ct = default);
}
