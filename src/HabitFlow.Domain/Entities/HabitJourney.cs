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

public sealed record HabitJourneyLinkedHabit(Guid StepId, Guid HabitId, string HabitName, string Status, DateTime CreatedAt);

public sealed record HabitJourneyProgress(
    int ConfirmedHabits, int CompletedActivities, int ExpectedActivities, decimal Percentage,
    string Explanation, bool HasInsufficientSample);

public sealed record HabitJourneyDetails(
    HabitJourney Journey,
    IReadOnlyList<HabitJourneyStep> Steps,
    HabitJourneyMember? Membership,
    IReadOnlyList<HabitJourneyLinkedHabit> LinkedHabits,
    HabitJourneyProgress Progress);

public sealed record JoinHabitJourneyHabitSelection(
    Guid StepId, string HabitName, HabitFrequencyType FrequencyType, int? TargetPerWeek,
    IReadOnlyCollection<int> SelectedDays, TimeOnly? ReminderTime);

public sealed record JoinHabitJourneyCommand(
    Guid ClientId, Guid UserId, Guid JourneyId, bool Confirmed,
    IReadOnlyList<JoinHabitJourneyHabitSelection> HabitSelections, string CorrelationId);
