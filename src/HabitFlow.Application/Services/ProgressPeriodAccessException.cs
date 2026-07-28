namespace HabitFlow.Application;

public sealed class ProgressPeriodAccessException(DateOnly availableFrom) : Exception("The requested progress period is outside the effective plan access window.")
{
    public DateOnly AvailableFrom { get; } = availableFrom;
    public const string Code = "progress_history_outside_access_window";
}
