using HabitFlow.Domain;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class UserService(IUserRepository repo, ILogger<UserService> logger)
{
    public async Task<User?> GetAsync(Guid id, CancellationToken ct = default)
    {
        try { return await repo.GetByIdAsync(id, ct); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao consultar usuário {UserId}", id); return null; }
    }

    public async Task<IReadOnlyList<User>> SearchAsync(string? query, CancellationToken ct = default)
    {
        try { return await repo.SearchAsync(query, ct); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao pesquisar usuários"); return Array.Empty<User>(); }
    }
}
