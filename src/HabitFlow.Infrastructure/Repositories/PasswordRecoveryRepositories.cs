using Dapper;
using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class PasswordResetTokenRepository(SqlExecutor db) : IPasswordResetTokenRepository
{
    public Task RevokeActiveAsync(Guid userId, DateTime now, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.password_reset_tokens set revoked_at=@now where user_id=@userId and used_at is null and revoked_at is null", new { userId, now }, ct);
    public Task CreateAsync(PasswordResetToken token, string? ipHash, string? userAgentHash, string correlationId, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.password_reset_tokens(id,user_id,token_hash,expires_at,created_at,requested_ip_hash,requested_user_agent_hash,request_correlation_id) values(@Id,@UserId,@TokenHash,@ExpiresAt,@CreatedAt,@ipHash,@userAgentHash,@correlationId)", new { token.Id, token.UserId, token.TokenHash, token.ExpiresAt, token.CreatedAt, ipHash, userAgentHash, correlationId }, ct);
    public Task<PasswordResetToken?> GetForUpdateAsync(string tokenHash, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<PasswordResetToken>("select id,user_id,token_hash,expires_at,used_at,revoked_at,created_at from habitflow.password_reset_tokens where token_hash=@tokenHash for update", new { tokenHash }, ct);
    public Task MarkUsedAndRevokeOthersAsync(Guid tokenId, Guid userId, DateTime now, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.password_reset_tokens set used_at=case when id=@tokenId then @now else used_at end, revoked_at=case when id<>@tokenId and used_at is null then @now else revoked_at end where user_id=@userId", new { tokenId, userId, now }, ct);
}

public sealed class PasswordResetRequestRepository(SqlExecutor db) : IPasswordResetRequestRepository
{
    public Task<int> CountByEmailHashAsync(string emailHash, DateTime since, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.password_reset_requests where email_hash=@emailHash and created_at>=@since", new { emailHash, since }, ct)!;
    public Task<int> CountByIpHashAsync(string ipHash, DateTime since, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<int>("select count(*) from habitflow.password_reset_requests where ip_hash=@ipHash and created_at>=@since", new { ipHash, since }, ct)!;
    public Task AddAsync(string emailHash, string ipHash, DateTime createdAt, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.password_reset_requests(id,email_hash,ip_hash,created_at) values(@id,@emailHash,@ipHash,@createdAt)", new { id = Guid.NewGuid(), emailHash, ipHash, createdAt }, ct);
}

public sealed class TransactionalEmailOutboxRepository(SqlExecutor db) : ITransactionalEmailOutboxRepository
{
    public Task EnqueueAsync(TransactionalEmailMessage m, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.transactional_email_outbox(id,client_id,user_id,template_code,recipient,subject,payload_json,status,idempotency_key,attempts,next_attempt_at,created_at,updated_at) values(@Id,@ClientId,@UserId,@TemplateCode,@Recipient,@Subject,cast(@PayloadJson as jsonb),@Status,@IdempotencyKey,@Attempts,@NextAttemptAt,@CreatedAt,@CreatedAt) on conflict(idempotency_key) do nothing", new { m.Id,m.ClientId,m.UserId,m.TemplateCode,m.Recipient,m.Subject,m.PayloadJson, Status=m.Status.ToString(),m.IdempotencyKey,m.Attempts,m.NextAttemptAt,m.CreatedAt }, ct);
    public async Task<IReadOnlyList<TransactionalEmailMessage>> ClaimBatchAsync(int size, CancellationToken ct = default) => (await db.QueryAsync<TransactionalEmailMessage>("with claimed as (select id from habitflow.transactional_email_outbox where status in ('Pending','Failed') and next_attempt_at<=now() order by created_at for update skip locked limit @size) update habitflow.transactional_email_outbox o set status='Processing',updated_at=now() from claimed where o.id=claimed.id returning o.id,o.client_id,o.user_id,o.template_code,o.recipient,o.subject,o.payload_json::text payload_json,o.status,o.idempotency_key,o.attempts,o.next_attempt_at,o.created_at", new { size }, ct)).ToList();
    public Task MarkSentAsync(Guid id, DateTime sentAt, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.transactional_email_outbox set status='Sent',sent_at=@sentAt,payload_json='{}'::jsonb,last_error=null,updated_at=@sentAt where id=@id", new { id, sentAt }, ct);
    public Task MarkFailedAsync(Guid id, string sanitizedError, DateTime nextAttempt, int maxAttempts, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.transactional_email_outbox set attempts=attempts+1,status=case when attempts+1>=@maxAttempts then 'DeadLetter' else 'Failed' end,next_attempt_at=@nextAttempt,last_error=left(@sanitizedError,500),updated_at=now() where id=@id", new { id, sanitizedError, nextAttempt, maxAttempts }, ct);
}

public sealed class UserSessionRevocationService(SqlExecutor db) : IUserSessionRevocationService
{
    // Cookie sessions are revoked by the atomic session_version increment on the user.
    public Task RevokeAsync(Guid userId, CancellationToken ct = default) => Task.CompletedTask;
}
