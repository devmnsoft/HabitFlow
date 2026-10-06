using System.Security.Cryptography;
using System.Text;
using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class UserInviteService(IUserInviteRepository invites, IUserRepository users, CurrentTenantService tenant, CurrentUserContext currentUser, AuditService audit, PlanEntitlementService plans, IClientRepository clients, AccountCapacityService capacity, IBillingEventLogRepository billingEvents, IUnitOfWork unitOfWork)
{
    public async Task<(UserInvite Invite, string Token)> CreateInviteAsync(Guid clientId, string email, UserRole role, CancellationToken ct = default)
    {
        tenant.EnsureCanAccessClient(clientId);
        var normalizedEmail = NormalizeEmail(email);
        if (role is not (UserRole.User or UserRole.Admin))
            throw new InvalidOperationException("Perfil de convite inválido.");

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await invites.LockClientAsync(clientId, ct);
            var now = DateTime.UtcNow;
            await invites.MarkExpiredAsync(clientId, now, ct);

            var existingMember = await clients.GetUserByClientEmailAsync(clientId, normalizedEmail, ct);
            if (existingMember is not null)
                throw new InvalidOperationException(IsActive(existingMember.AccountStatus)
                    ? "Este e-mail já pertence a uma pessoa ativa da conta."
                    : "Este e-mail já está vinculado à conta. Reative a pessoa pela central Pessoas.");

            if (await invites.GetPendingByClientAndEmailAsync(clientId, normalizedEmail, ct) is not null)
                throw new InvalidOperationException("Já existe um convite pendente para este e-mail. Reenvie o convite existente.");

            var occupiedSlots = await capacity.GetOccupiedSlotsAsync(clientId, ct);
            var canInvite = await plans.CanInviteUserAsync(clientId, occupiedSlots, ct);
            var blocked = canInvite ? null : await plans.GetInviteBlockAsync(clientId, occupiedSlots, ct);
            if (blocked is not null)
            {
                var planCode = await plans.GetEffectivePlanAsync(clientId, ct);
                var metadata = $"{{\"entitlement\":\"{blocked.Code}\",\"occupied_slots\":{occupiedSlots}}}";
                var correlationId = Guid.NewGuid();
                await billingEvents.LogAsync(clientId, currentUser.UserId == Guid.Empty ? null : currentUser.UserId, "billing.entitlement.blocked", "blocked", null, planCode, metadata, correlationId, ct);
                await audit.LogAsync("PlanLimitBlocked", $"Admissão bloqueada por '{blocked.Code}' no cliente {clientId} (ocupadas={occupiedSlots}).", userId: currentUser.UserId == Guid.Empty ? null : currentUser.UserId, ct: ct);
                await unitOfWork.CommitAsync(ct);
                throw new PlanLimitException(blocked.Message);
            }

            var token = GenerateToken();
            var invite = new UserInvite(Guid.NewGuid(), clientId, normalizedEmail, role, HashToken(token), UserInviteStatus.Pending, currentUser.UserId == Guid.Empty ? null : currentUser.UserId, null, now.AddDays(7), null, null, now, now);
            await invites.CreateAsync(invite, ct);
            var correlation = Guid.NewGuid();
            await audit.LogRequiredAsync("UserInviteCreated", "Convite criado.", userId: invite.InvitedByUserId,
                metadata: new { tenantId = clientId, targetId = invite.Id, after = new { invite.Email, invite.Role, invite.Status, invite.ExpiresAt }, correlationId = correlation }, ct: ct);
            await unitOfWork.CommitAsync(ct);
            return (invite, token);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
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

    public async Task<IReadOnlyList<UserInvite>> GetByClientAsync(Guid clientId, CancellationToken ct = default)
    {
        tenant.EnsureCanAccessClient(clientId);
        await invites.MarkExpiredAsync(clientId, DateTime.UtcNow, ct);
        return await invites.GetByClientAsync(clientId, ct);
    }

    public async Task<IReadOnlyList<AccountInviteItem>> GetSummariesByClientAsync(Guid clientId, CancellationToken ct = default)
    {
        tenant.EnsureCanAccessClient(clientId);
        var now = DateTime.UtcNow;
        await invites.MarkExpiredAsync(clientId, now, ct);
        var summaries = await invites.GetSummariesByClientAsync(clientId, ct);
        return summaries.Select(invite => new AccountInviteItem(invite.Id, invite.Email, invite.Role, invite.InviterName,
            invite.CreatedAt, invite.ExpiresAt, invite.ExpiresAt <= now && invite.Status == nameof(UserInviteStatus.Pending)
                ? nameof(UserInviteStatus.Expired)
                : invite.Status)).ToArray();
    }

    public async Task<(UserInvite Invite, string Token)> RotateTokenAsync(Guid inviteId, CancellationToken ct = default)
    {
        var initial = await invites.GetByIdAsync(inviteId, ct) ?? throw new InvalidOperationException("Convite não encontrado.");
        tenant.EnsureCanAccessClient(initial.ClientId);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await invites.LockClientAsync(initial.ClientId, ct);
            var now = DateTime.UtcNow;
            await invites.MarkExpiredAsync(initial.ClientId, now, ct);
            var invite = await invites.GetByIdAsync(inviteId, ct);
            if (invite is null || !invite.IsPending(now))
                throw new InvalidOperationException("Somente convites pendentes e não expirados podem ser reenviados.");

            var token = GenerateToken();
            if (!await invites.RotateTokenAsync(invite.Id, HashToken(token), now.AddDays(7), now, ct))
                throw new InvalidOperationException("Este convite não está mais disponível para reenvio.");

            var correlation = Guid.NewGuid();
            await audit.LogRequiredAsync("UserInviteRotated", "Token de convite rotacionado.", userId: currentUser.UserId,
                metadata: new { tenantId = invite.ClientId, targetId = invite.Id, before = new { invite.Role, invite.ExpiresAt }, after = new { invite.Role, expiresAt = now.AddDays(7) }, correlationId = correlation }, ct: ct);
            var updated = invite with { TokenHash = HashToken(token), ExpiresAt = now.AddDays(7), UpdatedAt = now };
            await unitOfWork.CommitAsync(ct);
            return (updated, token);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<bool> CancelAsync(Guid inviteId, CancellationToken ct = default)
    {
        var initial = await invites.GetByIdAsync(inviteId, ct) ?? throw new InvalidOperationException("Convite não encontrado.");
        tenant.EnsureCanAccessClient(initial.ClientId);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await invites.LockClientAsync(initial.ClientId, ct);
            var now = DateTime.UtcNow;
            await invites.MarkExpiredAsync(initial.ClientId, now, ct);
            var invite = await invites.GetByIdAsync(inviteId, ct);
            if (invite is null)
                throw new InvalidOperationException("Convite não encontrado.");
            if (invite.Status == UserInviteStatus.Canceled)
            {
                await unitOfWork.CommitAsync(ct);
                return true;
            }
            if (!invite.IsPending(now) || !await invites.MarkCanceledAsync(inviteId, now, ct))
            {
                await unitOfWork.CommitAsync(ct);
                return false;
            }

            var correlation = Guid.NewGuid();
            await audit.LogRequiredAsync("UserInviteCanceled", "Convite cancelado.", userId: currentUser.UserId,
                metadata: new { tenantId = invite.ClientId, targetId = invite.Id, before = invite.Status, after = UserInviteStatus.Canceled, correlationId = correlation }, ct: ct);
            await unitOfWork.CommitAsync(ct);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task AcceptAsync(string token, Guid userId, CancellationToken ct = default)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var invite = await invites.GetByTokenHashAsync(HashToken(token), ct);
            if (invite is null)
                throw new InvalidOperationException("Este convite é inválido ou expirou.");
            await invites.LockClientAsync(invite.ClientId, ct);
            invite = await invites.GetByTokenHashAsync(HashToken(token), ct);
            var now = DateTime.UtcNow;
            if (invite is null || !invite.IsPending(now))
                throw new InvalidOperationException("Este convite é inválido ou expirou.");

            var user = await users.GetByIdAsync(userId, ct) ?? throw new InvalidOperationException("Usuário não encontrado.");
            if (!string.Equals(EmailKey(user.Email), EmailKey(invite.Email), StringComparison.Ordinal))
                throw new InvalidOperationException("Entre com o e-mail convidado para aceitar este convite.");
            if (user.AccountStatus != AccountStatus.Active)
                throw new InvalidOperationException("Esta conta não pode aceitar convites no momento.");
            if (user.ClientId is not null)
                throw new InvalidOperationException(user.ClientId == invite.ClientId
                    ? "Você já pertence a esta conta."
                    : "Este convite pertence a outra conta e não altera seu vínculo atual.");

            var linked = user with { ClientId = invite.ClientId, Role = invite.Role, UpdatedAt = now };
            if (!await users.LinkToClientFromInviteAsync(userId, invite.ClientId, invite.Role, now, ct))
                throw new InvalidOperationException("A conta já foi vinculada a outra organização.");
            if (!await invites.MarkAcceptedAsync(invite.Id, userId, now, ct))
                throw new InvalidOperationException("Este convite não está mais disponível.");

            var correlation = Guid.NewGuid();
            await audit.LogRequiredAsync("UserInviteAccepted", "Convite aceito.", userId: userId,
                metadata: new { tenantId = invite.ClientId, targetId = userId, before = new { user.ClientId, user.Role }, after = new { linked.ClientId, linked.Role }, correlationId = correlation }, ct: ct);
            await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace("+", "-").Replace("/", "_").TrimEnd('=');

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 200 || !System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var address) ||
            !string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Informe um e-mail válido.", nameof(email));
        return address.Address.ToLowerInvariant();
    }

    private static string EmailKey(string email) => (email ?? string.Empty).Trim().ToLowerInvariant();
    private static bool IsActive(string accountStatus) => string.Equals(accountStatus, nameof(AccountStatus.Active), StringComparison.OrdinalIgnoreCase);

}

/// <summary>Lançado quando uma operação é bloqueada por limite do plano contratado (entitlement).</summary>
public sealed class PlanLimitException(string message) : InvalidOperationException(message);

public sealed record AccountInviteItem(Guid Id, string Email, string Role, string? InviterName, DateTime CreatedAt, DateTime ExpiresAt, string Status);
