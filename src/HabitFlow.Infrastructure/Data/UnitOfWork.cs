using System.Data;
using HabitFlow.Application;

namespace HabitFlow.Infrastructure;

public sealed class UnitOfWork(DbConnectionFactory factory) : IUnitOfWork, IAsyncDisposable
{
    private IDbConnection? _connection;
    private IDbTransaction? _transaction;
    internal static readonly AsyncLocal<UnitOfWork?> Current = new();
    internal IDbConnection? Connection => _connection;
    internal IDbTransaction? Transaction => _transaction;

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is not null) return;
        _connection = await factory.OpenAsync(ct);
        _transaction = _connection.BeginTransaction();
        Current.Value = this;
    }

    public Task CommitAsync(CancellationToken ct = default)
    {
        _transaction?.Commit();
        Clear();
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken ct = default)
    {
        _transaction?.Rollback();
        Clear();
        return Task.CompletedTask;
    }

    private void Clear()
    {
        _transaction?.Dispose();
        _connection?.Dispose();
        _transaction = null;
        _connection = null;
        if (ReferenceEquals(Current.Value, this)) Current.Value = null;
    }

    public ValueTask DisposeAsync()
    {
        Clear();
        return ValueTask.CompletedTask;
    }
}
