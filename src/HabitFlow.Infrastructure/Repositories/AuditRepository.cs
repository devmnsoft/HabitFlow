using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class AuditRepository(SqlExecutor db) : IAuditRepository
{
    public Task AddSystemAsync(SystemAuditLog log, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.system_audit_logs(id,user_id,user_email,severity,source,action,message,metadata,error_code,error_fingerprint,created_at,read_by_admin) values(@Id,@UserId,@UserEmail,@Severity::text,@Source,@Action,@Message,@Metadata::jsonb,@ErrorCode,@ErrorFingerprint,@CreatedAt,@ReadByAdmin)", log, ct);
    public async Task<IReadOnlyList<SystemAuditLog>> RecentAsync(CancellationToken ct = default) => (await db.QueryAsync<SystemAuditLog>("select id, user_id, user_email, severity, source, action, message, metadata, error_code, error_fingerprint, created_at, read_by_admin from habitflow.system_audit_logs order by created_at desc limit 200", null, ct)).ToList();
}
