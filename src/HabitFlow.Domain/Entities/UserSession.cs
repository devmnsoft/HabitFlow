namespace HabitFlow.Domain;

public sealed record UserSession(
    Guid Id,
    Guid UserId,
    Guid? ClientId,
    string UserAgent,
    string IpAddress,
    DateTime CreatedAt,
    DateTime LastActivityAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    string? RevocationReason)
{
    public bool IsActive(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow;
}
