using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class AdminAuditRepository(SqlExecutor db) : IAdminAuditRepository
{
    public Task AddAsync(AdminAuditLog log, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.admin_audit_logs(id,admin_user_id,admin_email,action,target_user_id,target_user_email,reason,metadata,created_at) values(@Id,@AdminUserId,@AdminEmail,@Action,@TargetUserId,@TargetUserEmail,@Reason,@Metadata::jsonb,@CreatedAt)", log, ct);
}
