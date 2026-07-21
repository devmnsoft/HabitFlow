using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class AdminService(UserService users, AdminAuthorizationService authorization, ILogger<AdminService> logger)
{
    public async Task<Result<IReadOnlyList<User>>> SearchUsersAsync(User admin, string? query, CancellationToken ct = default)
    {
        try
        {
            var result = authorization.EnsureAdmin(admin);
            return result.IsFailure
                ? Result<IReadOnlyList<User>>.Failure(result.Error.Code, result.Error.Message)
                : Result<IReadOnlyList<User>>.Success(await users.SearchAsync(query, ct));
        }
        catch (Exception ex) { logger.LogError(ex, "Erro administrativo ao pesquisar usuários"); return Result<IReadOnlyList<User>>.Failure("admin.search_error", "Não foi possível pesquisar usuários."); }
    }
}
