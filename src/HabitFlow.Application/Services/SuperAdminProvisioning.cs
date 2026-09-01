using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed record CreateSuperAdminCommand(string Name, string Email, string Password, string Confirmation, string Actor, string Reason, string CorrelationId);
public sealed record ResetSuperAdminPasswordCommand(string Email, string Password, string Confirmation, string Actor, string Reason, string CorrelationId);
public sealed record PromoteSuperAdminCommand(string Email, string Actor, string Reason, string CorrelationId);
public sealed record SuperAdminProvisioningResult(bool Success, string Message, Guid? UserId = null);

public interface ISuperAdminProvisioningRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task<User> CreateOrPromoteAsync(string name, string email, string passwordHash, bool mustChangePassword, string actor, string reason, string correlationId, CancellationToken ct);
    Task<User?> PromoteAsync(string email, string actor, string reason, string correlationId, CancellationToken ct);
    Task ResetPasswordAsync(Guid userId, string passwordHash, string actor, string reason, string correlationId, CancellationToken ct);
    Task<(User User, bool Created, bool Updated)> BootstrapAsync(string name, string email, string document, string passwordHash, string correlationId, CancellationToken ct);
}

public sealed class SuperAdminOptions
{
    public const string SectionName = "SuperAdmin";
    public string Email { get; set; } = "comercial@mnsoft.com.br";
    public string Document { get; set; } = "18160057000113";
    public string? InitialPassword { get; set; }
}

public sealed class SuperAdminBootstrapService(ISuperAdminProvisioningRepository repository, IPasswordHasher hasher)
{
    public Task<(User User, bool Created, bool Updated)> BootstrapAsync(SuperAdminOptions options, string correlationId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.InitialPassword))
            throw new InvalidOperationException("HABITFLOW_SUPERADMIN_INITIAL_PASSWORD não configurada; bootstrap seguro não executado.");
        var document = new string(options.Document.Where(char.IsDigit).ToArray());
        if (document.Length != 14) throw new InvalidOperationException("HABITFLOW_SUPERADMIN_DOCUMENT deve ser um CNPJ com 14 dígitos.");
        return repository.BootstrapAsync("Super Administrador MNSOFT", PasswordRecoveryService.NormalizeEmail(options.Email), document,
            hasher.Hash(options.InitialPassword), correlationId, ct);
    }
}

public sealed class CreateSuperAdminHandler(ISuperAdminProvisioningRepository repository, IPasswordPolicy policy, IPasswordHasher hasher)
{
    public async Task<SuperAdminProvisioningResult> HandleAsync(CreateSuperAdminCommand command, CancellationToken ct = default)
    {
        if (command.Password != command.Confirmation) return new(false, "A confirmação da senha não confere.");
        var email = PasswordRecoveryService.NormalizeEmail(command.Email);
        var current = await repository.FindByEmailAsync(email, ct);
        var candidate = current ?? SuperAdminCandidate(command.Name, email);
        var validation = policy.Validate(command.Password, candidate);
        if (validation is not null) return new(false, validation);
        var user = await repository.CreateOrPromoteAsync(command.Name.Trim(), email, hasher.Hash(command.Password), false, command.Actor, command.Reason, command.CorrelationId, ct);
        return new(true, "Super Administrador provisionado com segurança.", user.Id);
    }

    private static User SuperAdminCandidate(string name, string email) => new(Guid.Empty, name.Trim(), email, "!not-a-password-hash!", null,
        UserRole.SuperAdmin, AccountStatus.Active, RiskStatus.Normal, UserPlan.Free, PlanStatus.Active, false, true, null, null, null, null, DateTime.UtcNow, DateTime.UtcNow);
}

public sealed record CreateDevelopmentSuperAdminCommand(string Name, string Email, string Password, string Actor, string CorrelationId);

/// <summary>Development-only provisioning path. Environment and terminal checks remain at the host boundary.</summary>
public sealed class CreateDevelopmentSuperAdminHandler(ISuperAdminProvisioningRepository repository, IPasswordPolicy policy, IPasswordHasher hasher)
{
    public async Task<SuperAdminProvisioningResult> HandleAsync(CreateDevelopmentSuperAdminCommand command, CancellationToken ct = default)
    {
        var email = PasswordRecoveryService.NormalizeEmail(command.Email);
        var current = await repository.FindByEmailAsync(email, ct);
        var candidate = current ?? new User(Guid.Empty, command.Name.Trim(), email, "!not-a-password-hash!", null,
            UserRole.SuperAdmin, AccountStatus.Active, RiskStatus.Normal, UserPlan.Free, PlanStatus.Active, false, true,
            null, null, null, null, DateTime.UtcNow, DateTime.UtcNow);
        var validation = policy.Validate(command.Password, candidate);
        if (validation is not null) return new(false, validation);
        var user = await repository.CreateOrPromoteAsync(command.Name.Trim(), email, hasher.Hash(command.Password), true,
            command.Actor, "provisionamento Development", command.CorrelationId, ct);
        return new(true, "Super Administrador de desenvolvimento provisionado.", user.Id);
    }
}

public sealed class ResetSuperAdminPasswordHandler(ISuperAdminProvisioningRepository repository, IPasswordPolicy policy, IPasswordHasher hasher)
{
    public async Task<SuperAdminProvisioningResult> HandleAsync(ResetSuperAdminPasswordCommand command, CancellationToken ct = default)
    {
        if (command.Password != command.Confirmation) return new(false, "A confirmação da senha não confere.");
        var user = await repository.FindByEmailAsync(PasswordRecoveryService.NormalizeEmail(command.Email), ct);
        if (user is null || user.Role != UserRole.SuperAdmin || user.ClientId is not null) return new(false, "Super Administrador global não encontrado.");
        var validation = policy.Validate(command.Password, user);
        if (validation is not null) return new(false, validation);
        await repository.ResetPasswordAsync(user.Id, hasher.Hash(command.Password), command.Actor, command.Reason, command.CorrelationId, ct);
        return new(true, "Senha redefinida e sessões anteriores revogadas.", user.Id);
    }
}

public sealed class PromoteSuperAdminHandler(ISuperAdminProvisioningRepository repository)
{
    public async Task<SuperAdminProvisioningResult> HandleAsync(PromoteSuperAdminCommand command, CancellationToken ct = default)
    {
        var user = await repository.PromoteAsync(PasswordRecoveryService.NormalizeEmail(command.Email), command.Actor, command.Reason, command.CorrelationId, ct);
        return user is null ? new(false, "Usuário não encontrado.") : new(true, "Usuário promovido a Super Administrador.", user.Id);
    }
}
