using System.Data;

namespace HabitFlow.Infrastructure;

public sealed class UnitOfWork(DbConnectionFactory factory)
{
    public Task<IDbConnection> OpenAsync(CancellationToken ct = default) => factory.OpenAsync(ct);
}
