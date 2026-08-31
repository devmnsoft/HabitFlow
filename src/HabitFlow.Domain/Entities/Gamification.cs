namespace HabitFlow.Domain;

public sealed record WeeklyGoal(Guid Id, Guid ClientId, Guid UserId, string Name, DateOnly WeekStart,
    DateOnly WeekEnd, int TargetCompletions, int CurrentCompletions, string Status, DateTime CreatedAt, DateTime? CompletedAt);
public sealed record AchievementDefinition(string Code, string Name, string Description, string Icon,
    string Criterion, string Category, string Rarity, bool IsActive);
public sealed record UserAchievement(Guid Id, Guid ClientId, Guid UserId, string AchievementCode,
    string Status, DateTime UnlockedAt, string Name = "", string Description = "", string Icon = "", string Category = "", string Rarity = "");
public sealed record StreakFreeze(Guid Id, Guid ClientId, Guid UserId, Guid HabitId, DateOnly FrozenDate, string? Reason, DateTime CreatedAt);
public sealed record GamificationSnapshot(IReadOnlyList<WeeklyGoal> WeeklyGoals,
    IReadOnlyList<UserAchievement> Achievements, IReadOnlyList<AchievementDefinition> NextAchievements,
    int TotalCompletions, int CurrentStreak, int BestStreak);

public enum LeaderboardScope { Private, Team, General }
public sealed record PointsBalance(int TotalPoints, int TodayPoints, int DailyLimit);
public sealed record LeaderboardPreference(Guid ClientId, Guid UserId, bool IsOptedIn, LeaderboardScope Scope,
    string PublicName, Guid? TeamId, DateTime UpdatedAt);
public sealed record LeaderboardEntry(int Position, string PublicName, int Points, LeaderboardScope Scope);
