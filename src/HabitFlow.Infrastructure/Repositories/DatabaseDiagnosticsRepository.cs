using Dapper;
using HabitFlow.Application;

namespace HabitFlow.Infrastructure;

public sealed class DatabaseDiagnosticsRepository(DbConnectionFactory factory) : IDatabaseDiagnosticsRepository
{
    public async Task<DatabaseDiagnostics> GetAsync(CancellationToken ct = default)
    {
        using var connection = await factory.OpenAsync(ct);
        var database = await connection.ExecuteScalarAsync<string>(new CommandDefinition("select current_database()", cancellationToken: ct));
        var schema = await connection.ExecuteScalarAsync<string>(new CommandDefinition("select current_schema()", cancellationToken: ct));
        var version = await connection.ExecuteScalarAsync<string>(new CommandDefinition("select version()", cancellationToken: ct));
        var schemaExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition("select exists(select 1 from information_schema.schemata where schema_name='habitflow')", cancellationToken: ct));
        var tableCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("select count(*) from information_schema.tables where table_schema='habitflow' and table_type='BASE TABLE'", cancellationToken: ct));
        var publicConflicts = await connection.ExecuteScalarAsync<int>(new CommandDefinition("select count(*) from information_schema.tables where table_schema='public' and table_name = any(@Tables)", new { Tables = RequiredTables }, cancellationToken: ct));
        var requiredTables = await connection.ExecuteScalarAsync<int>(new CommandDefinition("select count(*) from information_schema.tables where table_schema='habitflow' and table_name = any(@Tables)", new { Tables = RequiredTables }, cancellationToken: ct));
        var users = await SafeCount(connection, "select count(*) from habitflow.users", ct);
        var habits = await SafeCount(connection, "select count(*) from habitflow.habits", ct);
        var logs = await SafeCount(connection, "select count(*) from habitflow.system_audit_logs", ct);
        var requiredOk = requiredTables == RequiredTables.Length;
        var status = !schemaExists || !requiredOk ? "unhealthy" : publicConflicts > 0 ? "warning" : "healthy";
        return new DatabaseDiagnostics(status, database, "habitflow", schemaExists, tableCount, publicConflicts, version, DateTime.UtcNow, users, habits, logs, requiredOk, schema, null);
    }

    private static readonly string[] RequiredTables =
    [
        "users", "habits", "habit_completions", "support_tickets", "support_messages", "system_audit_logs",
        "admin_audit_logs", "system_settings", "lgpd_requests", "billing_events", "notifications", "user_reports",
        "habit_objectives", "habit_templates", "habit_reminders", "reminder_dispatches", "schema_migrations",
        "user_privacy_consents", "privacy_request_events"
    ];

    private static async Task<int> SafeCount(System.Data.IDbConnection connection, string sql, CancellationToken ct)
    {
        try { return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, cancellationToken: ct)); }
        catch { return 0; }
    }
}
