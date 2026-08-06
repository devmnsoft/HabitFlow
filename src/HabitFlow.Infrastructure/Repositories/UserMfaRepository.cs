using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class UserMfaRepository(SqlExecutor db) : IUserMfaRepository
{
    public async Task<UserMfaSetting?> GetAsync(Guid userId, Guid? clientId, CancellationToken ct = default)
    {
        var row = await db.QuerySingleOrDefaultAsync<SettingRow>("select user_id,client_id,protected_secret,is_enabled,last_accepted_time_step,created_at,enabled_at from habitflow.user_mfa_settings where user_id=@userId and client_id is not distinct from @clientId", new { userId, clientId }, ct);
        return row is null ? null : new(row.UserId, row.ClientId, row.ProtectedSecret, row.IsEnabled, row.LastAcceptedTimeStep, row.CreatedAt, row.EnabledAt);
    }

    public Task SavePendingAsync(Guid userId, Guid? clientId, string protectedSecret, DateTime createdAt, CancellationToken ct = default) => db.ExecuteAsync(
        "insert into habitflow.user_mfa_settings(user_id,client_id,protected_secret,is_enabled,created_at) values(@userId,@clientId,@protectedSecret,false,@createdAt) on conflict(user_id) do update set client_id=excluded.client_id,protected_secret=excluded.protected_secret,is_enabled=false,last_accepted_time_step=null,created_at=excluded.created_at,enabled_at=null", new { userId, clientId, protectedSecret, createdAt }, ct);

    public async Task<bool> EnableAsync(Guid userId, Guid? clientId, long timeStep, DateTime enabledAt, CancellationToken ct = default) => await db.ExecuteAsync(
        "update habitflow.user_mfa_settings set is_enabled=true,last_accepted_time_step=@timeStep,enabled_at=@enabledAt where user_id=@userId and client_id is not distinct from @clientId and is_enabled=false", new { userId, clientId, timeStep, enabledAt }, ct) == 1;

    public async Task<bool> AcceptTimeStepAsync(Guid userId, Guid? clientId, long timeStep, CancellationToken ct = default) => await db.ExecuteAsync(
        "update habitflow.user_mfa_settings set last_accepted_time_step=@timeStep where user_id=@userId and client_id is not distinct from @clientId and is_enabled=true and (last_accepted_time_step is null or last_accepted_time_step<@timeStep)", new { userId, clientId, timeStep }, ct) == 1;

    public Task DisableAsync(Guid userId, Guid? clientId, DateTime occurredAt, CancellationToken ct = default) => db.ExecuteAsync(
        "update habitflow.user_mfa_settings set is_enabled=false,protected_secret='',last_accepted_time_step=null,enabled_at=null where user_id=@userId and client_id is not distinct from @clientId; delete from habitflow.user_mfa_recovery_codes where user_id=@userId and client_id is not distinct from @clientId", new { userId, clientId, occurredAt }, ct);

    public Task ReplaceRecoveryCodesAsync(Guid userId, Guid? clientId, IReadOnlyCollection<string> hashes, DateTime createdAt, CancellationToken ct = default) => db.ExecuteAsync(
        "delete from habitflow.user_mfa_recovery_codes where user_id=@userId and client_id is not distinct from @clientId; insert into habitflow.user_mfa_recovery_codes(id,user_id,client_id,code_hash,created_at) select gen_random_uuid(),@userId,@clientId,value,@createdAt from unnest(@hashes) value", new { userId, clientId, hashes = hashes.ToArray(), createdAt }, ct);

    public async Task<bool> ConsumeRecoveryCodeAsync(Guid userId, Guid? clientId, string hash, DateTime usedAt, CancellationToken ct = default) => await db.ExecuteAsync(
        "update habitflow.user_mfa_recovery_codes set used_at=@usedAt where user_id=@userId and client_id is not distinct from @clientId and code_hash=@hash and used_at is null", new { userId, clientId, hash, usedAt }, ct) == 1;

    public async Task<UserMfaChallenge> CreateChallengeAsync(Guid userId, Guid? clientId, DateTime expiresAt, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        await db.ExecuteAsync("insert into habitflow.user_mfa_challenges(id,user_id,client_id,failed_attempts,expires_at) values(@id,@userId,@clientId,0,@expiresAt)", new { id, userId, clientId, expiresAt }, ct);
        return new(id, userId, clientId, 0, expiresAt, null);
    }

    public async Task<UserMfaChallenge?> GetChallengeAsync(Guid id, Guid userId, Guid? clientId, CancellationToken ct = default)
    {
        var row = await db.QuerySingleOrDefaultAsync<ChallengeRow>("select id,user_id,client_id,failed_attempts,expires_at,verified_at from habitflow.user_mfa_challenges where id=@id and user_id=@userId and client_id is not distinct from @clientId", new { id, userId, clientId }, ct);
        return row is null ? null : new(row.Id, row.UserId, row.ClientId, row.FailedAttempts, row.ExpiresAt, row.VerifiedAt);
    }
    public Task RegisterChallengeFailureAsync(Guid id, Guid userId, Guid? clientId, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.user_mfa_challenges set failed_attempts=failed_attempts+1 where id=@id and user_id=@userId and client_id is not distinct from @clientId and verified_at is null", new { id, userId, clientId }, ct);
    public Task VerifyChallengeAsync(Guid id, Guid userId, Guid? clientId, DateTime verifiedAt, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.user_mfa_challenges set verified_at=@verifiedAt where id=@id and user_id=@userId and client_id is not distinct from @clientId and verified_at is null", new { id, userId, clientId, verifiedAt }, ct);
    public Task AddSecurityEventAsync(Guid userId, Guid? clientId, string eventType, DateTime occurredAt, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.user_security_events(id,user_id,client_id,event_type,occurred_at) values(gen_random_uuid(),@userId,@clientId,@eventType,@occurredAt)", new { userId, clientId, eventType, occurredAt }, ct);

    private sealed record SettingRow(Guid UserId, Guid? ClientId, string ProtectedSecret, bool IsEnabled, long? LastAcceptedTimeStep, DateTime CreatedAt, DateTime? EnabledAt);
    private sealed record ChallengeRow(Guid Id, Guid UserId, Guid? ClientId, int FailedAttempts, DateTime ExpiresAt, DateTime? VerifiedAt);
}
