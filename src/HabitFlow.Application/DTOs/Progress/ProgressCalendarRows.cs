namespace HabitFlow.Application;

public sealed class ProgressHabitRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string FrequencyTypeCode { get; set; } = string.Empty;
    public TimeOnly? ReminderTime { get; set; }
    public bool IsPaused { get; set; }
    public DateTime? PausedAt { get; set; }
    public decimal? TargetQuantity { get; set; }
    public string? TargetUnit { get; set; }
    public string? MinimumVersionName { get; set; }
    public decimal? MinimumVersionQuantity { get; set; }
    public int RetroactiveAdjustmentDays { get; set; } = 7;
}
public sealed class ProgressWeekDayRow { public Guid HabitId { get; set; } public int DayOfWeek { get; set; } }
public sealed class ProgressCompletionRow { public Guid HabitId { get; set; } public DateOnly CompletedDate { get; set; } }
public sealed record ProgressData(IReadOnlyList<ProgressHabitRow> Habits, IReadOnlyList<ProgressWeekDayRow> WeekDays,
    IReadOnlyList<ProgressCompletionRow> Completions);
