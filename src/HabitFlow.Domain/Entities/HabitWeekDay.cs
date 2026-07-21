namespace HabitFlow.Domain;

public sealed record HabitWeekDay(Guid Id, Guid HabitId, int DayOfWeek, DateTime CreatedAt)
{
    public bool IsValid() => DayOfWeek is >= 0 and <= 6;
}
