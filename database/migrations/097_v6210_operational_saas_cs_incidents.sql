-- Migration 097: HabitFlow v6.21.0 - Operação SaaS, Customer Success, Incidentes e Suporte Avançado
-- Schema habitflow

CREATE SCHEMA IF NOT EXISTS habitflow;

-- 1. Colunas adicionais no suporte v2 para ciclo completo e satisfação
ALTER TABLE habitflow.support_tickets_v2 
    ADD COLUMN IF NOT EXISTS resolution_reason VARCHAR(1000) NULL,
    ADD COLUMN IF NOT EXISTS satisfaction_rating INTEGER NULL CHECK (satisfaction_rating BETWEEN 1 AND 5),
    ADD COLUMN IF NOT EXISTS satisfaction_feedback VARCHAR(1000) NULL,
    ADD COLUMN IF NOT EXISTS reopened_at TIMESTAMPTZ NULL,
    ADD COLUMN IF NOT EXISTS resolved_at TIMESTAMPTZ NULL;


-- 2. Tabela de Incidentes Operacionais
CREATE TABLE IF NOT EXISTS habitflow.operational_incidents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    severity VARCHAR(20) NOT NULL CHECK (severity IN ('Info', 'Minor', 'Major', 'Critical')),
    status VARCHAR(20) NOT NULL CHECK (status IN ('Investigating', 'Identified', 'Monitoring', 'Resolved', 'Canceled')),
    impact TEXT NOT NULL,
    affected_tenants_count INTEGER NOT NULL DEFAULT 0,
    starts_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    estimated_resolution_at TIMESTAMPTZ NULL,
    resolved_at TIMESTAMPTZ NULL,
    canceled_at TIMESTAMPTZ NULL,
    responsible_user_id VARCHAR(100) NULL,
    responsible_name VARCHAR(150) NULL,
    communication_sent BOOLEAN NOT NULL DEFAULT FALSE,
    communication_notes TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_operational_incidents_status_severity 
    ON habitflow.operational_incidents (status, severity);
CREATE INDEX IF NOT EXISTS idx_operational_incidents_created_at 
    ON habitflow.operational_incidents (created_at DESC);

-- 3. Tabela de Tenants Afetados por Incidente
CREATE TABLE IF NOT EXISTS habitflow.incident_tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    incident_id UUID NOT NULL REFERENCES habitflow.operational_incidents(id) ON DELETE CASCADE,
    client_id UUID NOT NULL,
    impact_summary VARCHAR(500) NULL,
    notified BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_incident_tenants_incident_client 
    ON habitflow.incident_tenants (incident_id, client_id);
CREATE INDEX IF NOT EXISTS idx_incident_tenants_client 
    ON habitflow.incident_tenants (client_id);

-- 4. Snapshots de Saúde do Tenant (Customer Success)
CREATE TABLE IF NOT EXISTS habitflow.tenant_health_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL,
    score INTEGER NOT NULL CHECK (score BETWEEN 0 AND 100),
    health_status VARCHAR(30) NOT NULL CHECK (health_status IN ('Healthy', 'Attention', 'AtRisk', 'Blocked', 'TrialEnding', 'PaymentIssue')),
    risk_factors TEXT[] NULL,
    metrics_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    captured_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_tenant_health_snapshots_client_captured 
    ON habitflow.tenant_health_snapshots (client_id, captured_at DESC);
CREATE INDEX IF NOT EXISTS idx_tenant_health_snapshots_status 
    ON habitflow.tenant_health_snapshots (health_status, captured_at DESC);

-- 5. Notas e Alertas de Customer Success
CREATE TABLE IF NOT EXISTS habitflow.customer_success_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL,
    author_user_id VARCHAR(100) NOT NULL,
    author_name VARCHAR(150) NOT NULL,
    note_type VARCHAR(30) NOT NULL DEFAULT 'General' CHECK (note_type IN ('General', 'RiskFlag', 'CallLog', 'PlanReview', 'ActionPlan')),
    content TEXT NOT NULL,
    is_risk_flag BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_cs_notes_client_created 
    ON habitflow.customer_success_notes (client_id, created_at DESC);

-- 6. Eventos de Auditoria Operacional Consolidada
CREATE TABLE IF NOT EXISTS habitflow.operational_audit_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_name VARCHAR(100) NOT NULL,
    correlation_id VARCHAR(100) NOT NULL,
    client_id UUID NULL,
    executor_user_id VARCHAR(100) NULL,
    executor_email VARCHAR(200) NULL,
    severity VARCHAR(20) NOT NULL DEFAULT 'Info' CHECK (severity IN ('Info', 'Warning', 'Error', 'Critical')),
    status VARCHAR(30) NOT NULL DEFAULT 'Success',
    payload_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_operational_audit_event_name_occurred 
    ON habitflow.operational_audit_events (event_name, occurred_at DESC);
CREATE INDEX IF NOT EXISTS idx_operational_audit_client_occurred 
    ON habitflow.operational_audit_events (client_id, occurred_at DESC);
CREATE INDEX IF NOT EXISTS idx_operational_audit_correlation 
    ON habitflow.operational_audit_events (correlation_id);
