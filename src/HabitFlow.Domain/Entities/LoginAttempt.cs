namespace HabitFlow.Domain;

public sealed record LoginAttempt(Guid Id, string? Email, bool Success, string? IpAddress, string? UserAgent, DateTime CreatedAt);
