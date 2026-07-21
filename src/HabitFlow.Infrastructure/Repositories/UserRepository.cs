using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class UserRepository(SqlExecutor db) : IUserRepository
{
    private const string Columns = "id, name, email, password_hash, photo_url, role, account_status, risk_status, plan, plan_status, wants_premium_notice, onboarding_completed, accepted_terms_at, accepted_privacy_at, last_login_at, last_activity_at, created_at, updated_at";

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<User>("select " + Columns + " from habitflow.users where id = @id", new { id }, ct);
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<User>("select " + Columns + " from habitflow.users where email = @email", new { email }, ct);
    public async Task<IReadOnlyList<User>> SearchAsync(string? term, CancellationToken ct = default) => (await db.QueryAsync<User>("select " + Columns + " from habitflow.users where @term is null or email ilike @like or name ilike @like order by created_at desc", new { term = string.IsNullOrWhiteSpace(term) ? null : term, like = "%" + term + "%" }, ct)).ToList();
    public Task CreateAsync(User u, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.users(id,name,email,password_hash,photo_url,role,account_status,risk_status,plan,plan_status,wants_premium_notice,onboarding_completed,accepted_terms_at,accepted_privacy_at,last_login_at,last_activity_at,created_at,updated_at) values(@Id,@Name,@Email,@PasswordHash,@PhotoUrl,@Role::text,@AccountStatus::text,@RiskStatus::text,@Plan::text,@PlanStatus::text,@WantsPremiumNotice,@OnboardingCompleted,@AcceptedTermsAt,@AcceptedPrivacyAt,@LastLoginAt,@LastActivityAt,@CreatedAt,@UpdatedAt)", u, ct);
    public Task UpdateAsync(User u, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.users set name=@Name, photo_url=@PhotoUrl, role=@Role::text, account_status=@AccountStatus::text, risk_status=@RiskStatus::text, plan=@Plan::text, plan_status=@PlanStatus::text, updated_at=@UpdatedAt where id=@Id", u, ct);
    public Task AddLoginAttemptAsync(LoginAttempt a, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.login_attempts(id,email,success,ip_address,user_agent,created_at) values(@Id,@Email,@Success,@IpAddress,@UserAgent,@CreatedAt)", a, ct);
}
