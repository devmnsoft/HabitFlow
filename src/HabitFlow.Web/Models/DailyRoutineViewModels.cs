using HabitFlow.Application;

namespace HabitFlow.Web.Models;

public sealed record DailyRoutineViewModel(
    DateOnly LocalDate,
    int Scheduled,
    int Completed,
    int Percentage,
    IReadOnlyList<RoutinePeriodGroupViewModel> Groups);

public sealed record RoutinePeriodGroupViewModel(
    string Key,
    string Title,
    string Description,
    string Icon,
    IReadOnlyList<RoutineHabitCardViewModel> Habits);

public sealed record RoutineHabitCardViewModel(
    Guid HabitId,
    string Name,
    string Color,
    string Category,
    string IconCode,
    Guid? ObjectiveId,
    TimeOnly? PreferredTime,
    int EstimatedMinutes,
    DailyRoutineItemStatus Status,
    int SortOrder,
    int Version,
    string StatusLabel)
{
    public bool CanComplete => Status is DailyRoutineItemStatus.Available or DailyRoutineItemStatus.Upcoming;
}

public static class DailyRoutineViewModelMapper
{
    private static readonly (string Key, string Title, string Description, string Icon)[] Sections =
    [
        ("morning", "Manhã", "Comece com passos leves e intencionais.", "sunrise"),
        ("afternoon", "Tarde", "Retome o foco sem cobrar perfeição.", "sun"),
        ("evening", "Noite", "Feche o dia cuidando do que importa.", "moon"),
        ("flexible", "Quando puder", "Hábitos flexíveis para encaixar no seu ritmo.", "clock"),
        ("completed", "Concluídos", "Cada passo conta. Muito bem.", "check-circle"),
        ("paused", "Pausados hoje", "Uma pausa consciente também protege sua rotina.", "pause-circle"),
        ("moved", "Movidos", "Estes passos já estão organizados para outro momento.", "calendar")
    ];

    public static DailyRoutineViewModel From(DailyRoutinePlan plan)
    {
        var cards = plan.Items.Select(ToCard).ToList();
        var groups = Sections.Select(section => new RoutinePeriodGroupViewModel(
                section.Key, section.Title, section.Description, section.Icon,
                cards.Where(card => GroupKey(card) == section.Key).OrderBy(card => card.SortOrder).ThenBy(card => card.PreferredTime).ToList()))
            .Where(group => group.Habits.Count > 0)
            .ToList();
        return new(plan.LocalDate, plan.Scheduled, plan.Completed, plan.Percentage, groups);
    }

    private static RoutineHabitCardViewModel ToCard(DailyRoutineItem item) => new(
        item.HabitId, item.Name, item.Color, item.Category ?? "Sem categoria", item.IconCode ?? "check",
        item.ObjectiveId, item.PreferredTime, item.EstimatedMinutes, item.Status, item.SortOrder, item.Version,
        item.Status switch
        {
            DailyRoutineItemStatus.Completed => "Concluído",
            DailyRoutineItemStatus.Excused => "Pausado hoje",
            DailyRoutineItemStatus.Moved => "Movido para amanhã",
            DailyRoutineItemStatus.Upcoming => "Mais tarde",
            DailyRoutineItemStatus.Missed => "Pendente",
            _ => "Disponível"
        });

    private static string GroupKey(RoutineHabitCardViewModel item)
    {
        if (item.Status == DailyRoutineItemStatus.Completed) return "completed";
        if (item.Status == DailyRoutineItemStatus.Excused) return "paused";
        if (item.Status == DailyRoutineItemStatus.Moved) return "moved";
        if (!item.PreferredTime.HasValue) return "flexible";
        return item.PreferredTime.Value.Hour switch { < 12 => "morning", < 18 => "afternoon", _ => "evening" };
    }
}
