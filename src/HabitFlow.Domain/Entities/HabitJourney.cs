namespace HabitFlow.Domain;

public sealed record HabitJourney(
    Guid Id, Guid? ClientId, string Name, string Description, string Category, string Goal,
    int SuggestedDurationDays, string Frequency, string Difficulty, string[] Tags,
    string? TargetAudience, string MinimumPlan, bool IsActive, bool IsOfficial,
    DateTime CreatedAt, DateTime UpdatedAt);

public sealed record HabitJourneyStep(
    Guid Id, Guid JourneyId, int StepOrder, string Title, string Description,
    string SuggestedHabitName, string SuggestedFrequency, TimeOnly? SuggestedReminderTime);

public sealed record HabitJourneyMember(
    Guid Id, Guid JourneyId, Guid ClientId, Guid UserId, string Status,
    decimal ProgressPercentage, DateTime JoinedAt, DateTime? CompletedAt);

public sealed record HabitJourneyDetails(HabitJourney Journey, IReadOnlyList<HabitJourneyStep> Steps, HabitJourneyMember? Membership);

public sealed record JoinHabitJourneyCommand(Guid ClientId, Guid UserId, Guid JourneyId, bool Confirmed, string CorrelationId);
