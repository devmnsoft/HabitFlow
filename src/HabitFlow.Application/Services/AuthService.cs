using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class AuthService(IUserRepository users, IPasswordHasher hasher, AuditService audit, ILogger<AuthService> logger)
{
    public async Task<Result<User>> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Tentativa de cadastro para {Email}", dto.Email);
            if (dto.Password.Length < 8) return Result<User>.Failure("password.short", "Senha deve ter ao menos 8 caracteres.");
            if (await users.GetByEmailAsync(dto.Email.ToLowerInvariant(), ct) is not null) return Result<User>.Failure("email.exists", "E-mail já cadastrado.");
            var now = DateTime.UtcNow;
            var user = new User(Guid.NewGuid(), dto.Name.Trim(), dto.Email.ToLowerInvariant(), hasher.Hash(dto.Password), null, UserRole.User, AccountStatus.Active, RiskStatus.Normal, UserPlan.Free, PlanStatus.Active, false, false, now, now, null, null, now, now);
            await users.CreateAsync(user, ct);
            await audit.LogAsync("user_registered", "Novo usuário", AuditSeverity.Info, user.Id, user.Email, null, ct);
            return Result<User>.Success(user);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, PostgresErrorHelper.IsDatabaseMissing(ex) ? PostgresErrorHelper.DatabaseMissingLogMessage : "Erro ao cadastrar {Email}", dto.Email);
            if (!PostgresErrorHelper.IsConnectionFailure(ex))
            {
                await audit.LogAsync("register_error", "Erro ao cadastrar usuário.", AuditSeverity.Error, null, dto.Email, new { dto.Email }, ct);
            }
            return PostgresErrorHelper.IsConnectionFailure(ex)
                ? Result<User>.Failure(PostgresErrorHelper.BuildErrorCode(ex), PostgresErrorHelper.BuildFriendlyMessage(ex))
                : Result<User>.Failure("auth.register_error", "Não foi possível concluir o cadastro agora.");
        }
    }

    public async Task<Result<User>> LoginAsync(LoginDto dto, string? ip, string? userAgent, CancellationToken ct = default)
    {
        try
        {
            logger.LogInformation("Tentativa de login para {Email}", dto.Email);
            var email = dto.Email.ToLowerInvariant();
            var user = await users.GetByEmailAsync(email, ct);
            var ok = user is not null && hasher.Verify(dto.Password, user.PasswordHash);
            await users.AddLoginAttemptAsync(new LoginAttempt(Guid.NewGuid(), email, ok, ip, userAgent, DateTime.UtcNow), ct);
            await audit.LogAsync(ok ? "login_success" : "login_failed", "Tentativa de login", ok ? AuditSeverity.Info : AuditSeverity.Warning, user?.Id, user?.Email ?? email, null, ct);
            return ok ? Result<User>.Success(user!) : Result<User>.Failure("login.invalid", "E-mail ou senha inválidos.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, PostgresErrorHelper.IsDatabaseMissing(ex) ? PostgresErrorHelper.DatabaseMissingLogMessage : "Erro ao realizar login para {Email}", dto.Email);
            if (!PostgresErrorHelper.IsConnectionFailure(ex))
            {
                await audit.LogAsync("login_error", "Erro ao realizar login.", AuditSeverity.Error, null, dto.Email, new { dto.Email }, ct);
            }
            return PostgresErrorHelper.IsConnectionFailure(ex)
                ? Result<User>.Failure(PostgresErrorHelper.BuildErrorCode(ex), PostgresErrorHelper.BuildFriendlyMessage(ex))
                : Result<User>.Failure("auth.login_error", "Não foi possível realizar o login agora.");
        }
    }
}
