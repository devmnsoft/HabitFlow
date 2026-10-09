namespace HabitFlow.Domain;

public static class CustomerHealthV624Status
{
    public const string Healthy = "Healthy";
    public const string Attention = "Attention";
    public const string AtRisk = "AtRisk";
    public const string Critical = "Critical";
    public const string Inactive = "Inactive";

    public static readonly string[] All = [Healthy, Attention, AtRisk, Critical, Inactive];
}

public sealed record CustomerHealthSignal(
    Guid ClientId,
    string TenantName,
    string Plan,
    string SubscriptionStatus,
    int ActiveUsers7Days,
    int ActiveUsers30Days,
    int HabitsCreated,
    int HabitCompletions30Days,
    int AverageStreak,
    int TemplatesUsed,
    int JourneysUsed,
    int AiRequests30Days,
    bool WeeklyReviewCompleted,
    int PendingInvites,
    bool HasPaymentFailure,
    int? TrialDaysRemaining,
    int OpenTickets,
    bool MetricsAvailable = true);

public sealed record CustomerHealthDecision(
    Guid ClientId,
    int Score,
    string Status,
    bool ChurnRisk,
    IReadOnlyList<string> Signals,
    IReadOnlyList<string> UnavailableMetrics,
    IReadOnlyList<string> RecommendedActions);

public sealed record ActivationFunnelEvent(
    Guid Id,
    Guid ClientId,
    Guid? UserId,
    string Scope,
    string StepCode,
    string Status,
    string CorrelationId,
    DateTime OccurredAt);

public sealed record RetentionRiskEvent(
    Guid Id,
    Guid ClientId,
    Guid? UserId,
    string RiskCode,
    string Severity,
    string Recommendation,
    string Status,
    string CorrelationId,
    DateTime DetectedAt);

public sealed record OperationNote(
    Guid Id,
    Guid ClientId,
    Guid AuthorUserId,
    string NoteType,
    string Content,
    string CorrelationId,
    DateTime CreatedAt);

public sealed record AiOperationInsight(
    Guid Id,
    Guid ClientId,
    Guid? UserId,
    string InsightType,
    string Provider,
    string Model,
    string SanitizedSummary,
    bool RequiresHumanReview,
    string CorrelationId,
    DateTime CreatedAt);

public interface ICustomerOperationsRepository
{
    Task UpsertHealthScoreAsync(CustomerHealthDecision decision, string metricsJson, CancellationToken ct = default);
    Task RecordHealthEventAsync(Guid clientId, string status, int score, string correlationId, CancellationToken ct = default);
    Task<bool> RecordActivationStepAsync(ActivationFunnelEvent item, CancellationToken ct = default);
    Task RecordRetentionRiskAsync(RetentionRiskEvent item, CancellationToken ct = default);
    Task CreateOperationNoteAsync(OperationNote note, CancellationToken ct = default);
    Task RecordAiInsightAsync(AiOperationInsight insight, CancellationToken ct = default);
}

public static class ActivationFunnelPolicy
{
    public static readonly string[] UserSteps =
    [
        "user.registered", "user.email_confirmed", "user.login", "onboarding.started",
        "objective.selected", "habit.created", "checkin.completed", "weekly_review.completed",
        "ai.recommendation.viewed", "template.used", "trial.started"
    ];

    public static readonly string[] TenantSteps =
    [
        "tenant.created", "plan.defined", "owner.created", "users.invited",
        "first_user.active", "journey.used", "report.opened", "billing.configured", "support.contacted"
    ];

    public static bool IsKnown(string scope, string stepCode)
    {
        var list = scope.Equals("Tenant", StringComparison.OrdinalIgnoreCase) ? TenantSteps : UserSteps;
        return list.Contains(stepCode, StringComparer.OrdinalIgnoreCase);
    }
}

public static class CustomerHealthV624Policy
{
    public static CustomerHealthDecision Evaluate(CustomerHealthSignal signal)
    {
        var unavailable = new List<string>();
        var factors = new List<string>();
        var actions = new List<string>();
        var score = 0;

        if (!signal.MetricsAvailable)
            unavailable.Add("Algumas métricas operacionais ainda não estão disponíveis.");

        Add(signal.ActiveUsers7Days > 0, 18, "Usuários ativos nos últimos 7 dias", "Acionar reativação dos usuários-chave.");
        Add(signal.ActiveUsers30Days > 0, 12, "Usuários ativos nos últimos 30 dias", "Revisar onboarding do tenant.");
        Add(signal.HabitsCreated > 0, 12, "Hábitos criados", "Sugerir criação do primeiro hábito.");
        Add(signal.HabitCompletions30Days > 0, 14, "Conclusões nos últimos 30 dias", "Sugerir rotina de retomada.");
        Add(signal.AverageStreak >= 3, 8, "Streak médio saudável", "Ajustar frequência dos hábitos com baixa adesão.");
        Add(signal.TemplatesUsed > 0, 8, "Templates utilizados", "Recomendar templates por objetivo.");
        Add(signal.JourneysUsed > 0, 8, "Jornadas utilizadas", "Convidar admin para aderir a uma jornada guiada.");
        Add(signal.WeeklyReviewCompleted, 8, "Revisão semanal feita", "Solicitar primeira revisão semanal.");
        Add(signal.OpenTickets == 0, 6, "Sem tickets abertos", "Priorizar resposta de suporte.");
        Add(!signal.HasPaymentFailure, 6, "Pagamento sem falha", "Acionar regularização financeira.");

        if (signal.PendingInvites > 0)
        {
            factors.Add("Convites pendentes");
            actions.Add("Relembrar administradores sobre convites pendentes.");
        }

        if (signal.TrialDaysRemaining is >= 0 and <= 3)
        {
            score -= 10;
            factors.Add("Trial próximo do fim");
            actions.Add("Conduzir conversa de upgrade antes do fim do trial.");
        }

        if (signal.HasPaymentFailure)
        {
            score -= 25;
            factors.Add("Falha ou pendência de pagamento");
        }

        if (signal.AiRequests30Days == 0 && !signal.Plan.Equals(PlanCodes.Free, StringComparison.OrdinalIgnoreCase))
        {
            factors.Add("Uso de IA zerado em plano pago");
            actions.Add("Apresentar casos de uso de IA operacional e de rotina.");
        }

        score = Math.Clamp(score, 0, 100);
        var inactive = signal.ActiveUsers30Days == 0 && signal.HabitCompletions30Days == 0;
        var critical = signal.HasPaymentFailure && (score < 45 || signal.OpenTickets > 0);
        var status = inactive ? CustomerHealthV624Status.Inactive :
            critical ? CustomerHealthV624Status.Critical :
            score >= 75 ? CustomerHealthV624Status.Healthy :
            score >= 50 ? CustomerHealthV624Status.Attention :
            CustomerHealthV624Status.AtRisk;

        return new(signal.ClientId, score, status, !status.Equals(CustomerHealthV624Status.Healthy, StringComparison.Ordinal),
            factors.Distinct().ToArray(), unavailable, actions.Distinct().ToArray());

        void Add(bool ok, int points, string positive, string action)
        {
            if (ok)
            {
                score += points;
                return;
            }

            factors.Add("Ausente: " + positive);
            actions.Add(action);
        }
    }
}

public static class RetentionRiskPolicy
{
    public static IReadOnlyList<RetentionRiskEvent> Detect(CustomerHealthSignal signal, Guid? userId, string correlationId)
    {
        var now = DateTime.UtcNow;
        var risks = new List<RetentionRiskEvent>();
        void Add(string code, string severity, string recommendation)
            => risks.Add(new(Guid.NewGuid(), signal.ClientId, userId, code, severity, recommendation, "Open", correlationId, now));

        if (signal.ActiveUsers7Days == 0) Add("user.no_login_7d", "High", "Sugerir campanha de reativação e revisão de onboarding.");
        if (signal.HabitsCreated == 0) Add("user.no_active_habit", "High", "Orientar criação do primeiro hábito a partir de template.");
        if (signal.ActiveUsers30Days == 0) Add("tenant.no_active_users", "Critical", "Acionar contato comercial ou CS do tenant.");
        if (signal.TrialDaysRemaining is >= 0 and <= 3 && signal.HabitCompletions30Days == 0) Add("trial.no_activation", "Critical", "Priorizar ativação antes do encerramento do trial.");
        if (signal.HasPaymentFailure) Add("billing.payment_pending", "Critical", "Abrir acompanhamento financeiro sem bloquear dados existentes.");
        if (signal.OpenTickets > 0) Add("support.open_ticket", "Medium", "Revisar SLA e pendências de resposta.");
        if (signal.TemplatesUsed == 0) Add("templates.not_used", "Medium", "Recomendar templates oficiais alinhados ao objetivo do tenant.");
        if (signal.AiRequests30Days == 0 && !signal.Plan.Equals(PlanCodes.Free, StringComparison.OrdinalIgnoreCase)) Add("ai.zero_usage_paid_plan", "Low", "Mostrar ao admin casos práticos de IA com revisão humana.");
        return risks;
    }
}
