using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class DatabaseDiagnosticsService(IDatabaseDiagnosticsRepository repository, ILogger<DatabaseDiagnosticsService> logger)
{
    public async Task<Result<DatabaseDiagnostics>> GetAsync(CancellationToken ct = default)
    {
        try
        {
            return Result<DatabaseDiagnostics>.Success(await repository.GetAsync(ct));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, PostgresErrorHelper.IsInvalidPassword(ex) ? PostgresErrorHelper.InvalidPasswordLogMessage : PostgresErrorHelper.IsDatabaseMissing(ex) ? PostgresErrorHelper.DatabaseMissingLogMessage : "Falha no diagnóstico do banco PostgreSQL HabitFlow");
            return Result<DatabaseDiagnostics>.Success(new DatabaseDiagnostics(
                "unhealthy", null, "habitflow", false, 0, 0, null, DateTime.UtcNow, 0, 0, 0, false, null,
                PostgresErrorHelper.BuildFriendlyMessage(ex), PostgresErrorHelper.BuildErrorCode(ex)));
        }
    }
}
