using HabitFlow.Domain;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class CurrentTenantService(CurrentUserContext currentUser, ILogger<CurrentTenantService> logger)
{
    public Guid? GetCurrentClientId() => currentUser.ClientId;

    public Guid RequireCurrentClientId() => currentUser.ClientId
        ?? throw new TenantAccessDeniedException("Usuário sem cliente vinculado.");

    public bool IsSuperAdmin() => currentUser.IsSuperAdmin;

    public bool CanAccessClient(Guid clientId) => currentUser.IsSuperAdmin || currentUser.ClientId == clientId;

    public void EnsureCanAccessClient(Guid clientId)
    {
        if (!CanAccessClient(clientId))
        {
            logger.LogWarning(ApplicationEvents.SecurityTenantIsolationViolationBlocked, "security.tenant_isolation_violation_blocked ClientId={ClientId} RequestedClientId={RequestedClientId} Result={Result}", currentUser.ClientId, clientId, "blocked");
            throw new TenantAccessDeniedException("Acesso negado ao cliente solicitado.");
        }
    }
}

public sealed class TenantAccessDeniedException(string message) : UnauthorizedAccessException(message);
