using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class CurrentTenantService(CurrentUserContext currentUser)
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
            throw new TenantAccessDeniedException("Acesso negado ao cliente solicitado.");
        }
    }
}

public sealed class TenantAccessDeniedException(string message) : UnauthorizedAccessException(message);
