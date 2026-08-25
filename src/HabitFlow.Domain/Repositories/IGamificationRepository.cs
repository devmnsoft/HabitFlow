namespace HabitFlow.Domain;

public interface IGamificationRepository
{
    Task<WeeklyGoal?> CreateWeeklyGoalAsync(WeeklyGoal goal, IReadOnlyCollection<Guid> habitIds, CancellationToken ct = default);
    Task<IReadOnlyList<WeeklyGoal>> ListWeeklyGoalsAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserAchievement>> ListAchievementsAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<AchievementDefinition>> ListLockedDefinitionsAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<WeeklyGoal>> ApplyCompletionAsync(Guid clientId, Guid userId, Guid habitId, Guid completionId, DateOnly date, CancellationToken ct = default);
    Task<int> CountCompletionsAsync(Guid clientId, Guid userId, CancellationToken ct = default);
    Task<bool> UnlockAsync(Guid clientId, Guid userId, string code, DateTime unlockedAt, CancellationToken ct = default);
    Task<bool> UseFreezeAsync(StreakFreeze freeze, CancellationToken ct = default);
}
