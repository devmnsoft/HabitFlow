using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class TenantHealthEvaluationService(
    IProductActivationRepository activationRepo,
    ILogger<TenantHealthEvaluationService> logger)
{
    public TenantCustomerSuccessIndicator Evaluate(TenantActivationRow row, int? trialRemainingDays = null, int completions30Days = 0, int reportsCount = 0)
    {
        var riskFactors = new List<string>();
        var commercialPendencies = new List<string>();

        var score = 0;
        void AddPoints(bool condition, int points) { if (condition) score += points; }

        // Ativação e onboarding
        AddPoints(row.CompanyDone && row.PlanReviewed && row.HasAdmin, 20);
        // Uso recente
        AddPoints(row.Active7Days, 20);
        // Usuários ativos
        AddPoints(row.ActiveUsers > 0, 15);
        // Hábitos e conclusões
        AddPoints(row.CompletionsKnown ? row.Completions7Days > 0 : row.FirstHabit, 15);
        // Pagamento em dia
        var hasPaymentIssue = IsPaymentIssue(row);
        if (!hasPaymentIssue) AddPoints(true, 15);
        else
        {
            score -= 30;
            riskFactors.Add("Inadimplência ou pagamento pendente");
            commercialPendencies.Add("Fatura pendente ou vencida");
        }

        // Sem chamados abertos
        AddPoints(row.TicketsKnown ? row.OpenTickets == 0 : true, 15);

        // Penalidades
        var isBlocked = row.Status.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
                     || row.BenefitsStatus.Contains("Blocked", StringComparison.OrdinalIgnoreCase);

        if (isBlocked)
        {
            score -= 30;
            riskFactors.Add("Benefícios ou conta com bloqueio operacional");
            commercialPendencies.Add("Benefícios bloqueados");
        }

        if (!row.Active15Days)
        {
            score -= 20;
            riskFactors.Add("Sem atividade nos últimos 15 dias");
        }

        var isFree = row.Plan.Equals("Free", StringComparison.OrdinalIgnoreCase);
        var limitBlocked = isFree && row.Habits >= AppConstants.FreePlanHabitLimit;
        if (limitBlocked)
        {
            score -= 10;
            riskFactors.Add("Limite de hábitos do plano atingido");
            commercialPendencies.Add("Upgrade recomendado por teto de uso");
        }

        score = Math.Clamp(score, 0, 100);

        // Determinação canônica do HealthCategory
        string category;
        var isTrialEnding = (row.SubscriptionStatus.Equals("Trial", StringComparison.OrdinalIgnoreCase) || row.Plan.Equals("Trial", StringComparison.OrdinalIgnoreCase))
                         && trialRemainingDays is not null && trialRemainingDays.Value <= 3;

        if (isBlocked)
        {
            category = TenantHealthStatus.Blocked;
        }
        else if (hasPaymentIssue)
        {
            category = TenantHealthStatus.PaymentIssue;
        }
        else if (isTrialEnding)
        {
            category = TenantHealthStatus.TrialEnding;
            riskFactors.Add($"Trial expirando em {trialRemainingDays} dia(s)");
            commercialPendencies.Add("Trial próximo do encerramento");
        }
        else if (score < 45 || !row.Active15Days)
        {
            category = TenantHealthStatus.AtRisk;
            riskFactors.Add("Risco iminente de desengajamento/churn");
        }
        else if (score < 75)
        {
            category = TenantHealthStatus.Attention;
        }
        else
        {
            category = TenantHealthStatus.Healthy;
        }

        var churnRisk = category is TenantHealthStatus.AtRisk or TenantHealthStatus.Blocked or TenantHealthStatus.PaymentIssue;

        // Onboarding step e percent
        var onboardingDoneCount = (row.CompanyDone ? 1 : 0) + (row.PlanReviewed ? 1 : 0) +
                                  (row.HasAdmin ? 1 : 0) + (row.UsersInvited ? 1 : 0) +
                                  (row.FirstHabit ? 1 : 0) + (row.BillingDone ? 1 : 0) +
                                  (row.NotificationsConfigured ? 1 : 0);
        var onboardingPercent = (int)Math.Round(onboardingDoneCount * 100.0 / 7.0);
        var currentStep = !row.CompanyDone ? "Dados da empresa" :
                          !row.FirstHabit ? "Primeiro hábito" :
                          !row.UsersInvited ? "Convite de equipe" :
                          !row.BillingDone ? "Revisão de cobrança" : "Implantação concluída";

        return new TenantCustomerSuccessIndicator(
            row.ClientId,
            row.Name,
            row.Status,
            row.Plan,
            trialRemainingDays,
            row.SubscriptionStatus,
            row.PaymentStatus,
            row.ActiveUsers,
            row.InactiveUsers,
            row.Habits,
            row.Completions7Days,
            completions30Days,
            row.AiRequests7Days,
            reportsCount,
            row.OpenTickets,
            row.Active7Days ? DateTime.UtcNow.AddDays(-2) : row.Active15Days ? DateTime.UtcNow.AddDays(-10) : DateTime.UtcNow.AddDays(-20),
            churnRisk,
            currentStep,
            onboardingPercent,
            limitBlocked,
            commercialPendencies,
            score,
            category,
            riskFactors
        );
    }

    public async Task<IReadOnlyList<TenantCustomerSuccessIndicator>> EvaluateAllAsync(HomologationQuery query, CancellationToken ct = default)
    {
        var rows = await activationRepo.ListTenantsAsync(query, ct);
        var list = new List<TenantCustomerSuccessIndicator>(rows.Count);

        foreach (var row in rows)
        {
            // Se plano for trial de 15 dias, calcula estimativa de dias restantes
            int? trialRemaining = null;
            if (row.SubscriptionStatus.Equals("Trial", StringComparison.OrdinalIgnoreCase))
            {
                trialRemaining = 3; // Estimativa padrão se não houver coluna direta de expiração na leitura atual
            }

            var evaluated = Evaluate(row, trialRemaining, row.Completions7Days * 4, 1);
            list.Add(evaluated);

            // Emite auditoria para eventos de risco
            if (evaluated.HealthCategory == TenantHealthStatus.AtRisk)
            {
                logger.LogWarning(ApplicationEvents.CustomerSuccessRiskDetected,
                    "customer_success.risk_detected ClientId={ClientId} Score={Score} Category={Category}",
                    row.ClientId, evaluated.HealthScore, evaluated.HealthCategory);
            }
            else if (evaluated.HealthCategory == TenantHealthStatus.PaymentIssue)
            {
                logger.LogWarning(ApplicationEvents.BillingPaymentIssueDetected,
                    "billing.payment.issue_detected ClientId={ClientId} PaymentStatus={PaymentStatus}",
                    row.ClientId, row.PaymentStatus);
            }
            else if (evaluated.HealthCategory == TenantHealthStatus.TrialEnding)
            {
                logger.LogInformation(ApplicationEvents.BillingTrialEnding,
                    "billing.trial.ending ClientId={ClientId} DaysRemaining={DaysRemaining}",
                    row.ClientId, trialRemaining);
            }

            logger.LogInformation(ApplicationEvents.TenantHealthUpdated,
                "tenant.health.updated ClientId={ClientId} Score={Score} Category={Category}",
                row.ClientId, evaluated.HealthScore, evaluated.HealthCategory);
        }

        return list;
    }

    private static bool IsPaymentIssue(TenantActivationRow row) =>
        row.PaymentStatus.Equals("Overdue", StringComparison.OrdinalIgnoreCase) ||
        row.PaymentStatus.Equals("Pending", StringComparison.OrdinalIgnoreCase) ||
        row.SubscriptionStatus.Equals("PastDue", StringComparison.OrdinalIgnoreCase) ||
        row.SubscriptionStatus.Equals("PaymentPending", StringComparison.OrdinalIgnoreCase);
}
