using HabitFlow.Domain;

namespace HabitFlow.Infrastructure;

public sealed class CustomerOperationsRepository(SqlExecutor db) : ICustomerOperationsRepository
{
    public Task UpsertHealthScoreAsync(CustomerHealthDecision decision, string metricsJson, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.customer_health_scores(id, client_id, score, status, churn_risk, signals, unavailable_metrics, recommended_actions, metrics_json, calculated_at, updated_at)
            values(gen_random_uuid(), @ClientId, @Score, @Status, @ChurnRisk, @Signals, @UnavailableMetrics, @RecommendedActions, cast(@MetricsJson as jsonb), now(), now())
            on conflict(client_id) do update set
                score = excluded.score,
                status = excluded.status,
                churn_risk = excluded.churn_risk,
                signals = excluded.signals,
                unavailable_metrics = excluded.unavailable_metrics,
                recommended_actions = excluded.recommended_actions,
                metrics_json = excluded.metrics_json,
                calculated_at = excluded.calculated_at,
                updated_at = now()
            """, new
        {
            decision.ClientId,
            decision.Score,
            decision.Status,
            decision.ChurnRisk,
            Signals = decision.Signals.ToArray(),
            UnavailableMetrics = decision.UnavailableMetrics.ToArray(),
            RecommendedActions = decision.RecommendedActions.ToArray(),
            MetricsJson = metricsJson
        }, ct);

    public Task RecordHealthEventAsync(Guid clientId, string status, int score, string correlationId, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.customer_health_events(id, client_id, event_code, score, status, correlation_id, created_at)
            values(gen_random_uuid(), @clientId, 'customer.health.calculated', @score, @status, @correlationId, now())
            """, new { clientId, status, score, correlationId }, ct);

    public async Task<bool> RecordActivationStepAsync(ActivationFunnelEvent item, CancellationToken ct = default)
    {
        var affected = await db.ExecuteAsync("""
            insert into habitflow.activation_funnel_events(id, client_id, user_id, scope, step_code, status, correlation_id, occurred_at)
            values(@Id, @ClientId, @UserId, @Scope, @StepCode, @Status, @CorrelationId, @OccurredAt)
            on conflict do nothing
            """, item, ct);
        return affected > 0;
    }

    public Task RecordRetentionRiskAsync(RetentionRiskEvent item, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.retention_risk_events(id, client_id, user_id, risk_code, severity, recommendation, status, correlation_id, detected_at)
            values(@Id, @ClientId, @UserId, @RiskCode, @Severity, @Recommendation, @Status, @CorrelationId, @DetectedAt)
            on conflict(client_id, risk_code, correlation_id) do nothing
            """, item, ct);

    public Task CreateOperationNoteAsync(OperationNote note, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.operation_notes(id, client_id, author_user_id, note_type, content, correlation_id, created_at)
            values(@Id, @ClientId, @AuthorUserId, @NoteType, @Content, @CorrelationId, @CreatedAt)
            """, note, ct);

    public Task RecordAiInsightAsync(AiOperationInsight insight, CancellationToken ct = default) =>
        db.ExecuteAsync("""
            insert into habitflow.ai_operation_insights(id, client_id, user_id, insight_type, provider, model, sanitized_summary, requires_human_review, correlation_id, created_at)
            values(@Id, @ClientId, @UserId, @InsightType, @Provider, @Model, @SanitizedSummary, @RequiresHumanReview, @CorrelationId, @CreatedAt)
            """, insight, ct);
}
