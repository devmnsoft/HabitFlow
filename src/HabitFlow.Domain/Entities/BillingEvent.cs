namespace HabitFlow.Domain;

public sealed record BillingEvent(Guid Id, Guid? UserId, string? Provider, string EventType, UserPlan? Plan, string? Status, decimal? Amount, string? Metadata, DateTime CreatedAt);
