using HabitFlow.Application;

namespace HabitFlow.Web.Models;

public sealed record DailyRoutineViewModel(
    DateOnly LocalDate,
    int Scheduled,
    int Completed,
    int Percentage,
    IReadOnlyList<RoutinePeriodGroupViewModel> Groups,
    string Greeting,
    string Motivation,
    string? FocusHabitName,
    IReadOnlyList<RoutineHabitCardViewModel> NextActions,
    int Overdue,
    int CurrentStreak,
    IReadOnlyList<DailyPulseViewModel> WeeklyPulse)
{
    public int Pending => Math.Max(0, Scheduled - Completed);
    public bool PlanLimitReached { get; init; }
}

public sealed record DailyPulseViewModel(string Label, int Percentage, bool IsToday);

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
    string FrequencyLabel,
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
        ("now", "Agora", "Comece pelo próximo passo possível.", "play"),
        ("next", "Próximos", "O que pode esperar fica organizado aqui.", "clock"),
        ("completed", "Concluídos", "Cada passo conta. Muito bem.", "check-circle"),
        ("paused", "Pausados hoje", "Uma pausa consciente também protege sua rotina.", "pause-circle"),
        ("moved", "Não programados hoje", "Estes passos já estão organizados para outro momento.", "calendar")
    ];

    public static DailyRoutineViewModel From(DailyRoutinePlan plan)
    {
        var cards = plan.Items.Select(ToCard).ToList();
        var groups = Sections.Select(section => new RoutinePeriodGroupViewModel(
                section.Key, section.Title, section.Description, section.Icon,
                cards.Where(card => GroupKey(card) == section.Key)
                    .OrderBy(card => card.PreferredTime.HasValue ? 0 : 1).ThenBy(card => card.PreferredTime)
                    .ThenBy(card => card.ObjectiveId.HasValue ? 0 : 1).ThenBy(card => card.EstimatedMinutes)
                    .ThenBy(card => card.SortOrder).ThenBy(card => card.Name).ToList()))
            .Where(group => group.Habits.Count > 0)
            .ToList();
        var focus = cards.FirstOrDefault(x => x.CanComplete && x.ObjectiveId.HasValue)?.Name;
        var motivation = plan.Scheduled switch
        {
            0 => "Seu dia está livre. Escolha o próximo passo com calma.",
            _ when plan.Completed == plan.Scheduled => "Você cuidou do que planejou. Consistência também é saber encerrar.",
            _ when plan.Percentage >= 50 => "Você já concluiu metade do dia. Continue no seu ritmo.",
            _ when plan.Pending <= 2 => $"Hoje está leve: {plan.Pending} passos importantes.",
            _ => "Seu foco hoje é manter consistência, não perfeição."
        };
        var nextActions = cards.Where(x => x.CanComplete).Take(3).ToList();
        var overdue = cards.Count(x => x.Status == DailyRoutineItemStatus.Missed);
        var pulse = Enumerable.Range(-6, 7)
            .Select(offset => new DailyPulseViewModel(
                plan.LocalDate.AddDays(offset).ToString("ddd").TrimEnd('.'),
                offset == 0 ? plan.Percentage : 0,
                offset == 0))
            .ToList();
        return new(plan.LocalDate, plan.Scheduled, plan.Completed, plan.Percentage, groups, "Olá", motivation, focus,
            nextActions, overdue, plan.Completed > 0 ? 1 : 0, pulse);
    }

    private static RoutineHabitCardViewModel ToCard(DailyRoutineItem item) => new(
        item.HabitId, item.Name, item.Color, item.Category ?? "Sem categoria", item.IconCode ?? "check",
        item.ObjectiveId, item.PreferredTime, item.EstimatedMinutes, Frequency(item.Frequency), item.Status, item.SortOrder, item.Version,
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
        return item.Status == DailyRoutineItemStatus.Upcoming ? "next" : "now";
    }

    private static string Frequency(HabitFlow.Domain.HabitFrequencyType frequency) => frequency switch
    {
        HabitFlow.Domain.HabitFrequencyType.Daily => "Todos os dias",
        HabitFlow.Domain.HabitFrequencyType.Weekdays => "Dias úteis",
        HabitFlow.Domain.HabitFrequencyType.Weekends => "Fins de semana",
        _ => "Dias escolhidos"
    };
}
