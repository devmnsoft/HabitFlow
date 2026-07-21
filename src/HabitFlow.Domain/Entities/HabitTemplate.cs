namespace HabitFlow.Domain;

public sealed record HabitTemplate(Guid Id, Guid ObjectiveId, string Name, string Description, string Category, string SuggestedFrequency, string SuggestedColor, HabitDifficulty Difficulty, int? EstimatedTimeMinutes, string? BenefitText, int SortOrder, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt)
{
    public bool CanBeUsed() => IsActive;
}
