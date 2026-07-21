using HabitFlow.Domain;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class AuditService(IAuditRepository repo, LogSanitizer sanitizer, ILogger<AuditService> logger)
{
    public async Task LogAsync(string action, string message, AuditSeverity severity = AuditSeverity.Info, Guid? userId = null, string? email = null, object? metadata = null, CancellationToken ct = default)
    {
        try
        {
            await repo.AddSystemAsync(new SystemAuditLog(Guid.NewGuid(), userId, email, severity, "web", action, sanitizer.Sanitize(message), metadata is null ? null : sanitizer.SanitizeJson(metadata), null, null, DateTime.UtcNow, false), ct);
        }
        catch (Exception ex) when (PostgresErrorHelper.IsConnectionFailure(ex))
        {
            logger.LogWarning(ex, PostgresErrorHelper.IsDatabaseMissing(ex) ? PostgresErrorHelper.DatabaseMissingLogMessage : "Banco indisponível; auditoria de sistema {Action} não foi persistida.", action);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao registrar auditoria de sistema para {Action}", action);
        }
    }
}
