using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class UserInviteRepository(SqlExecutor db) : IUserInviteRepository
{
    private const string Columns = "id, client_id, email, role, token_hash, status, invited_by_user_id, accepted_by_user_id, expires_at, accepted_at, canceled_at, created_at, updated_at";

    public Task CreateAsync(UserInvite invite, CancellationToken ct = default) => db.ExecuteAsync(@"
insert into habitflow.user_invites(id, client_id, email, role, token_hash, status, invited_by_user_id, accepted_by_user_id, expires_at, accepted_at, canceled_at, created_at, updated_at)
values(@Id, @ClientId, @Email, @Role, @TokenHash, @Status, @InvitedByUserId, @AcceptedByUserId, @ExpiresAt, @AcceptedAt, @CanceledAt, @CreatedAt, @UpdatedAt)", ToParameters(invite), ct);

    public Task<UserInvite?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<UserInvite>("select " + Columns + " from habitflow.user_invites where token_hash = @tokenHash", new { tokenHash }, ct);

    public async Task<IReadOnlyList<UserInvite>> GetByClientAsync(Guid clientId, CancellationToken ct = default) => (await db.QueryAsync<UserInvite>("select " + Columns + " from habitflow.user_invites where client_id = @clientId order by created_at desc", new { clientId }, ct)).ToList();

    public Task MarkAcceptedAsync(Guid inviteId, Guid acceptedByUserId, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.user_invites set status = 'Accepted', accepted_by_user_id = @acceptedByUserId, accepted_at = now(), updated_at = now() where id = @inviteId and status = 'Pending'", new { inviteId, acceptedByUserId }, ct);

    public Task MarkExpiredAsync(DateTime utcNow, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.user_invites set status = 'Expired', updated_at = @utcNow where status = 'Pending' and expires_at <= @utcNow", new { utcNow }, ct);

    private static object ToParameters(UserInvite invite) => new { invite.Id, invite.ClientId, invite.Email, Role = DbEnum.Text(invite.Role), invite.TokenHash, Status = DbEnum.Text(invite.Status), invite.InvitedByUserId, invite.AcceptedByUserId, invite.ExpiresAt, invite.AcceptedAt, invite.CanceledAt, invite.CreatedAt, invite.UpdatedAt };
}
