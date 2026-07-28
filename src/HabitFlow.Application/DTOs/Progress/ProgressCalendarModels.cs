namespace HabitFlow.Application;

public enum ProgressDayStatus { NoSchedule, Future, NotStarted, Partial, Completed }

public sealed record ProgressCalendarDayViewModel(DateOnly Date, int DayNumber, bool IsCurrentMonth, bool IsToday,
    bool IsFuture, bool HasSchedule, int ScheduledCount, int CompletedCount, int PendingCount,
    decimal CompletionPercentage, ProgressDayStatus Status, string AccessibilityLabel, string DetailUrl);

public sealed record ProgressSummaryViewModel(int ScheduledCount, int CompletedCount, int PendingCount,
    decimal CompletionPercentage, int ActiveDays, int CompletedDays, int PartialDays, int CurrentStreak, int BestStreak);

public sealed class ProgressCalendarViewModel
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = string.Empty;
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public int PreviousYear { get; init; }
    public int PreviousMonth { get; init; }
    public int NextYear { get; init; }
    public int NextMonth { get; init; }
    public bool CanGoPrevious { get; init; }
    public bool CanGoNext { get; init; }
    public DateOnly Today { get; init; }
    public int CurrentStreak { get; init; }
    public int BestStreak { get; init; }
    public int ScheduledCount { get; init; }
    public int CompletedCount { get; init; }
    public int PendingCount { get; init; }
    public decimal CompletionPercentage { get; init; }
    public int ActiveDays { get; init; }
    public int CompletedDays { get; init; }
    public int PartialDays { get; init; }
    public IReadOnlyList<ProgressCalendarDayViewModel> Days { get; init; } = [];
    public string PlanCode { get; init; } = string.Empty;
    public bool HasFullHistory { get; init; }
    public DateOnly? HistoryLimitStart { get; init; }
    public DateOnly ConsistencyPeriodStart { get; init; }
    public DateOnly ConsistencyPeriodEnd { get; init; }
    public bool IsBestStreakLimitedByPlan { get; init; }
    public bool IsCurrentMonth { get; init; }
    public string InsightMessage { get; init; } = string.Empty;
}

public sealed record ProgressHabitStatusViewModel(Guid HabitId, string Name, string? Category, TimeOnly? SuggestedTime, bool Completed);
public sealed record ProgressDayDetailViewModel(DateOnly Date, int ScheduledCount, int CompletedCount, int PendingCount,
    decimal CompletionPercentage, ProgressDayStatus Status, IReadOnlyList<ProgressHabitStatusViewModel> Habits);
public sealed record ProgressComparisonViewModel(ProgressSummaryViewModel Current, ProgressSummaryViewModel Previous,
    decimal PercentagePointChange);
