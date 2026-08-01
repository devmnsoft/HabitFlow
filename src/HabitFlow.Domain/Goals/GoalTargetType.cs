namespace HabitFlow.Domain;

public enum GoalTargetType
{
    HabitCompletions,
    ActiveDays,
    StreakDays,
    WeeklyCompletions,
    Custom
}

public static class GoalTargetTypes
{
    public static bool TryParse(string? value, out GoalTargetType targetType) =>
        Enum.TryParse(value, true, out targetType) && Enum.IsDefined(targetType);

    public static string ToPublicText(this GoalTargetType targetType) => targetType switch
    {
        GoalTargetType.HabitCompletions => "Conclusões de hábitos",
        GoalTargetType.ActiveDays => "Dias ativos",
        GoalTargetType.StreakDays => "Dias de sequência",
        GoalTargetType.WeeklyCompletions => "Conclusões na semana",
        GoalTargetType.Custom => "Meta personalizada",
        _ => throw new ArgumentOutOfRangeException(nameof(targetType))
    };
}
