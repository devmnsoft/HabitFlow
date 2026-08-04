using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class UserSessionRepository(SqlExecutor db) : IUserSessionRepository
{
    private const string Columns = "id,user_id,client_id,user_agent,ip_address,created_at,last_activity_at,expires_at,revoked_at,revocation_reason";

    public Task CreateAsync(UserSession s, CancellationToken ct = default) => db.ExecuteAsync(
        "insert into habitflow.user_sessions(id,user_id,client_id,user_agent,ip_address,created_at,last_activity_at,expires_at,revoked_at,revocation_reason) values(@Id,@UserId,@ClientId,@UserAgent,@IpAddress,@CreatedAt,@LastActivityAt,@ExpiresAt,@RevokedAt,@RevocationReason)", s, ct);

    public Task<UserSession?> GetOwnedAsync(Guid id, Guid userId, Guid? clientId, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<UserSession>(
        $"select {Columns} from habitflow.user_sessions where id=@id and user_id=@userId and client_id is not distinct from @clientId", new { id, userId, clientId }, ct);

    public async Task<IReadOnlyList<UserSession>> ListActiveAsync(Guid userId, Guid? clientId, CancellationToken ct = default) => (await db.QueryAsync<UserSession>(
        $"select {Columns} from habitflow.user_sessions where user_id=@userId and client_id is not distinct from @clientId and revoked_at is null and expires_at>now() order by last_activity_at desc", new { userId, clientId }, ct)).ToList();

    public Task TouchAsync(Guid id, Guid userId, DateTime occurredAt, CancellationToken ct = default) => db.ExecuteAsync(
        "update habitflow.user_sessions set last_activity_at=@occurredAt where id=@id and user_id=@userId and revoked_at is null and last_activity_at < @occurredAt - interval '1 minute'", new { id, userId, occurredAt }, ct);

    public Task RevokeAsync(Guid id, Guid userId, string reason, CancellationToken ct = default) => db.ExecuteAsync(
        "update habitflow.user_sessions set revoked_at=coalesce(revoked_at,now()),revocation_reason=coalesce(revocation_reason,@reason) where id=@id and user_id=@userId", new { id, userId, reason }, ct);

    public Task RevokeAllAsync(Guid userId, Guid? exceptSessionId, string reason, CancellationToken ct = default) => db.ExecuteAsync(
        "update habitflow.user_sessions set revoked_at=now(),revocation_reason=@reason where user_id=@userId and revoked_at is null and (@exceptSessionId is null or id<>@exceptSessionId)", new { userId, exceptSessionId, reason }, ct);

    public Task IncrementSessionVersionAsync(Guid userId, CancellationToken ct = default) => db.ExecuteAsync(
        "update habitflow.users set session_version=session_version+1,updated_at=now() where id=@userId", new { userId }, ct);
}
