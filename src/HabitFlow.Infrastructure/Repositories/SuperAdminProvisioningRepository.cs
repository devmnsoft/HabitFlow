using HabitFlow.Application;
using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class SuperAdminProvisioningRepository(SqlExecutor db, IUnitOfWork unitOfWork) : ISuperAdminProvisioningRepository
{
    private const string UserColumns = "id,name,email,password_hash,photo_url,role,account_status,risk_status,plan,plan_status,wants_premium_notice,onboarding_completed,accepted_terms_at,accepted_privacy_at,last_login_at,last_activity_at,created_at,updated_at,client_id,session_version,must_change_password";

    public Task<User?> FindByEmailAsync(string email, CancellationToken ct) => db.QuerySingleOrDefaultAsync<User>($"select {UserColumns} from habitflow.users where email=@email", new { email }, ct);

    public async Task<User> CreateOrPromoteAsync(string name, string email, string passwordHash, bool mustChangePassword, string actor, string reason, string correlationId, CancellationToken ct)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var existing = await FindByEmailAsync(email, ct);
            var id = existing?.Id ?? Guid.NewGuid();
            await db.ExecuteAsync("""
                insert into habitflow.users(id,name,email,password_hash,role,account_status,risk_status,plan,plan_status,onboarding_completed,created_at,updated_at,client_id,session_version,must_change_password)
                values(@id,@name,@email,@passwordHash,'SuperAdmin','Active','Normal','Free','Active',true,now(),now(),null,1,@mustChangePassword)
                on conflict(email) do update set name=@name,password_hash=@passwordHash,role='SuperAdmin',account_status='Active',client_id=null,session_version=habitflow.users.session_version+1,must_change_password=@mustChangePassword,updated_at=now()
                """, new { id, name, email, passwordHash, mustChangePassword }, ct);
            await db.ExecuteAsync("update habitflow.password_reset_tokens set revoked_at=now() where user_id=@id and used_at is null and revoked_at is null", new { id }, ct);
            await EnsureAuthorityAsync(id, ct);
            await AuditAsync(existing is null ? "superadmin.created" : "superadmin.promoted", id, email, actor, reason, correlationId, ct);
            await unitOfWork.CommitAsync(ct);
            return (await FindByEmailAsync(email, ct))!;
        }
        catch { await unitOfWork.RollbackAsync(ct); throw; }
    }

    public async Task<User?> PromoteAsync(string email, string actor, string reason, string correlationId, CancellationToken ct)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var user = await FindByEmailAsync(email, ct);
            if (user is null) { await unitOfWork.RollbackAsync(ct); return null; }
            await db.ExecuteAsync("update habitflow.users set role='SuperAdmin',client_id=null,account_status='Active',session_version=session_version+1,updated_at=now() where id=@id", new { user.Id }, ct);
            await EnsureAuthorityAsync(user.Id, ct);
            await AuditAsync("superadmin.promoted", user.Id, email, actor, reason, correlationId, ct);
            await unitOfWork.CommitAsync(ct);
            return await FindByEmailAsync(email, ct);
        }
        catch { await unitOfWork.RollbackAsync(ct); throw; }
    }

    public async Task ResetPasswordAsync(Guid userId, string passwordHash, string actor, string reason, string correlationId, CancellationToken ct)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            var user = await db.QuerySingleOrDefaultAsync<User>($"select {UserColumns} from habitflow.users where id=@userId and role='SuperAdmin' and client_id is null for update", new { userId }, ct) ?? throw new InvalidOperationException("Super Administrador global não encontrado.");
            await db.ExecuteAsync("update habitflow.users set password_hash=@passwordHash,session_version=session_version+1,must_change_password=false,updated_at=now() where id=@userId", new { userId, passwordHash }, ct);
            await db.ExecuteAsync("update habitflow.password_reset_tokens set revoked_at=now() where user_id=@userId and used_at is null and revoked_at is null", new { userId }, ct);
            await AuditAsync("superadmin.password_reset", userId, user.Email, actor, reason, correlationId, ct);
            await AuditAsync("superadmin.session_revoked", userId, user.Email, actor, reason, correlationId, ct);
            await unitOfWork.CommitAsync(ct);
        }
        catch { await unitOfWork.RollbackAsync(ct); throw; }
    }

    private async Task EnsureAuthorityAsync(Guid userId, CancellationToken ct)
    {
        await db.ExecuteAsync("insert into habitflow.role_permissions(role_id,permission_code) select id,'Platform.FullAccess' from habitflow.roles where code='super_admin' on conflict do nothing", null, ct);
        await db.ExecuteAsync("""
            insert into habitflow.user_role_assignments(id,user_id,role_id,client_id,created_at)
            select gen_random_uuid(),@userId,id,null,now() from habitflow.roles r where r.code='super_admin'
            and not exists(select 1 from habitflow.user_role_assignments a where a.user_id=@userId and a.role_id=r.id and a.client_id is null and a.revoked_at is null)
            """, new { userId }, ct);
    }

    private Task AuditAsync(string action, Guid userId, string email, string actor, string reason, string correlationId, CancellationToken ct)
    {
        var masked = email.Length < 3 ? "***" : $"{email[0]}***{email[email.IndexOf('@')..]}";
        return db.ExecuteAsync("insert into habitflow.system_audit_logs(id,user_id,user_email,severity,source,action,message,metadata,created_at) values(gen_random_uuid(),@userId,@masked,'Info','AdminCli',@action,@action,cast(@metadata as jsonb),now())",
            new { action, userId, masked, metadata = System.Text.Json.JsonSerializer.Serialize(new { actor, reason, correlationId, occurredAtUtc = DateTime.UtcNow }) }, ct);
    }
}
