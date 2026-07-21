namespace HabitFlow.Application;

public sealed record DashboardDto(string Name, int ActiveHabits, int DoneToday, int DayPercent, int BestStreak, IReadOnlyList<HabitDto> Habits);
