-- Migration 101: HabitFlow v6.26.0 - Homologação de Produção, Observabilidade, LGPD, Backup/Restore, Governança Operacional e Release SaaS
-- Schema habitflow

BEGIN;

CREATE SCHEMA IF NOT EXISTS habitflow;

-- 1. Observabilidade e Health Checks
CREATE TABLE IF NOT EXISTS habitflow.system_health_checks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    component_name VARCHAR(60) NOT NULL UNIQUE,
    status VARCHAR(30) NOT NULL CHECK (status IN ('Healthy', 'Degraded', 'Unhealthy', 'Disabled', 'NotConfigured')),
    duration_ms INTEGER NOT NULL DEFAULT 0,
    details TEXT NULL,
    error_message TEXT NULL,
    operational_recommendation TEXT NULL,
    last_checked_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS habitflow.system_health_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    component_name VARCHAR(60) NOT NULL,
    status VARCHAR(30) NOT NULL,
    duration_ms INTEGER NOT NULL DEFAULT 0,
    error_message TEXT NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_system_health_history_comp_date
    ON habitflow.system_health_history (component_name, recorded_at DESC);

-- 2. LGPD e Privacidade
CREATE TABLE IF NOT EXISTS habitflow.lgpd_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    request_type VARCHAR(40) NOT NULL CHECK (request_type IN ('Export', 'Deletion', 'Anonymization', 'Rectification')),
    status VARCHAR(40) NOT NULL DEFAULT 'Aberta' CHECK (status IN ('Aberta', 'Em análise', 'Aguardando confirmação', 'Processada', 'Recusada', 'Cancelada')),
    reason TEXT NULL,
    admin_notes TEXT NULL,
    processed_by_user_id UUID NULL,
    processed_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_lgpd_requests_client_user
    ON habitflow.lgpd_requests (client_id, user_id, status, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.consent_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    consent_type VARCHAR(60) NOT NULL CHECK (consent_type IN ('TermsOfService', 'PrivacyPolicy', 'Marketing', 'Analytics', 'DataProcessing')),
    granted BOOLEAN NOT NULL DEFAULT TRUE,
    policy_version VARCHAR(30) NOT NULL DEFAULT '1.0',
    ip_address VARCHAR(45) NULL,
    user_agent TEXT NULL,
    granted_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_consent_records_user
    ON habitflow.consent_records (user_id, consent_type, granted_at DESC);

-- 3. Registros de Backup e Restore
CREATE TABLE IF NOT EXISTS habitflow.backup_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    backup_type VARCHAR(30) NOT NULL CHECK (backup_type IN ('Full', 'SchemaOnly', 'DataOnly', 'Automated')),
    status VARCHAR(30) NOT NULL CHECK (status IN ('Completed', 'Failed', 'Verified', 'Restored')),
    file_name VARCHAR(200) NOT NULL,
    size_bytes BIGINT NOT NULL DEFAULT 0,
    duration_ms INTEGER NOT NULL DEFAULT 0,
    integrity_status VARCHAR(30) NOT NULL DEFAULT 'Pending' CHECK (integrity_status IN ('Pending', 'Valid', 'Corrupted', 'Skipped')),
    sha256_hash CHAR(64) NULL,
    notes TEXT NULL,
    verified_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_backup_records_created
    ON habitflow.backup_records (created_at DESC);

-- 4. Extensões para Gestão de Incidentes (SEV1 a SEV4, Causa Raiz e Próximos Passos)
ALTER TABLE habitflow.operational_incidents
    ADD COLUMN IF NOT EXISTS sev_code VARCHAR(10) NOT NULL DEFAULT 'SEV3';

ALTER TABLE habitflow.operational_incidents
    ADD COLUMN IF NOT EXISTS affected_module VARCHAR(80) NULL DEFAULT 'Geral';

ALTER TABLE habitflow.operational_incidents
    ADD COLUMN IF NOT EXISTS root_cause TEXT NULL;

ALTER TABLE habitflow.operational_incidents
    ADD COLUMN IF NOT EXISTS actions_taken TEXT NULL;

ALTER TABLE habitflow.operational_incidents
    ADD COLUMN IF NOT EXISTS next_steps TEXT NULL;

-- 5. Governança de Release e Homologação
CREATE TABLE IF NOT EXISTS habitflow.release_checklist_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    release_version VARCHAR(20) NOT NULL,
    category VARCHAR(50) NOT NULL,
    title VARCHAR(150) NOT NULL,
    description TEXT NULL,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    completed_by VARCHAR(120) NULL,
    completed_at TIMESTAMPTZ NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_release_checklist_version
    ON habitflow.release_checklist_items (release_version, sort_order ASC);

-- Seed padrão de itens do checklist de release para v6.26.0
INSERT INTO habitflow.release_checklist_items (release_version, category, title, description, is_completed, sort_order)
VALUES
    ('v6.26.0', 'Observabilidade', 'Health Checks de produção operacionais', 'Banco, migrations, IA, SMTP e webhooks monitorados', true, 1),
    ('v6.26.0', 'LGPD', 'Fluxo de consentimentos e solicitações LGPD auditado', 'Exportação, anonimização e exclusão com retenção legal', true, 2),
    ('v6.26.0', 'Backup', 'Plano de backup/restore e RPO/RTO documentados', 'RPO <= 1h e RTO <= 30m com verificação de integridade', true, 3),
    ('v6.26.0', 'Incidentes', 'Gestão de incidentes SEV1-SEV4 com comunicação', 'Painel operacional integrado e transparente para clientes', true, 4),
    ('v6.26.0', 'Segurança', 'Isolamento multi-tenant e sanitização de secrets', 'Zero vazamento de credenciais em logs e headers seguros', true, 5),
    ('v6.26.0', 'Qualidade', 'Suíte de testes automatizados e builds verdes', 'Testes unitários e de integração 100% aprovados', true, 6)
ON CONFLICT DO NOTHING;

COMMIT;
