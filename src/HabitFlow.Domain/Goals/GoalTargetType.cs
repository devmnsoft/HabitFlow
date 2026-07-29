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
        GoalTargetType.HabitCompletions => "Conclusões",
        GoalTargetType.ActiveDays => "Dias ativos",
        GoalTargetType.StreakDays => "Sequência",
        GoalTargetType.WeeklyCompletions => "Conclusões na semana",
        GoalTargetType.Custom => "Acompanhamento manual",
        _ => throw new ArgumentOutOfRangeException(nameof(targetType))
    };
}
