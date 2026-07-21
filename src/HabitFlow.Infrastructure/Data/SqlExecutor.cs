using Dapper;

namespace HabitFlow.Infrastructure;

public sealed class SqlExecutor(DbConnectionFactory factory)
{
    public async Task<int> ExecuteAsync(string sql, object? parameters = null, CancellationToken ct = default)
    {
        using var connection = await factory.OpenAsync(ct);
        return await connection.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object? parameters = null, CancellationToken ct = default)
    {
        using var connection = await factory.OpenAsync(ct);
        return await connection.QueryAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }

    public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null, CancellationToken ct = default)
    {
        using var connection = await factory.OpenAsync(ct);
        return await connection.QuerySingleOrDefaultAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: ct));
    }
}
