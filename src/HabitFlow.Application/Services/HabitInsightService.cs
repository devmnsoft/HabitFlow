namespace HabitFlow.Application;

public enum HabitInsightType { ConsistencyImproved, ConsistencyDropped, StreakMaintained, StreakBroken, HabitAtRisk, BestHabit, OverloadedRoutine, ReminderRecommended, ChallengeRecommended, UpgradeRecommended }
public enum HabitInsightSeverity { Positive, Information, Attention }

public sealed record HabitInsight(HabitInsightType Type, string Title, string Description, string CalculatedReason,
    string SuggestedAction, string ActionRoute, HabitInsightSeverity Severity, string Category,
    DateTimeOffset GeneratedAt, Guid ClientId, Guid UserId);

/// <summary>Deterministic, explainable rules. Insights are projections and are not persisted.</summary>
public sealed class HabitInsightService(TimeProvider clock)
{
    public IReadOnlyList<HabitInsight> Build(Guid clientId, Guid userId, WeeklyReviewResult review, int? previousPercentage = null)
    {
        if (clientId == Guid.Empty || userId == Guid.Empty) throw new ArgumentException("Conta e pessoa são obrigatórias.");
        var result = new List<HabitInsight>();
        var now = clock.GetUtcNow();
        HabitInsight Create(HabitInsightType type, string title, string description, string reason, string action,
            string route, HabitInsightSeverity severity, string category) =>
            new(type, title, description, reason, action, SafeRoute(route), severity, category, now, clientId, userId);

        var best = review.Habits.OrderByDescending(x => x.Percentage).ThenByDescending(x => x.Completed).FirstOrDefault();
        if (best is not null)
            result.Add(Create(HabitInsightType.BestHabit, "Seu melhor ritmo", $"{best.Name} foi seu hábito mais consistente.",
                $"{best.Completed} de {best.Scheduled} ocorrências concluídas ({best.Percentage}%).", "Manter o ritmo atual", $"/habits/{best.HabitId}", HabitInsightSeverity.Positive, best.Category));

        var risk = review.Habits.Where(x => x.Scheduled >= 3).OrderBy(x => x.Percentage).FirstOrDefault(x => x.Percentage < 50);
        if (risk is not null)
            result.Add(Create(HabitInsightType.HabitAtRisk, "Um ritmo pode ficar mais leve", $"{risk.Name} ficou abaixo do esperado. Isso é informação, não falha.",
                $"{risk.Completed} de {risk.Scheduled} ocorrências concluídas ({risk.Percentage}%).", "Rever frequência", $"/habits/{risk.HabitId}/edit", HabitInsightSeverity.Attention, risk.Category));

        if (best?.CurrentStreak >= 3)
            result.Add(Create(HabitInsightType.StreakMaintained, "Sequência mantida", $"Você preservou uma sequência de {best.CurrentStreak} em {best.Name}.",
                "Três ou mais ocorrências planejadas consecutivas foram concluídas.", "Continuar amanhã", "/my-day", HabitInsightSeverity.Positive, best.Category));

        if (previousPercentage.HasValue && Math.Abs(review.Percentage - previousPercentage.Value) >= 5)
        {
            var improved = review.Percentage > previousPercentage;
            var delta = Math.Abs(review.Percentage - previousPercentage.Value);
            result.Add(Create(improved ? HabitInsightType.ConsistencyImproved : HabitInsightType.ConsistencyDropped,
                improved ? "Consistência em evolução" : "Seu ritmo mudou", improved ? "Seu ritmo cresceu em relação ao período anterior." : "Talvez sua rotina precise de um pouco mais de espaço.",
                $"Variação calculada de {(improved ? "+" : "-")}{delta} pontos percentuais.", improved ? "Repetir o que funcionou" : "Simplificar a semana", "/weekly-review", improved ? HabitInsightSeverity.Positive : HabitInsightSeverity.Attention, "Consistência"));
        }
        if (review.Scheduled >= 28 && review.Percentage < 55)
            result.Add(Create(HabitInsightType.OverloadedRoutine, "Agenda cheia", "Menos compromissos podem tornar a semana mais repetível.",
                $"Foram {review.Scheduled} ocorrências planejadas com {review.Percentage}% de consistência.", "Revisar agenda", "/habits", HabitInsightSeverity.Attention, "Rotina"));
        return result;
    }

    private static string SafeRoute(string route) => route.StartsWith('/') && !route.StartsWith("//", StringComparison.Ordinal) ? route : "/my-day";
}
