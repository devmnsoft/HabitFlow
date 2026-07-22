using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class AuditRepository(SqlExecutor db) : IAuditRepository
{
    public Task AddSystemAsync(SystemAuditLog log, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.system_audit_logs(id,user_id,user_email,severity,source,action,message,metadata,error_code,error_fingerprint,created_at,read_by_admin) values(@Id,@UserId,@UserEmail,@Severity,@Source,@Action,@Message,@Metadata::jsonb,@ErrorCode,@ErrorFingerprint,@CreatedAt,@ReadByAdmin)", new { log.Id, log.UserId, log.UserEmail, Severity = DbEnum.Text(log.Severity), log.Source, log.Action, log.Message, log.Metadata, log.ErrorCode, log.ErrorFingerprint, log.CreatedAt, log.ReadByAdmin }, ct);
    public async Task<IReadOnlyList<SystemAuditLog>> RecentAsync(CancellationToken ct = default) => (await db.QueryAsync<SystemAuditLog>("select id, user_id, user_email, severity, source, action, message, metadata, error_code, error_fingerprint, created_at, read_by_admin from habitflow.system_audit_logs order by created_at desc limit 200", null, ct)).ToList();
}
