using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class UserSessionRepository(SqlExecutor db) : IUserSessionRepository
{
    private const string Columns = "id,user_id,client_id,user_agent,ip_address,created_at,last_activity_at,expires_at,revoked_at,revocation_reason";

    public Task CreateAsync(UserSession s, CancellationToken ct = default) => db.ExecuteAsync(
        "insert into habitflow.user_sessions(id,user_id,client_id,user_agent,ip_address,created_at,last_activity_at,expires_at,revoked_at,revocation_reason) values(@Id,@UserId,@ClientId,@UserAgent,@IpAddress,@CreatedAt,@LastActivityAt,@ExpiresAt,@RevokedAt,@RevocationReason)", s, ct);

    public async Task<UserSession?> GetOwnedAsync(Guid id, Guid userId, Guid? clientId, CancellationToken ct = default) =>
        Map(await db.QuerySingleOrDefaultAsync<UserSessionRow>($"select {Columns} from habitflow.user_sessions where id=@id and user_id=@userId and client_id is not distinct from @clientId", new { id, userId, clientId }, ct));

    public async Task<UserSession?> GetActiveOwnedAsync(Guid id, Guid userId, Guid? clientId, DateTime utcNow, CancellationToken ct = default) =>
        Map(await db.QuerySingleOrDefaultAsync<UserSessionRow>($"select {Columns} from habitflow.user_sessions where id=@id and user_id=@userId and client_id is not distinct from @clientId and revoked_at is null and expires_at>@utcNow", new { id, userId, clientId, utcNow }, ct));

    public async Task<IReadOnlyList<UserSession>> ListActiveAsync(Guid userId, Guid? clientId, CancellationToken ct = default) => (await db.QueryAsync<UserSessionRow>(
        $"select {Columns} from habitflow.user_sessions where user_id=@userId and client_id is not distinct from @clientId and revoked_at is null and expires_at>now() order by last_activity_at desc", new { userId, clientId }, ct)).Select(row => Map(row)!).ToList();

    public Task TouchAsync(Guid id, Guid userId, Guid? clientId, DateTime occurredAt, TimeSpan minimumInterval, CancellationToken ct = default) => db.ExecuteAsync(
        "update habitflow.user_sessions set last_activity_at=@occurredAt where id=@id and user_id=@userId and client_id is not distinct from @clientId and revoked_at is null and expires_at>@occurredAt and last_activity_at <= @occurredAt - @minimumInterval", new { id, userId, clientId, occurredAt, minimumInterval }, ct);

    public Task RevokeAsync(Guid id, Guid userId, string reason, CancellationToken ct = default) => db.ExecuteAsync(
        "update habitflow.user_sessions set revoked_at=coalesce(revoked_at,now()),revocation_reason=coalesce(revocation_reason,@reason) where id=@id and user_id=@userId", new { id, userId, reason }, ct);

    public Task RevokeAllAsync(Guid userId, Guid? exceptSessionId, string reason, CancellationToken ct = default) => db.ExecuteAsync(
        "update habitflow.user_sessions set revoked_at=now(),revocation_reason=@reason where user_id=@userId and revoked_at is null and (@exceptSessionId is null or id<>@exceptSessionId)", new { userId, exceptSessionId, reason }, ct);

    private static UserSession? Map(UserSessionRow? row) => row is null ? null : new(row.Id, row.UserId, row.ClientId, row.UserAgent, row.IpAddress, Utc(row.CreatedAt), Utc(row.LastActivityAt), Utc(row.ExpiresAt), row.RevokedAt is null ? null : Utc(row.RevokedAt.Value), row.RevocationReason);
    private static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private sealed record UserSessionRow(Guid Id, Guid UserId, Guid? ClientId, string UserAgent, string IpAddress, DateTime CreatedAt, DateTime LastActivityAt, DateTime ExpiresAt, DateTime? RevokedAt, string? RevocationReason);
}
