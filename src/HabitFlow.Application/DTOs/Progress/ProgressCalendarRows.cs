namespace HabitFlow.Application;

public sealed class ProgressHabitRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FrequencyTypeCode { get; set; } = string.Empty;
    public TimeOnly? ReminderTime { get; set; }
}
public sealed class ProgressWeekDayRow { public Guid HabitId { get; set; } public int DayOfWeek { get; set; } }
public sealed class ProgressCompletionRow { public Guid HabitId { get; set; } public DateOnly CompletedDate { get; set; } }
public sealed record ProgressData(IReadOnlyList<ProgressHabitRow> Habits, IReadOnlyList<ProgressWeekDayRow> WeekDays,
    IReadOnlyList<ProgressCompletionRow> Completions);
