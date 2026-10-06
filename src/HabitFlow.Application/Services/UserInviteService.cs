using System.Security.Cryptography;
using System.Text;
using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class UserInviteService(IUserInviteRepository invites, IUserRepository users, CurrentTenantService tenant, CurrentUserContext currentUser, AuditService audit, PlanEntitlementService plans, IClientRepository clients, IBillingEventLogRepository billingEvents)
{
    public async Task<(UserInvite Invite, string Token)> CreateInviteAsync(Guid clientId, string email, UserRole role, CancellationToken ct = default)
    {
        tenant.EnsureCanAccessClient(clientId);
        if (role == UserRole.SuperAdmin)
        {
            throw new InvalidOperationException("Convites não podem criar SuperAdmin.");
        }

        var activeUsers = (await clients.GetUsersAsync(clientId, ct)).Count;
        if (!await plans.CanInviteUserAsync(clientId, activeUsers, ct))
        {
            var planCode = await plans.GetEffectivePlanAsync(clientId, ct);
            var metadata = $"{{\"entitlement\":\"users_limit\",\"active_users\":{activeUsers}}}";
            var correlationId = Guid.NewGuid();
            await billingEvents.LogAsync(clientId, currentUser.UserId == Guid.Empty ? null : currentUser.UserId, "billing.entitlement.blocked", "blocked", null, planCode, metadata, correlationId, ct);
            await audit.LogAsync("PlanLimitBlocked", $"Limite de usuários do plano atingido no cliente {clientId} (ativo={activeUsers}).", userId: currentUser.UserId == Guid.Empty ? null : currentUser.UserId, ct: ct);
            throw new PlanLimitException("Limite de usuários do plano atingido. Veja os planos ou fale com o comercial em comercial@mnsoft.com.br para ampliar a equipe.");
        }

        var token = GenerateToken();
        var now = DateTime.UtcNow;
        var invite = new UserInvite(Guid.NewGuid(), clientId, email.Trim().ToLowerInvariant(), role, HashToken(token), UserInviteStatus.Pending, currentUser.UserId == Guid.Empty ? null : currentUser.UserId, null, now.AddDays(7), null, null, now, now);
        await invites.CreateAsync(invite, ct);
        await audit.LogAsync("UserInviteCreated", $"Convite criado para {invite.Email} no cliente {clientId}.", userId: invite.InvitedByUserId, ct: ct);
        return (invite, token);
    }

    public async Task<UserInvite?> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        var invite = await invites.GetByTokenHashAsync(HashToken(token), ct);
        if (invite is null || !invite.IsPending(DateTime.UtcNow))
        {
            return null;
        }
        return invite;
    }

    public async Task AcceptAsync(string token, Guid userId, CancellationToken ct = default)
    {
        var invite = await ValidateTokenAsync(token, ct) ?? throw new InvalidOperationException("Este convite é inválido ou expirou.");
        var user = await users.GetByIdAsync(userId, ct) ?? throw new InvalidOperationException("Usuário não encontrado.");
        var linked = user with { ClientId = invite.ClientId, Role = invite.Role, UpdatedAt = DateTime.UtcNow };
        await users.UpdateAsync(linked, ct);
        await invites.MarkAcceptedAsync(invite.Id, userId, ct);
        await audit.LogAsync("UserInviteAccepted", $"Usuário vinculado ao cliente {invite.ClientId} por convite.", userId: userId, ct: ct);
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").TrimEnd('=');
}

/// <summary>Lançado quando uma operação é bloqueada por limite do plano contratado (entitlement).</summary>
public sealed class PlanLimitException(string message) : InvalidOperationException(message);
