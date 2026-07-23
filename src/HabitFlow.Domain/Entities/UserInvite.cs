namespace HabitFlow.Domain;

public sealed record UserInvite(
    Guid Id,
    Guid ClientId,
    string Email,
    UserRole Role,
    string TokenHash,
    UserInviteStatus Status,
    Guid? InvitedByUserId,
    Guid? AcceptedByUserId,
    DateTime ExpiresAt,
    DateTime? AcceptedAt,
    DateTime? CanceledAt,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public bool IsPending(DateTime utcNow) => Status == UserInviteStatus.Pending && ExpiresAt > utcNow;
}

public enum UserInviteStatus
{
    Pending,
    Accepted,
    Expired,
    Canceled
}
