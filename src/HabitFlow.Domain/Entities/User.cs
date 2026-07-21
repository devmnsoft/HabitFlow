namespace HabitFlow.Domain;

public sealed record User(Guid Id, string Name, string Email, string PasswordHash, string? PhotoUrl, UserRole Role, AccountStatus AccountStatus, RiskStatus RiskStatus, UserPlan Plan, PlanStatus PlanStatus, bool WantsPremiumNotice, bool OnboardingCompleted, DateTime? AcceptedTermsAt, DateTime? AcceptedPrivacyAt, DateTime? LastLoginAt, DateTime? LastActivityAt, DateTime CreatedAt, DateTime UpdatedAt)
{
    public bool CanUseDashboard => AccountStatus == AccountStatus.Active;
    public bool HasRestrictedAccess => AccountStatus == AccountStatus.Suspended;
    public bool CanManageUsers => Role == UserRole.Admin;
}
