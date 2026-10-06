using HabitFlow.Domain;

namespace HabitFlow.Application;

public sealed class AccountPeopleService(
    IClientRepository clients,
    IUserRepository users,
    IUserInviteRepository invites,
    AccountCapacityService capacity,
    CurrentTenantService tenant,
    CurrentUserContext currentUser,
    AuditService audit,
    IUnitOfWork unitOfWork)
{
    public async Task<AccountPeoplePage> SearchAsync(Guid clientId, string? search, string? role, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        EnsureCanManage(clientId);
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        role = NormalizeRole(role);
        status = NormalizeStatus(status);
        var total = await clients.CountUsersAsync(clientId, search, role, status, ct);
        var pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, pageCount);
        var offset = checked((page - 1) * pageSize);
        var people = await clients.SearchUsersAsync(clientId, search, role, status, offset, pageSize, ct);
        var usage = await capacity.GetUsageAsync(clientId, ct);
        return new AccountPeoplePage(people, page, pageSize, total, usage.Occupied, usage.Limit, usage.InviteBlock);
    }

    public async Task ChangeRoleAsync(Guid clientId, Guid userId, UserRole role, string reason, CancellationToken ct = default)
    {
        EnsureCanManage(clientId);
        EnsureReason(reason);
        if (role == UserRole.SuperAdmin)
            throw new InvalidOperationException("Perfil solicitado inválido.");

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await invites.LockClientAsync(clientId, ct);
            var target = await GetTargetAsync(clientId, userId, ct);
            if (target.Role == role)
            {
                await unitOfWork.CommitAsync(ct);
                return;
            }
            if (!TenantAccessPolicy.CanGrant(currentUser.Role, target.Role) ||
                !TenantAccessPolicy.CanGrant(currentUser.Role, role))
                throw new TenantAccessDeniedException("Seu perfil não permite alterar este perfil.");
            if (IsAdministrator(target.Role) && !IsAdministrator(role) && IsActive(target))
                await EnsureAnotherActiveAdministratorAsync(clientId, userId, ct);

            var updated = target with { Role = role, UpdatedAt = DateTime.UtcNow };
            await users.UpdateAsync(updated, ct);
            await users.IncrementSessionVersionAsync(userId, ct);
            await WriteAuditAsync("ClientUserRoleChanged", clientId, target, updated, reason, ct);
            await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task SetActiveAsync(Guid clientId, Guid userId, bool active, string reason, CancellationToken ct = default)
    {
        EnsureCanManage(clientId);
        EnsureReason(reason);

        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            await invites.LockClientAsync(clientId, ct);
            var target = await GetTargetAsync(clientId, userId, ct);
            if (IsActive(target) == active)
            {
                await unitOfWork.CommitAsync(ct);
                return;
            }

            if (!TenantAccessPolicy.CanGrant(currentUser.Role, target.Role))
                throw new TenantAccessDeniedException("Seu perfil não permite alterar o acesso desta pessoa.");
            if (!active && IsAdministrator(target.Role))
                await EnsureAnotherActiveAdministratorAsync(clientId, userId, ct);

            if (active)
            {
                var client = await clients.GetByIdAsync(clientId, ct);
                if (client is null || !client.IsActive || client.Status != ClientStatus.Active)
                    throw new InvalidOperationException("A conta está indisponível e não permite reativação de pessoas.");

                if (!await capacity.CanReactivateAsync(clientId, ct))
                    throw new PlanLimitException("Não há vagas disponíveis para reativar esta pessoa. Cancele um convite pendente ou amplie a capacidade.");
            }

            var updated = target with { AccountStatus = active ? AccountStatus.Active : AccountStatus.Blocked, UpdatedAt = DateTime.UtcNow };
            await users.UpdateAsync(updated, ct);
            if (!active) await users.IncrementSessionVersionAsync(userId, ct);
            await WriteAuditAsync(active ? "ClientUserReactivated" : "ClientUserDeactivated", clientId, target, updated, reason, ct);
            await unitOfWork.CommitAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<User> GetTargetAsync(Guid clientId, Guid userId, CancellationToken ct)
    {
        var target = await users.GetByIdAsync(userId, ct) ?? throw new InvalidOperationException("Pessoa não encontrada.");
        if (target.ClientId != clientId || target.Role == UserRole.SuperAdmin)
            throw new TenantAccessDeniedException("A pessoa não pertence a esta conta.");
        return target;
    }

    private async Task EnsureAnotherActiveAdministratorAsync(Guid clientId, Guid excludingUserId, CancellationToken ct)
    {
        var members = await clients.GetUsersAsync(clientId, ct);
        if (!members.Any(member => member.Id != excludingUserId && IsActive(member.AccountStatus) && IsAdministrator(member.Role)))
            throw new InvalidOperationException("A conta precisa manter ao menos uma pessoa administradora ativa.");
    }

    private async Task WriteAuditAsync(string action, Guid clientId, User before, User after, string reason, CancellationToken ct)
    {
        await audit.LogRequiredAsync(action, "Alteração de acesso de pessoa na conta.", userId: currentUser.UserId,
            metadata: new
            {
                tenantId = clientId,
                targetId = after.Id,
                before = new { before.Role, before.AccountStatus },
                after = new { after.Role, after.AccountStatus },
                reason = reason.Trim(),
                correlationId = Guid.NewGuid()
            }, ct: ct);
    }

    private void EnsureCanManage(Guid clientId)
    {
        tenant.EnsureCanAccessClient(clientId);
        if (!TenantAccessPolicy.CanManageUsers(currentUser.Role))
            throw new TenantAccessDeniedException("Você não tem permissão para administrar pessoas desta conta.");
    }

    private static bool IsActive(User user) => user.AccountStatus == AccountStatus.Active;
    private static bool IsActive(string status) => string.Equals(status, nameof(AccountStatus.Active), StringComparison.OrdinalIgnoreCase);
    private static bool IsAdministrator(string role) => Enum.TryParse<UserRole>(role, true, out var parsed) && IsAdministrator(parsed);
    private static bool IsAdministrator(UserRole role) => role is UserRole.Admin or UserRole.TenantAdmin or UserRole.TenantOwner;
    private static string? NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role) || role.Equals("All", StringComparison.OrdinalIgnoreCase)) return null;
        if (Enum.TryParse<UserRole>(role, true, out var parsed) && parsed != UserRole.SuperAdmin) return parsed.ToString();
        throw new ArgumentException("Filtro de perfil inválido.", nameof(role));
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || status.Equals("All", StringComparison.OrdinalIgnoreCase)) return null;
        if (Enum.TryParse<AccountStatus>(status, true, out var parsed)) return parsed.ToString();
        throw new ArgumentException("Filtro de status inválido.", nameof(status));
    }

    private static void EnsureReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 500)
            throw new ArgumentException("Informe o motivo (até 500 caracteres).", nameof(reason));
    }
}

public sealed record AccountPeoplePage(IReadOnlyList<ClientUserSummary> People, int Page, int PageSize, int Total, int OccupiedSlots, int UserLimit, PlanInviteBlock? InviteBlock)
{
    public int PageCount => Math.Max(1, (int)Math.Ceiling(Total / (double)PageSize));
    public int RemainingSlots => UserLimit < 0 ? -1 : Math.Max(0, UserLimit - OccupiedSlots);
    public int ExcessSlots => UserLimit < 0 ? 0 : Math.Max(0, OccupiedSlots - UserLimit);
}
