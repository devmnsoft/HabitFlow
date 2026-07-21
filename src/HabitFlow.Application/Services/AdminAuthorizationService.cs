using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed class AdminAuthorizationService
{
    public Result EnsureAdmin(User user) =>
        user.Role == UserRole.Admin
            ? Result.Success()
            : Result.Failure("admin.forbidden", "Acesso administrativo negado.");
}
