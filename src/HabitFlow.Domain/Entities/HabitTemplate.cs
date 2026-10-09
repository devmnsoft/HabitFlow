namespace HabitFlow.Domain;

public sealed record HabitTemplateDraft(
    Guid Id, Guid ObjectiveId, Guid? ClientId, Guid? CreatedBy, string Name, string Description,
    string Category, string Frequency, string Difficulty, int Minutes, string Audience, string Goal, string MinimumPlan);

public sealed record HabitTemplate(Guid Id, Guid ObjectiveId, string Name, string Description, string Category, string SuggestedFrequency, string SuggestedColor, HabitDifficulty Difficulty, int? EstimatedTimeMinutes, string? BenefitText, int SortOrder, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt,
    SuggestedWeekDays SuggestedDays = SuggestedWeekDays.EveryDay,
    int? SuggestedTargetPerWeek = null,
    TimeOnly? SuggestedReminderTime = null,
    string? IconCode = null,
    string? WhyItHelps = null,
    string? HowToStart = null,
    string? FirstAction = null,
    string[]? Tags = null,
    string MinimumPlanCode = "free",
    bool IsFeatured = false,
    int ContentVersion = 1,
    DateTime? PublishedAt = null,
    string? Audience = null,
    string? GoalText = null,
    Guid? CreatedBy = null,
    Guid? ClientId = null,
    string Language = "pt-BR",
    string Origin = "Official",
    string MarketplaceStatus = "Published")
{
    public bool CanBeUsed() => IsActive;

    public bool IsSuggestedOn(DayOfWeek day) => (SuggestedDays & (day switch
    {
        DayOfWeek.Sunday => SuggestedWeekDays.Sunday,
        DayOfWeek.Monday => SuggestedWeekDays.Monday,
        DayOfWeek.Tuesday => SuggestedWeekDays.Tuesday,
        DayOfWeek.Wednesday => SuggestedWeekDays.Wednesday,
        DayOfWeek.Thursday => SuggestedWeekDays.Thursday,
        DayOfWeek.Friday => SuggestedWeekDays.Friday,
        DayOfWeek.Saturday => SuggestedWeekDays.Saturday,
        _ => SuggestedWeekDays.None
    })) != 0;
}
