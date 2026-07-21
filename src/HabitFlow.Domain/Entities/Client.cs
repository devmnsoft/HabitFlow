namespace HabitFlow.Domain;

public sealed record Client(Guid Id, string Name, string? LegalName, string? Document, string? Email, string? Phone, string? ContactName, ClientPlan Plan, ClientStatus Status, string? Notes, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record ClientUserSummary(Guid Id, string Name, string Email, string Role, string AccountStatus, DateTime CreatedAt);
public sealed record ClientMetrics(int LinkedUsers, int ActiveHabits, int CompletedHabits);
