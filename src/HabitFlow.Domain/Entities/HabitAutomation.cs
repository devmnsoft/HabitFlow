namespace HabitFlow.Domain;

public enum HabitAutomationType
{
    SmartReminder,
    WeeklyReview,
    ConsistencyAlert,
    RecoveryPlan,
    RescheduleSuggestion,
    MotivationalBoost,
    MonthlySummary
}

public enum HabitAutomationStatus
{
    Active,
    Paused,
    Completed,
    Canceled,
    Error,
    AwaitingConfirmation
}

public sealed record HabitAutomation(
    Guid Id, Guid ClientId, Guid UserId, Guid? HabitId, HabitAutomationType Type,
    string Frequency, HabitAutomationStatus Status, TimeOnly QuietHoursStart,
    TimeOnly QuietHoursEnd, string? SettingsJson, DateTime? LastTriggeredAt,
    DateTime CreatedAt, DateTime UpdatedAt);
