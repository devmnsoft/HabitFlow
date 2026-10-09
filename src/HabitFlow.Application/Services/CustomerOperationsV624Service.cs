using System.Text.Json;
using HabitFlow.Domain;
using HabitFlow.Shared;
using Microsoft.Extensions.Logging;

namespace HabitFlow.Application;

public sealed class CustomerOperationsV624Service(ICustomerOperationsRepository repository, ILogger<CustomerOperationsV624Service> logger)
{
    public async Task<CustomerHealthDecision> CalculateHealthAsync(CustomerHealthSignal signal, string correlationId, CancellationToken ct = default)
    {
        var decision = CustomerHealthV624Policy.Evaluate(signal);
        var metricsJson = JsonSerializer.Serialize(new
        {
            signal.ActiveUsers7Days,
            signal.ActiveUsers30Days,
            signal.HabitsCreated,
            signal.HabitCompletions30Days,
            signal.AverageStreak,
            signal.TemplatesUsed,
            signal.JourneysUsed,
            signal.AiRequests30Days,
            signal.WeeklyReviewCompleted,
            signal.PendingInvites,
            signal.HasPaymentFailure,
            signal.TrialDaysRemaining,
            signal.OpenTickets
        });
        await repository.UpsertHealthScoreAsync(decision, metricsJson, ct);
        await repository.RecordHealthEventAsync(signal.ClientId, decision.Status, decision.Score, correlationId, ct);
        logger.LogInformation(ApplicationEvents.CustomerHealthCalculated,
            "customer.health.calculated ClientId={ClientId} Score={Score} Status={Status} CorrelationId={CorrelationId}",
            signal.ClientId, decision.Score, decision.Status, correlationId);
        return decision;
    }

    public async Task<Result<bool>> CompleteActivationStepAsync(Guid clientId, Guid? userId, string scope, string stepCode, string correlationId, CancellationToken ct = default)
    {
        if (clientId == Guid.Empty)
            return Result<bool>.Failure("activation.tenant_required", "A conta e obrigatoria.");
        if (!scope.Equals("User", StringComparison.OrdinalIgnoreCase) && !scope.Equals("Tenant", StringComparison.OrdinalIgnoreCase))
            return Result<bool>.Failure("activation.scope_invalid", "Escopo de funil invalido.");
        if (!ActivationFunnelPolicy.IsKnown(scope, stepCode))
            return Result<bool>.Failure("activation.step_invalid", "Etapa de ativacao desconhecida.");
        if (scope.Equals("User", StringComparison.OrdinalIgnoreCase) && userId is null)
            return Result<bool>.Failure("activation.user_required", "Usuario obrigatorio para etapa de usuario.");

        var inserted = await repository.RecordActivationStepAsync(new ActivationFunnelEvent(
            Guid.NewGuid(), clientId, userId, scope, stepCode, "Completed", correlationId, DateTime.UtcNow), ct);
        if (inserted)
            logger.LogInformation(ApplicationEvents.ActivationStepCompleted,
                "activation.step.completed ClientId={ClientId} UserId={UserId} Scope={Scope} Step={Step} CorrelationId={CorrelationId}",
                clientId, userId, scope, stepCode, correlationId);
        return Result<bool>.Success(inserted);
    }

    public async Task<IReadOnlyList<RetentionRiskEvent>> DetectRetentionRisksAsync(CustomerHealthSignal signal, Guid? userId, string correlationId, CancellationToken ct = default)
    {
        var risks = RetentionRiskPolicy.Detect(signal, userId, correlationId);
        foreach (var risk in risks)
        {
            await repository.RecordRetentionRiskAsync(risk, ct);
            logger.LogWarning(ApplicationEvents.RetentionRiskDetected,
                "retention.risk.detected ClientId={ClientId} UserId={UserId} RiskCode={RiskCode} Severity={Severity} CorrelationId={CorrelationId}",
                risk.ClientId, risk.UserId, risk.RiskCode, risk.Severity, correlationId);
        }
        return risks;
    }

    public Task CreateOperationNoteAsync(Guid clientId, Guid authorUserId, string noteType, string content, string correlationId, CancellationToken ct = default)
    {
        var clean = content?.Trim() ?? "";
        if (clean.Length < 5 || clean.Length > 2000)
            throw new ArgumentException("A observacao operacional deve ter entre 5 e 2000 caracteres.", nameof(content));
        if (!AllowedNoteTypes.Contains(noteType, StringComparer.Ordinal))
            throw new ArgumentException("Tipo de observacao operacional invalido.", nameof(noteType));

        logger.LogInformation(ApplicationEvents.OperationNoteCreated,
            "operation.note.created ClientId={ClientId} AuthorUserId={AuthorUserId} NoteType={NoteType} CorrelationId={CorrelationId}",
            clientId, authorUserId, noteType, correlationId);
        return repository.CreateOperationNoteAsync(new OperationNote(Guid.NewGuid(), clientId, authorUserId, noteType, clean, correlationId, DateTime.UtcNow), ct);
    }

    public Task RecordAiInsightAsync(Guid clientId, Guid? userId, string insightType, string provider, string model, string sanitizedSummary, string correlationId, CancellationToken ct = default)
    {
        var clean = sanitizedSummary.Length <= 4000 ? sanitizedSummary : sanitizedSummary[..4000];
        logger.LogInformation(ApplicationEvents.AiOperationInsightCreated,
            "ai.operation.insight.created ClientId={ClientId} UserId={UserId} InsightType={InsightType} Provider={Provider} CorrelationId={CorrelationId}",
            clientId, userId, insightType, provider, correlationId);
        return repository.RecordAiInsightAsync(new AiOperationInsight(Guid.NewGuid(), clientId, userId, insightType, provider, model, clean, true, correlationId, DateTime.UtcNow), ct);
    }

    private static readonly string[] AllowedNoteTypes =
    [
        "General", "RiskFlag", "CallLog", "PlanReview", "ActionPlan", "CommercialFollowUp"
    ];
}
