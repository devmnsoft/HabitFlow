namespace HabitFlow.Domain;
public sealed record UserGoal(Guid Id, Guid ClientId, Guid UserId, string? ObjectiveSlug, string Title, string? Description, string TargetType, int TargetValue, int CurrentValue, DateOnly StartDate, DateOnly? EndDate, string Status, string? Color, string? Icon, DateTime CreatedAt, DateTime UpdatedAt, DateTime? CompletedAt);
