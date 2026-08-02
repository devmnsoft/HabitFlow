namespace HabitFlow.Domain;

public enum OnboardingStatus { NotStarted, InProgress, Completed, Skipped }
public enum OnboardingStep { Objective = 1, Availability, Preferences, Recommendations, Review, Confirmation, Completed }

public sealed record UserOnboardingProgress(
    Guid ClientId, Guid UserId, OnboardingStep CurrentStep, string? SelectedObjectiveSlug,
    int? AvailableMinutes, string? PreferredFrequency, int[] PreferredDays,
    TimeOnly? PreferredTime, Guid[] SelectedTemplateIds, Guid? SelectedCollectionId,
    bool CreateGoal, string? GoalTargetType, decimal? GoalTargetValue,
    DateTime StartedAt, DateTime LastActivityAt, DateTime? CompletedAt,
    DateTime? SkippedAt, int Version)
{
    public OnboardingStatus Status => CompletedAt.HasValue ? OnboardingStatus.Completed
        : SkippedAt.HasValue ? OnboardingStatus.Skipped : OnboardingStatus.InProgress;
}

public interface IUserOnboardingProgressRepository
{
    Task<UserOnboardingProgress?> GetAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<UserOnboardingProgress> StartOrRestartAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<bool> SaveAsync(UserOnboardingProgress progress, int expectedVersion, CancellationToken ct = default);
}

public sealed record UserOnboardingDraftItem(Guid Id, Guid ClientId, Guid UserId, Guid TemplateId,
    Guid? CollectionId, string Name, string Frequency, int[] Days, int? TargetPerWeek,
    TimeOnly? PreferredTime, string Color, string? Category, bool IsRequired, int SortOrder);

public interface IUserOnboardingDraftRepository
{
    Task<IReadOnlyList<UserOnboardingDraftItem>> ListAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task ReplaceAsync(Guid clientId, Guid userId, IReadOnlyCollection<UserOnboardingDraftItem> items, CancellationToken ct = default);
    Task DeleteAsync(Guid clientId, Guid userId, CancellationToken ct = default);
}
