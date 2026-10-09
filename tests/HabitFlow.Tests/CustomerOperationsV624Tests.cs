using HabitFlow.Application;
using HabitFlow.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HabitFlow.Tests;

public sealed class CustomerOperationsV624Tests
{
    [Fact]
    public void CustomerHealth_Evaluates_Healthy_WhenActivationAndPaymentsAreStrong()
    {
        var decision = CustomerHealthV624Policy.Evaluate(CreateSignal(
            activeUsers7Days: 8,
            activeUsers30Days: 12,
            habitsCreated: 30,
            habitCompletions30Days: 220,
            averageStreak: 6,
            templatesUsed: 5,
            journeysUsed: 2,
            aiRequests30Days: 20,
            weeklyReviewCompleted: true,
            hasPaymentFailure: false));

        Assert.Equal(CustomerHealthV624Status.Healthy, decision.Status);
        Assert.True(decision.Score >= 75);
        Assert.False(decision.ChurnRisk);
    }

    [Fact]
    public void CustomerHealth_MarksInactive_WhenTenantHasNoUsersOrCompletions()
    {
        var decision = CustomerHealthV624Policy.Evaluate(CreateSignal(
            activeUsers7Days: 0,
            activeUsers30Days: 0,
            habitsCreated: 0,
            habitCompletions30Days: 0,
            averageStreak: 0,
            templatesUsed: 0,
            journeysUsed: 0,
            aiRequests30Days: 0,
            weeklyReviewCompleted: false,
            hasPaymentFailure: false));

        Assert.Equal(CustomerHealthV624Status.Inactive, decision.Status);
        Assert.True(decision.ChurnRisk);
        Assert.Contains(decision.RecommendedActions, x => x.Contains("reativação", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CustomerHealth_MarksCritical_WhenPaymentFailureAndOpenSupportExist()
    {
        var decision = CustomerHealthV624Policy.Evaluate(CreateSignal(
            activeUsers7Days: 2,
            activeUsers30Days: 4,
            habitsCreated: 8,
            habitCompletions30Days: 15,
            averageStreak: 2,
            templatesUsed: 1,
            journeysUsed: 0,
            aiRequests30Days: 0,
            weeklyReviewCompleted: false,
            hasPaymentFailure: true,
            openTickets: 2));

        Assert.Equal(CustomerHealthV624Status.Critical, decision.Status);
        Assert.Contains("Falha ou pendência de pagamento", decision.Signals);
    }

    [Fact]
    public async Task ActivationStep_ValidatesCanonicalStepsAndIsIdempotent()
    {
        var repo = new FakeCustomerOperationsRepository();
        var service = new CustomerOperationsV624Service(repo, NullLogger<CustomerOperationsV624Service>.Instance);
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var first = await service.CompleteActivationStepAsync(clientId, userId, "User", "habit.created", "corr-624");
        var duplicate = await service.CompleteActivationStepAsync(clientId, userId, "User", "habit.created", "corr-624");
        var invalid = await service.CompleteActivationStepAsync(clientId, userId, "User", "unknown.step", "corr-624");

        Assert.True(first.IsSuccess);
        Assert.True(first.Value);
        Assert.True(duplicate.IsSuccess);
        Assert.False(duplicate.Value);
        Assert.True(invalid.IsFailure);
        Assert.Equal("activation.step_invalid", invalid.Error.Code);
    }

    [Fact]
    public void RetentionRisk_DetectsTrialPaymentAndUsageRisks()
    {
        var risks = RetentionRiskPolicy.Detect(CreateSignal(
            activeUsers7Days: 0,
            activeUsers30Days: 0,
            habitsCreated: 0,
            habitCompletions30Days: 0,
            averageStreak: 0,
            templatesUsed: 0,
            journeysUsed: 0,
            aiRequests30Days: 0,
            weeklyReviewCompleted: false,
            hasPaymentFailure: true,
            trialDaysRemaining: 2,
            openTickets: 1), Guid.NewGuid(), "corr-risk");

        Assert.Contains(risks, x => x.RiskCode == "tenant.no_active_users" && x.Severity == "Critical");
        Assert.Contains(risks, x => x.RiskCode == "trial.no_activation");
        Assert.Contains(risks, x => x.RiskCode == "billing.payment_pending");
        Assert.Contains(risks, x => x.RiskCode == "ai.zero_usage_paid_plan");
    }

    [Fact]
    public async Task Service_PersistsHealthRiskNoteAndAiInsightContracts()
    {
        var repo = new FakeCustomerOperationsRepository();
        var service = new CustomerOperationsV624Service(repo, NullLogger<CustomerOperationsV624Service>.Instance);
        var clientId = Guid.NewGuid();
        var signal = CreateSignal(clientId: clientId, hasPaymentFailure: true, openTickets: 1);

        var decision = await service.CalculateHealthAsync(signal, "corr-health");
        var risks = await service.DetectRetentionRisksAsync(signal, null, "corr-risk");
        await service.CreateOperationNoteAsync(clientId, Guid.NewGuid(), "ActionPlan", "Acionar cliente para regularizacao e ativacao.", "corr-note");
        await service.RecordAiInsightAsync(clientId, null, "support_summary", "LocalGuard", "RuleEngine", "Resumo higienizado.", "corr-ai");

        Assert.Equal(decision, repo.LastDecision);
        Assert.Equal("corr-health", repo.LastHealthCorrelationId);
        Assert.NotEmpty(risks);
        Assert.Single(repo.Notes);
        Assert.Single(repo.AiInsights);
        Assert.True(repo.AiInsights[0].RequiresHumanReview);
    }

    [Fact]
    public void OperationalAi_MasksEmailsAndSecrets()
    {
        var masked = OperationalAiService.MaskSensitiveData("Contato joao@example.com token=abc123 senha: muito-secreta");

        Assert.DoesNotContain("joao@example.com", masked);
        Assert.DoesNotContain("abc123", masked);
        Assert.DoesNotContain("muito-secreta", masked);
        Assert.Contains("[email-removido]", masked);
        Assert.Contains("[REMOVIDO]", masked);
    }

    [Fact]
    public void DatabaseScripts_IncludeV624CustomerOperationsContracts()
    {
        var migration = File.ReadAllText(RepositoryRootLocator.PathTo("database", "migrations", "099_v6240_customer_operations_growth.sql"));
        var script = File.ReadAllText(RepositoryRootLocator.PathTo("database", "script_completo.sql"));
        var migrate = File.ReadAllText(RepositoryRootLocator.PathTo("database", "migrate.sql"));

        Assert.Contains("habitflow.customer_health_scores", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("habitflow.activation_funnel_events", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("habitflow.retention_risk_events", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("habitflow.operation_notes", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("habitflow.ai_operation_insights", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ux_activation_funnel_events_step", migration, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("099_v6240_customer_operations_growth.sql", migrate, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-- BEGIN include database/migrations/099_v6240_customer_operations_growth.sql", script, StringComparison.OrdinalIgnoreCase);
    }

    private static CustomerHealthSignal CreateSignal(
        Guid? clientId = null,
        int activeUsers7Days = 2,
        int activeUsers30Days = 4,
        int habitsCreated = 6,
        int habitCompletions30Days = 20,
        int averageStreak = 3,
        int templatesUsed = 1,
        int journeysUsed = 1,
        int aiRequests30Days = 3,
        bool weeklyReviewCompleted = true,
        bool hasPaymentFailure = false,
        int? trialDaysRemaining = null,
        int openTickets = 0) =>
        new(
            clientId ?? Guid.NewGuid(),
            "Cliente Teste",
            PlanCodes.Ritmo,
            "Active",
            activeUsers7Days,
            activeUsers30Days,
            habitsCreated,
            habitCompletions30Days,
            averageStreak,
            templatesUsed,
            journeysUsed,
            aiRequests30Days,
            weeklyReviewCompleted,
            PendingInvites: 0,
            hasPaymentFailure,
            trialDaysRemaining,
            openTickets);

    private sealed class FakeCustomerOperationsRepository : ICustomerOperationsRepository
    {
        private readonly HashSet<string> _activationKeys = new(StringComparer.OrdinalIgnoreCase);

        public CustomerHealthDecision? LastDecision { get; private set; }
        public string? LastHealthCorrelationId { get; private set; }
        public List<OperationNote> Notes { get; } = [];
        public List<AiOperationInsight> AiInsights { get; } = [];

        public Task UpsertHealthScoreAsync(CustomerHealthDecision decision, string metricsJson, CancellationToken ct = default)
        {
            LastDecision = decision;
            Assert.Contains("ActiveUsers7Days", metricsJson);
            return Task.CompletedTask;
        }

        public Task RecordHealthEventAsync(Guid clientId, string status, int score, string correlationId, CancellationToken ct = default)
        {
            LastHealthCorrelationId = correlationId;
            return Task.CompletedTask;
        }

        public Task<bool> RecordActivationStepAsync(ActivationFunnelEvent item, CancellationToken ct = default)
        {
            var key = $"{item.ClientId}:{item.UserId}:{item.Scope}:{item.StepCode}";
            return Task.FromResult(_activationKeys.Add(key));
        }

        public Task RecordRetentionRiskAsync(RetentionRiskEvent item, CancellationToken ct = default) => Task.CompletedTask;

        public Task CreateOperationNoteAsync(OperationNote note, CancellationToken ct = default)
        {
            Notes.Add(note);
            return Task.CompletedTask;
        }

        public Task RecordAiInsightAsync(AiOperationInsight insight, CancellationToken ct = default)
        {
            AiInsights.Add(insight);
            return Task.CompletedTask;
        }
    }
}
