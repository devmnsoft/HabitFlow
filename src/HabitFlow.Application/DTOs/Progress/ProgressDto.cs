namespace HabitFlow.Application;

public sealed record ProgressDto(int TotalHabits, int TotalCompletions, int BestStreak, int Rate7Days, int Rate30Days);
