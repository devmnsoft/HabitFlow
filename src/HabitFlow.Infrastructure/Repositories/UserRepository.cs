using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class UserRepository(SqlExecutor db) : IUserRepository
{
    private const string Columns = "id, name, email, password_hash, photo_url, role, account_status, risk_status, plan, plan_status, wants_premium_notice, onboarding_completed, accepted_terms_at, accepted_privacy_at, last_login_at, last_activity_at, created_at, updated_at";

    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<User>("select " + Columns + " from habitflow.users where id = @id", new { id }, ct);
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => db.QuerySingleOrDefaultAsync<User>("select " + Columns + " from habitflow.users where email = @email", new { email }, ct);
    public async Task<IReadOnlyList<User>> SearchAsync(string? term, CancellationToken ct = default) => (await db.QueryAsync<User>("select " + Columns + " from habitflow.users where @term is null or email ilike @like or name ilike @like order by created_at desc", new { term = string.IsNullOrWhiteSpace(term) ? null : term, like = "%" + term + "%" }, ct)).ToList();
    public Task CreateAsync(User u, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.users(id,name,email,password_hash,photo_url,role,account_status,risk_status,plan,plan_status,wants_premium_notice,onboarding_completed,accepted_terms_at,accepted_privacy_at,last_login_at,last_activity_at,created_at,updated_at) values(@Id,@Name,@Email,@PasswordHash,@PhotoUrl,@Role,@AccountStatus,@RiskStatus,@Plan,@PlanStatus,@WantsPremiumNotice,@OnboardingCompleted,@AcceptedTermsAt,@AcceptedPrivacyAt,@LastLoginAt,@LastActivityAt,@CreatedAt,@UpdatedAt)", ToParameters(u), ct);
    public Task UpdateAsync(User u, CancellationToken ct = default) => db.ExecuteAsync("update habitflow.users set name=@Name, photo_url=@PhotoUrl, role=@Role, account_status=@AccountStatus, risk_status=@RiskStatus, plan=@Plan, plan_status=@PlanStatus, updated_at=@UpdatedAt where id=@Id", ToParameters(u), ct);
    private static object ToParameters(User u) => new
    {
        u.Id,
        u.Name,
        u.Email,
        u.PasswordHash,
        u.PhotoUrl,
        Role = DbEnum.Text(u.Role),
        AccountStatus = DbEnum.Text(u.AccountStatus),
        RiskStatus = DbEnum.Text(u.RiskStatus),
        Plan = DbEnum.Text(u.Plan),
        PlanStatus = DbEnum.Text(u.PlanStatus),
        u.WantsPremiumNotice,
        u.OnboardingCompleted,
        u.AcceptedTermsAt,
        u.AcceptedPrivacyAt,
        u.LastLoginAt,
        u.LastActivityAt,
        u.CreatedAt,
        u.UpdatedAt
    };

    public Task AddLoginAttemptAsync(LoginAttempt a, CancellationToken ct = default) => db.ExecuteAsync("insert into habitflow.login_attempts(id,email,success,ip_address,user_agent,created_at) values(@Id,@Email,@Success,@IpAddress,@UserAgent,@CreatedAt)", a, ct);
}
