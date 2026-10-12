namespace HabitFlow.Domain;

public interface IPersonalPlanningRepository
{
    Task<IReadOnlyList<PersonalPriorityDetails>> ListPrioritiesAsync(Guid clientId, Guid userId, DateOnly onDate, CancellationToken ct = default);
    Task SavePrioritiesAsync(Guid clientId, Guid userId, IReadOnlyList<PersonalPriority> priorities, CancellationToken ct = default);
    Task<IReadOnlySet<string>> ListDismissedSuggestionKeysAsync(Guid clientId, Guid userId, DateOnly date, CancellationToken ct = default);
    Task DismissSuggestionAsync(Guid clientId, Guid userId, string suggestionKey, DateOnly date, CancellationToken ct = default);
    Task<MonthlyReview?> GetMonthlyReviewAsync(Guid clientId, Guid userId, DateOnly periodStart, CancellationToken ct = default);
    Task<MonthlyReview> UpsertMonthlyReviewAsync(MonthlyReview review, int expectedVersion, CancellationToken ct = default);
}
