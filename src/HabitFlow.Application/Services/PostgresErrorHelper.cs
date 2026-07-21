using System.Net.Sockets;
using System.Reflection;

namespace HabitFlow.Application;

public static class PostgresErrorHelper
{
    public const string InvalidPasswordSqlState = "28P01";
    public const string DatabaseMissingSqlState = "3D000";
    public const string ConnectionSqlState = "08001";
    public const string PermissionDeniedSqlState = "42501";
    public const string MissingTableSqlState = "42P01";

    public const string InvalidPasswordCode = "postgres.invalid_password";
    public const string DatabaseMissingCode = "postgres.database_missing";
    public const string ConnectionUnavailableCode = "postgres.unavailable";
    public const string PermissionDeniedCode = "postgres.permission_denied";
    public const string MissingTableCode = "postgres.table_missing";
    public const string GenericDatabaseCode = "postgres.error";

    public const string FriendlyInvalidPasswordMessage = "A senha do PostgreSQL está incorreta para o usuário configurado.";
    public const string FriendlyDatabaseMissingMessage = "O banco informado não existe.";
    public const string FriendlyConnectionUnavailableMessage = "Não foi possível conectar ao PostgreSQL.";
    public const string FriendlyPermissionDeniedMessage = "O usuário configurado não tem permissão para acessar o banco HabitFlow.";
    public const string FriendlyMissingTableMessage = "Uma tabela obrigatória do schema habitflow não foi encontrada.";
    public const string FriendlyGenericMessage = "Não foi possível acessar o banco de dados agora. Verifique a configuração do PostgreSQL.";

    public const string DatabaseMissingLogMessage = "Banco de dados habitflow não existe. Execute scripts/database/create-habitflow-db.ps1 e scripts/database/apply-script-completo.ps1.";
    public const string InvalidPasswordLogMessage = "Falha de autenticação PostgreSQL 28P01. Revise Username/Password da connection string.";

    public static bool IsInvalidPassword(Exception ex) => HasSqlState(ex, InvalidPasswordSqlState);
    public static bool IsDatabaseMissing(Exception ex) => HasSqlState(ex, DatabaseMissingSqlState);
    public static bool IsPermissionDenied(Exception ex) => HasSqlState(ex, PermissionDeniedSqlState);
    public static bool IsMissingTable(Exception ex) => HasSqlState(ex, MissingTableSqlState);

    public static bool IsConnectionUnavailable(Exception ex) =>
        HasSqlState(ex, ConnectionSqlState) || Contains(ex, e =>
            e is SocketException ||
            e.GetType().FullName?.Contains("NpgsqlException", StringComparison.OrdinalIgnoreCase) == true ||
            e.GetType().Name.Contains("Timeout", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("connection refused", StringComparison.OrdinalIgnoreCase));

    public static bool IsConnectionFailure(Exception ex) => IsInvalidPassword(ex) || IsDatabaseMissing(ex) || IsConnectionUnavailable(ex) || IsPermissionDenied(ex) || IsMissingTable(ex);

    public static string ToFriendlyCode(Exception ex) => IsInvalidPassword(ex) ? InvalidPasswordCode
        : IsDatabaseMissing(ex) ? DatabaseMissingCode
        : IsPermissionDenied(ex) ? PermissionDeniedCode
        : IsMissingTable(ex) ? MissingTableCode
        : IsConnectionUnavailable(ex) ? ConnectionUnavailableCode
        : GenericDatabaseCode;

    public static string ToFriendlyMessage(Exception ex) => IsInvalidPassword(ex) ? FriendlyInvalidPasswordMessage
        : IsDatabaseMissing(ex) ? FriendlyDatabaseMissingMessage
        : IsPermissionDenied(ex) ? FriendlyPermissionDeniedMessage
        : IsMissingTable(ex) ? FriendlyMissingTableMessage
        : IsConnectionUnavailable(ex) ? FriendlyConnectionUnavailableMessage
        : FriendlyGenericMessage;

    public static string BuildFriendlyMessage(Exception ex) => ToFriendlyMessage(ex);
    public static string BuildErrorCode(Exception ex) => ToFriendlyCode(ex);

    private static bool HasSqlState(Exception ex, string sqlState) => Contains(ex, e => string.Equals(GetSqlState(e), sqlState, StringComparison.OrdinalIgnoreCase));

    private static bool Contains(Exception ex, Func<Exception, bool> predicate)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (predicate(current)) return true;
            if (current is AggregateException aggregate && aggregate.InnerExceptions.Any(inner => Contains(inner, predicate))) return true;
        }
        return false;
    }

    private static string? GetSqlState(Exception ex)
    {
        var property = ex.GetType().GetProperty("SqlState", BindingFlags.Instance | BindingFlags.Public);
        return property?.GetValue(ex) as string ?? ex.Data["SqlState"] as string;
    }
}
