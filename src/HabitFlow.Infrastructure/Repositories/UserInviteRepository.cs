using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class UserInviteRepository(SqlExecutor db) : IUserInviteRepository
{
    private const string Columns = "id, client_id, email, role, token_hash, status, invited_by_user_id, accepted_by_user_id, expires_at, accepted_at, canceled_at, created_at, updated_at";

    public Task LockClientAsync(Guid clientId, CancellationToken ct = default) =>
        db.ExecuteAsync("select pg_advisory_xact_lock(hashtextextended(@lockKey, 0))", new { lockKey = $"habitflow:user-capacity:{clientId:N}" }, ct);

    public Task CreateAsync(UserInvite invite, CancellationToken ct = default) => db.ExecuteAsync(@"
insert into habitflow.user_invites(id, client_id, email, role, token_hash, status, invited_by_user_id, accepted_by_user_id, expires_at, accepted_at, canceled_at, created_at, updated_at)
values(@Id, @ClientId, @Email, @Role, @TokenHash, @Status, @InvitedByUserId, @AcceptedByUserId, @ExpiresAt, @AcceptedAt, @CanceledAt, @CreatedAt, @UpdatedAt)", ToParameters(invite), ct);

    public Task<UserInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<UserInvite>("select " + Columns + " from habitflow.user_invites where token_hash = @tokenHash", new { tokenHash }, ct);
    public Task<UserInvite?> GetByIdAsync(Guid inviteId, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<UserInvite>("select " + Columns + " from habitflow.user_invites where id = @inviteId", new { inviteId }, ct);
    public Task<UserInvite?> GetPendingByClientAndEmailAsync(Guid clientId, string normalizedEmail, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<UserInvite>("select " + Columns + " from habitflow.user_invites where client_id=@clientId and lower(trim(email))=@normalizedEmail and status='Pending' order by created_at desc limit 1", new { clientId, normalizedEmail }, ct);

    public Task<int> GetOccupiedSlotsAsync(Guid clientId, DateTime utcNow, CancellationToken ct = default) =>
        db.QuerySingleOrDefaultAsync<int>("""
            select (
                select count(*) from habitflow.users
                where client_id=@clientId and account_status='Active' and role <> 'SuperAdmin'
            ) + (
                select count(*) from habitflow.user_invites i
                where i.client_id=@clientId and i.status='Pending' and i.expires_at>@utcNow
                  and not exists (
                    select 1 from habitflow.users u
                    where u.client_id=i.client_id and u.account_status='Active' and u.role <> 'SuperAdmin'
                      and lower(trim(u.email))=lower(trim(i.email))
                  )
            )::int as occupied_slots
            """, new { clientId, utcNow }, ct);

    public async Task<IReadOnlyList<UserInvite>> GetByClientAsync(Guid clientId, CancellationToken ct = default) => (await db.QueryAsync<UserInvite>("select " + Columns + " from habitflow.user_invites where client_id = @clientId order by created_at desc", new { clientId }, ct)).ToList();

    public async Task<IReadOnlyList<ClientInviteSummary>> GetSummariesByClientAsync(Guid clientId, CancellationToken ct = default) =>
        (await db.QueryAsync<ClientInviteSummary>("""
            select i.id, i.email, i.role, i.status,
                   coalesce(u.name, u.email) as inviter_name,
                   i.created_at, i.expires_at
            from habitflow.user_invites i
            left join habitflow.users u on u.id = i.invited_by_user_id
            where i.client_id = @clientId
            order by i.created_at desc, i.id
            """, new { clientId }, ct)).ToList();

    public async Task<bool> MarkAcceptedAsync(Guid inviteId, Guid acceptedByUserId, DateTime utcNow, CancellationToken ct = default) =>
        await db.ExecuteAsync("update habitflow.user_invites set status = 'Accepted', accepted_by_user_id = @acceptedByUserId, accepted_at = @utcNow, updated_at = @utcNow where id = @inviteId and status = 'Pending' and expires_at > @utcNow", new { inviteId, acceptedByUserId, utcNow }, ct) == 1;

    public async Task<bool> MarkCanceledAsync(Guid inviteId, DateTime utcNow, CancellationToken ct = default) =>
        await db.ExecuteAsync("update habitflow.user_invites set status = 'Canceled', canceled_at = @utcNow, updated_at = @utcNow where id = @inviteId and status = 'Pending' and expires_at > @utcNow", new { inviteId, utcNow }, ct) == 1;

    public async Task<bool> RotateTokenAsync(Guid inviteId, string tokenHash, DateTime expiresAt, DateTime utcNow, CancellationToken ct = default) =>
        await db.ExecuteAsync("update habitflow.user_invites set token_hash = @tokenHash, expires_at = @expiresAt, updated_at = @utcNow where id = @inviteId and status = 'Pending' and expires_at > @utcNow", new { inviteId, tokenHash, expiresAt, utcNow }, ct) == 1;

    public Task MarkExpiredAsync(Guid clientId, DateTime utcNow, CancellationToken ct = default) =>
        db.ExecuteAsync("update habitflow.user_invites set status = 'Expired', updated_at = @utcNow where client_id = @clientId and status = 'Pending' and expires_at <= @utcNow", new { clientId, utcNow }, ct);

    private static object ToParameters(UserInvite invite) => new { invite.Id, invite.ClientId, invite.Email, Role = DbEnum.Text(invite.Role), invite.TokenHash, Status = DbEnum.Text(invite.Status), invite.InvitedByUserId, invite.AcceptedByUserId, invite.ExpiresAt, invite.AcceptedAt, invite.CanceledAt, invite.CreatedAt, invite.UpdatedAt };
}
