using HabitFlow.Domain;
using HabitFlow.Shared;

namespace HabitFlow.Application;

public sealed class RequiredPasswordChangeService(
    IUserRepository users,
    IPasswordHasher hasher,
    IPasswordPolicy policy,
    AuditService audit)
{
    public async Task<Result> ChangeAsync(Guid userId, string currentPassword, string newPassword, string confirmation, CancellationToken ct = default)
    {
        var user = await users.GetByIdAsync(userId, ct);
        if (user is null || !user.MustChangePassword)
            return Result.Failure("security.change_not_required", "A troca obrigatória não está disponível para esta conta.");
        if (!hasher.Verify(currentPassword, user.PasswordHash))
            return Result.Failure("security.current_password_invalid", "A senha atual não confere.");
        if (!string.Equals(newPassword, confirmation, StringComparison.Ordinal))
            return Result.Failure("security.password_mismatch", "A confirmação da nova senha não confere.");
        var violation = policy.Validate(newPassword, user);
        if (violation is not null) return Result.Failure("security.password_policy", violation);

        await users.UpdatePasswordAndSessionVersionAsync(user.Id, hasher.Hash(newPassword), ct);
        await audit.LogAsync(user.Role == UserRole.SuperAdmin ? "security.superadmin.password_changed" : "required_password_changed", "Senha obrigatória alterada; sessões anteriores revogadas.", AuditSeverity.Warning, user.Id, user.Email, new { previousSessionVersion = user.SessionVersion }, ct);
        return Result.Success();
    }
}
