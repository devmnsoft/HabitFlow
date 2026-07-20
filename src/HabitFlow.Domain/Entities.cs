namespace HabitFlow.Domain;

public enum UserRole { User, Admin }
public enum AccountStatus { Active, Blocked, Suspended, DeletedPending }
public enum RiskStatus { Normal, Watchlist, Suspicious }
public enum UserPlan { Free, Premium }
public enum PlanStatus { Active, Trial, Canceled, Inactive, PastDue }
public enum TicketStatus { Open, InProgress, Resolved, Closed }
public enum LgpdRequestType { Export, Delete }
public enum LgpdRequestStatus { Requested, InReview, Processing, Completed, Rejected, Canceled }
public enum AuditSeverity { Info, Warning, Error, Critical }

public sealed record User(Guid Id, string Name, string Email, string PasswordHash, string? PhotoUrl, UserRole Role, AccountStatus AccountStatus, RiskStatus RiskStatus, UserPlan Plan, PlanStatus PlanStatus, bool WantsPremiumNotice, bool OnboardingCompleted, DateTime? AcceptedTermsAt, DateTime? AcceptedPrivacyAt, DateTime? LastLoginAt, DateTime? LastActivityAt, DateTime CreatedAt, DateTime UpdatedAt)
{
    public bool CanUseDashboard => AccountStatus == AccountStatus.Active;
    public bool HasRestrictedAccess => AccountStatus == AccountStatus.Suspended;
    public bool CanManageUsers => Role == UserRole.Admin;
}
public sealed record Habit(Guid Id, Guid UserId, string Name, string Color, string? Category, bool IsArchived, DateTime? ArchivedAt, DateTime CreatedAt, DateTime UpdatedAt)
{ public bool BelongsTo(Guid userId) => UserId == userId; }
public sealed record HabitCompletion(Guid Id, Guid HabitId, Guid UserId, DateOnly CompletedDate, DateTime CreatedAt);
public sealed record SupportTicket(Guid Id, Guid UserId, string Protocol, string Type, TicketStatus Status, string Priority, string Title, string? Description, string? Source, DateTime CreatedAt, DateTime UpdatedAt, DateTime? ResolvedAt);
public sealed record SupportMessage(Guid Id, Guid TicketId, Guid? UserId, string Role, string Message, bool IsSensitiveBlocked, DateTime CreatedAt);
public sealed record SystemAuditLog(Guid Id, Guid? UserId, string? UserEmail, AuditSeverity Severity, string Source, string Action, string Message, string? Metadata, string? ErrorCode, string? ErrorFingerprint, DateTime CreatedAt, bool ReadByAdmin);
public sealed record AdminAuditLog(Guid Id, Guid? AdminUserId, string? AdminEmail, string Action, Guid? TargetUserId, string? TargetUserEmail, string? Reason, string? Metadata, DateTime CreatedAt);
public sealed record SystemSetting(string Key, string Value, DateTime UpdatedAt, Guid? UpdatedBy);
public sealed record LoginAttempt(Guid Id, string? Email, bool Success, string? IpAddress, string? UserAgent, DateTime CreatedAt);
public sealed record LgpdRequest(Guid Id, Guid UserId, string Protocol, LgpdRequestType Type, LgpdRequestStatus Status, string? Notes, string? RejectionReason, Guid? HandledBy, DateTime CreatedAt, DateTime UpdatedAt, DateTime? CompletedAt);
public sealed record BillingEvent(Guid Id, Guid? UserId, string? Provider, string EventType, UserPlan? Plan, string? Status, decimal? Amount, string? Metadata, DateTime CreatedAt);

public static class DomainPolicies
{
    public const int FreePlanActiveHabitLimit = 5;
    public static bool CanCreateHabit(User user, int activeHabits) => user.Plan == UserPlan.Premium || activeHabits < FreePlanActiveHabitLimit;
    public static bool CanChangeHabit(User user, Habit habit) => user.Role == UserRole.Admin || habit.UserId == user.Id;
}
