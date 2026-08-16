namespace HabitFlow.Application;

public sealed record RoutineRecommendation(
    string Code,
    string Title,
    string Message,
    string Severity,
    string ActionLabel,
    string? ActionUrl,
    Guid? HabitId,
    Guid? GoalId,
    int Priority);

public sealed record RoutineRecommendationContext(
    IReadOnlyList<WeeklyReviewHabitResult> Habits,
    IReadOnlyList<WeeklyReviewGoalResult> Goals,
    int OverallConsistency,
    int ActiveHabitCount,
    int? ActiveHabitLimit = null);

/// <summary>Small, deterministic rules shared by product surfaces. No profile data leaves the application.</summary>
public sealed class RoutineRecommendationService
{
    public IReadOnlyList<RoutineRecommendation> Build(RoutineRecommendationContext context, int maximum = 3)
    {
        var result = new List<RoutineRecommendation>();
        var heavy = context.Habits.Where(x => x.Percentage < 50 && x.EstimatedTimeMinutes >= 20)
            .OrderBy(x => x.Percentage).ThenByDescending(x => x.EstimatedTimeMinutes).FirstOrDefault();
        if (heavy is not null) result.Add(new("habit_too_heavy", "Deixe este hábito mais leve",
            $"{heavy.Name} pode caber melhor na semana com menos dias ou menos minutos.", "attention",
            "Ajustar rotina", $"/habits/{heavy.HabitId}#adaptive-routine", heavy.HabitId, null, 10));

        var forgotten = context.Habits.Where(x => x.Percentage < 50 && !x.HasReminder)
            .OrderBy(x => x.Percentage).FirstOrDefault();
        if (forgotten is not null) result.Add(new("habit_low_consistency_no_reminder", "Crie um ponto de apoio",
            $"Um lembrete pode ajudar a reencontrar {forgotten.Name} no momento certo.", "suggestion",
            "Criar lembrete", $"/habits/{forgotten.HabitId}/reminders", forgotten.HabitId, null, 20));

        var unlinked = context.Goals.FirstOrDefault(x => !x.HasLinkedHabit && x.Status == "Active");
        if (unlinked is not null) result.Add(new("goal_without_habit", "Transforme intenção em rotina",
            $"Vincule um hábito pequeno ao objetivo {unlinked.Title}.", "suggestion", "Vincular hábito",
            $"/goals/{unlinked.GoalId}", null, unlinked.GoalId, 30));

        if (context.ActiveHabitLimit is > 0 && context.ActiveHabitCount >= Math.Ceiling(context.ActiveHabitLimit.Value * .8m))
            result.Add(new("plan_near_limit", "Organize seu espaço", "Seu plano está perto do limite de hábitos ativos. Ajustar um hábito não consome nova cota.",
                "info", "Ver uso do plano", "/account/plan/usage", null, null, 40));

        if (context.ActiveHabitCount == 0) result.Add(new("first_habit_needed", "Comece com um passo possível",
            "Escolha um hábito curto na biblioteca e adapte ao seu dia.", "suggestion", "Abrir biblioteca", "/habit-library", null, null, 1));
        else if (context.OverallConsistency >= 70) result.Add(new("keep_current_rhythm", "Seu ritmo está funcionando",
            "Mantenha a frequência atual por mais uma semana antes de aumentar o desafio.", "positive", "Ver meus hábitos", "/habits", null, null, 90));
        else if (context.OverallConsistency < 30) result.Add(new("simplify_routine", "Escolha um foco principal",
            "Retome pelo menor hábito, sem tentar compensar tudo de uma vez.", "attention", "Abrir Meu Dia", "/my-day", null, null, 15));

        return result.GroupBy(x => x.Code).Select(x => x.First()).OrderBy(x => x.Priority).Take(Math.Clamp(maximum, 1, 3)).ToList();
    }
}
