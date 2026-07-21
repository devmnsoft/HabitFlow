using System.Data;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace HabitFlow.Infrastructure;

public sealed class DbConnectionFactory(IConfiguration configuration)
{
    public async Task<IDbConnection> OpenAsync(CancellationToken ct = default)
    {
        var connection = new NpgsqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync(ct);
        return connection;
    }
}
