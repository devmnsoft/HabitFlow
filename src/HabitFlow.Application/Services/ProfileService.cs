using HabitFlow.Domain;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class ProfileService(IUserRepository users, ILogger<ProfileService> logger)
{
    public async Task<User?> GetAsync(Guid id, CancellationToken ct = default)
    {
        try { return await users.GetByIdAsync(id, ct); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao carregar perfil {UserId}", id); return null; }
    }
}
