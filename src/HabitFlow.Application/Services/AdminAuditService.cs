using HabitFlow.Domain;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class AdminAuditService(IAdminAuditRepository repo, ILogger<AdminAuditService> logger)
{
    public async Task LogAsync(User admin, string action, string? reason, Guid? targetId = null, string? targetEmail = null, CancellationToken ct = default)
    {
        try
        {
            await repo.AddAsync(new AdminAuditLog(Guid.NewGuid(), admin.Id, admin.Email, action, targetId, targetEmail, reason, null, DateTime.UtcNow), ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao registrar auditoria administrativa para {Action}", action);
        }
    }
}
