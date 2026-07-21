using System.Net.Sockets;
using System.Reflection;

namespace HabitFlow.Application;

public static class PostgresErrorHelper
{
    public const string DatabaseMissingSqlState = "3D000";
    public const string DatabaseMissingCode = "database.missing";
    public const string ConnectionFailureCode = "database.connection_failure";
    public const string FriendlyDatabaseMissingMessage = "Banco de dados não configurado. Execute o script de instalação do HabitFlow.";
    public const string FriendlyConnectionFailureMessage = "PostgreSQL indisponível. Verifique se o serviço está ativo e se a connection string está correta.";
    public const string DatabaseMissingLogMessage = "Banco de dados habitflow não existe. Execute scripts/database/create-habitflow-db.ps1 e scripts/database/apply-script-completo.ps1.";

    public static bool IsDatabaseMissing(Exception ex) => Contains(ex, e => string.Equals(GetSqlState(e), DatabaseMissingSqlState, StringComparison.OrdinalIgnoreCase));

    public static bool IsConnectionFailure(Exception ex) =>
        IsDatabaseMissing(ex) || Contains(ex, e => e is SocketException || e.GetType().FullName?.Contains("NpgsqlException", StringComparison.OrdinalIgnoreCase) == true || e.GetType().Name.Contains("Timeout", StringComparison.OrdinalIgnoreCase));

    public static string BuildFriendlyMessage(Exception ex) => IsDatabaseMissing(ex)
        ? FriendlyDatabaseMissingMessage
        : IsConnectionFailure(ex)
            ? FriendlyConnectionFailureMessage
            : "Falha ao acessar o banco de dados. Tente novamente em instantes.";

    public static string BuildErrorCode(Exception ex) => IsDatabaseMissing(ex) ? DatabaseMissingCode : IsConnectionFailure(ex) ? ConnectionFailureCode : "database.error";

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
