-- Migration 099: HabitFlow v6.24.0 - Operacao SaaS Inteligente, Retencao, Saude do Cliente e Growth
-- Schema habitflow

CREATE SCHEMA IF NOT EXISTS habitflow;

-- Saude operacional consolidada por tenant, com sinais auditaveis e acoes recomendadas.
CREATE TABLE IF NOT EXISTS habitflow.customer_health_scores (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    score INTEGER NOT NULL CHECK (score BETWEEN 0 AND 100),
    status VARCHAR(30) NOT NULL CHECK (status IN ('Healthy', 'Attention', 'AtRisk', 'Critical', 'Inactive')),
    churn_risk BOOLEAN NOT NULL DEFAULT FALSE,
    signals TEXT[] NOT NULL DEFAULT ARRAY[]::TEXT[],
    unavailable_metrics TEXT[] NOT NULL DEFAULT ARRAY[]::TEXT[],
    recommended_actions TEXT[] NOT NULL DEFAULT ARRAY[]::TEXT[],
    metrics_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    calculated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_customer_health_scores_client UNIQUE (client_id)
);

CREATE INDEX IF NOT EXISTS idx_customer_health_scores_status
    ON habitflow.customer_health_scores (status, score DESC, updated_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.customer_health_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    event_code VARCHAR(100) NOT NULL,
    score INTEGER NOT NULL CHECK (score BETWEEN 0 AND 100),
    status VARCHAR(30) NOT NULL CHECK (status IN ('Healthy', 'Attention', 'AtRisk', 'Critical', 'Inactive')),
    correlation_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_customer_health_events_client_created
    ON habitflow.customer_health_events (client_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_customer_health_events_correlation
    ON habitflow.customer_health_events (correlation_id);

-- Funil canonico de ativacao por usuario e por tenant.
CREATE TABLE IF NOT EXISTS habitflow.activation_funnel_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    scope VARCHAR(20) NOT NULL CHECK (scope IN ('User', 'Tenant')),
    step_code VARCHAR(80) NOT NULL,
    status VARCHAR(30) NOT NULL CHECK (status IN ('Completed', 'Skipped', 'Blocked')),
    correlation_id VARCHAR(100) NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_activation_funnel_events_step
    ON habitflow.activation_funnel_events (
        client_id,
        scope,
        step_code,
        COALESCE(user_id, '00000000-0000-0000-0000-000000000000'::uuid)
    );
CREATE INDEX IF NOT EXISTS idx_activation_funnel_events_client_scope
    ON habitflow.activation_funnel_events (client_id, scope, occurred_at DESC);
CREATE INDEX IF NOT EXISTS idx_activation_funnel_events_user
    ON habitflow.activation_funnel_events (user_id, occurred_at DESC);

-- Riscos de retencao tratados como eventos idempotentes e acompanhaveis.
CREATE TABLE IF NOT EXISTS habitflow.retention_risk_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    risk_code VARCHAR(80) NOT NULL,
    severity VARCHAR(20) NOT NULL CHECK (severity IN ('Low', 'Medium', 'High', 'Critical')),
    recommendation TEXT NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Open' CHECK (status IN ('Open', 'Acknowledged', 'Resolved', 'Dismissed')),
    correlation_id VARCHAR(100) NOT NULL,
    detected_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolved_at TIMESTAMPTZ NULL,
    CONSTRAINT uq_retention_risk_client_code_correlation UNIQUE (client_id, risk_code, correlation_id)
);

CREATE INDEX IF NOT EXISTS idx_retention_risk_events_client_status
    ON habitflow.retention_risk_events (client_id, status, severity, detected_at DESC);
CREATE INDEX IF NOT EXISTS idx_retention_risk_events_user
    ON habitflow.retention_risk_events (user_id, detected_at DESC);

-- Notas operacionais de CS, suporte e plano de acao.
CREATE TABLE IF NOT EXISTS habitflow.operation_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    author_user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE RESTRICT,
    note_type VARCHAR(30) NOT NULL CHECK (note_type IN ('General', 'RiskFlag', 'CallLog', 'PlanReview', 'ActionPlan', 'CommercialFollowUp')),
    content TEXT NOT NULL CHECK (char_length(content) BETWEEN 5 AND 2000),
    correlation_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_operation_notes_client_created
    ON habitflow.operation_notes (client_id, created_at DESC);

-- Insights de IA operacional sempre higienizados e pendentes de revisao humana.
CREATE TABLE IF NOT EXISTS habitflow.ai_operation_insights (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    insight_type VARCHAR(80) NOT NULL,
    provider VARCHAR(50) NOT NULL,
    model VARCHAR(100) NOT NULL,
    sanitized_summary TEXT NOT NULL CHECK (char_length(sanitized_summary) <= 4000),
    requires_human_review BOOLEAN NOT NULL DEFAULT TRUE,
    correlation_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_ai_operation_insights_client_created
    ON habitflow.ai_operation_insights (client_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_ai_operation_insights_correlation
    ON habitflow.ai_operation_insights (correlation_id);

-- Compatibilidade do snapshot v6.21 com novos estados de saude v6.24.
ALTER TABLE habitflow.tenant_health_snapshots
    DROP CONSTRAINT IF EXISTS tenant_health_snapshots_health_status_check;
ALTER TABLE habitflow.tenant_health_snapshots
    ADD CONSTRAINT tenant_health_snapshots_health_status_check
    CHECK (health_status IN ('Healthy', 'Attention', 'AtRisk', 'Blocked', 'TrialEnding', 'PaymentIssue', 'Critical', 'Inactive'));
