namespace HabitFlow.Application;

public enum ProgressDayStatus { NoSchedule, Future, NotStarted, Partial, Completed }

public sealed record ProgressCalendarDayViewModel(DateOnly Date, int DayNumber, bool IsCurrentMonth, bool IsToday,
    bool IsFuture, bool HasSchedule, int ScheduledCount, int CompletedCount, int PendingCount,
    decimal CompletionPercentage, ProgressDayStatus Status, string AccessibilityLabel, string DetailUrl);

public sealed record ProgressSummaryViewModel(int ScheduledCount, int CompletedCount, int PendingCount,
    decimal CompletionPercentage, int ActiveDays, int CompletedDays, int PartialDays, int CurrentStreak, int BestStreak);

public sealed record ProgressCalendarViewModel(int Year, int Month, string MonthName, DateOnly PeriodStart,
    DateOnly PeriodEnd, int PreviousYear, int PreviousMonth, int NextYear, int NextMonth, bool CanGoPrevious,
    bool CanGoNext, DateOnly Today, int CurrentStreak, int BestStreak, int ScheduledCount, int CompletedCount,
    int PendingCount, decimal CompletionPercentage, int ActiveDays, int CompletedDays, int PartialDays,
    IReadOnlyList<ProgressCalendarDayViewModel> Days, string PlanCode, bool HasFullHistory,
    DateOnly? HistoryLimitStart, bool IsCurrentMonth, string InsightMessage);

public sealed record ProgressHabitStatusViewModel(Guid HabitId, string Name, string? Category, TimeOnly? SuggestedTime, bool Completed);
public sealed record ProgressDayDetailViewModel(DateOnly Date, int ScheduledCount, int CompletedCount, int PendingCount,
    decimal CompletionPercentage, ProgressDayStatus Status, IReadOnlyList<ProgressHabitStatusViewModel> Habits);
public sealed record ProgressComparisonViewModel(ProgressSummaryViewModel Current, ProgressSummaryViewModel Previous,
    decimal PercentagePointChange);
