namespace HabitFlow.Domain;
public enum UserRole { User, Admin } public enum UserPlan { Free, Premium } public enum AccountStatus { Active, Blocked }
public sealed record User(Guid Id,string Name,string Email,string PasswordHash,string Role,string AccountStatus,string Plan,string PlanStatus,bool WantsPremiumNotice,bool OnboardingCompleted,DateTime? AcceptedTermsAt,DateTime? AcceptedPrivacyAt,DateTime? LastLoginAt,DateTime CreatedAt,DateTime UpdatedAt);
public sealed record Habit(Guid Id,Guid UserId,string Name,string Color,string? Category,bool IsArchived,DateTime? ArchivedAt,DateTime CreatedAt,DateTime UpdatedAt);
public sealed record HabitCompletion(Guid Id,Guid HabitId,Guid UserId,DateOnly CompletedDate,DateTime CreatedAt);
public sealed record SupportTicket(Guid Id,Guid UserId,string Protocol,string Type,string Status,string Priority,string? Title,string? Description,string? Source,DateTime CreatedAt,DateTime UpdatedAt,DateTime? ResolvedAt);
public sealed record SupportMessage(Guid Id,Guid TicketId,Guid? UserId,string Role,string Message,bool IsSensitiveBlocked,DateTime CreatedAt);
public sealed record SystemAuditLog(Guid Id,Guid? UserId,string? UserEmail,string Severity,string Source,string Action,string Message,string? Metadata,string? ErrorCode,string? ErrorFingerprint,DateTime CreatedAt,bool ReadByAdmin);
public sealed record AdminAuditLog(Guid Id,Guid AdminUserId,string AdminEmail,string Action,Guid? TargetUserId,string? TargetUserEmail,string? Reason,string? Metadata,DateTime CreatedAt);
public sealed record SystemSetting(string Key,string Value,DateTime UpdatedAt,Guid? UpdatedBy);
public sealed record UserSession(Guid Id,Guid UserId,string SessionHash,DateTime CreatedAt,DateTime ExpiresAt);
public sealed record NotificationLog(Guid Id,Guid? UserId,string Channel,string Severity,string Message,DateTime CreatedAt);
public sealed record LoginAttempt(Guid Id,string Email,bool Success,string? IpAddress,string? UserAgent,DateTime CreatedAt);
public sealed record LgpdRequest(Guid Id,Guid UserId,string Type,string Status,string Protocol,string? Notes,DateTime CreatedAt,DateTime UpdatedAt,DateTime? CompletedAt);
