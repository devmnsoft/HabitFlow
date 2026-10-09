-- Migration 100: HabitFlow v6.25.0 - PWA Mobile, Offline-First, Notificações Reais, Integrações, API Pública, Webhooks e Portabilidade SaaS
-- Schema habitflow

BEGIN;

CREATE SCHEMA IF NOT EXISTS habitflow;

-- Fila de sincronizacao offline controlada com suporte a status: Pending, Synced, Failed, Conflict, Discarded
CREATE TABLE IF NOT EXISTS habitflow.offline_sync_queue (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    action_type VARCHAR(40) NOT NULL,
    entity_id UUID NULL,
    payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    status VARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending', 'Synced', 'Failed', 'Conflict', 'Discarded')),
    error_message TEXT NULL,
    conflict_details JSONB NULL,
    client_created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    synced_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_offline_sync_queue_client_user
    ON habitflow.offline_sync_queue (client_id, user_id, status, created_at DESC);

-- Evolucao de webhooks: colunas para controle de retries, backoff e pausa automatica
ALTER TABLE habitflow.integration_webhooks
    ADD COLUMN IF NOT EXISTS consecutive_failures INTEGER NOT NULL DEFAULT 0;

ALTER TABLE habitflow.integration_webhooks
    ADD COLUMN IF NOT EXISTS last_failed_at TIMESTAMPTZ NULL;

ALTER TABLE habitflow.integration_webhooks
    ADD COLUMN IF NOT EXISTS is_paused BOOLEAN NOT NULL DEFAULT FALSE;

-- Evolucao das tentativas de entrega de webhooks
ALTER TABLE habitflow.webhook_delivery_attempts
    ADD COLUMN IF NOT EXISTS response_body TEXT NULL;

ALTER TABLE habitflow.webhook_delivery_attempts
    ADD COLUMN IF NOT EXISTS error_message TEXT NULL;

ALTER TABLE habitflow.webhook_delivery_attempts
    ADD COLUMN IF NOT EXISTS duration_ms INTEGER NULL;

-- Portabilidade e exportacao de dados pessoais e de tenant (LGPD)
CREATE TABLE IF NOT EXISTS habitflow.data_export_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    scope VARCHAR(20) NOT NULL CHECK (scope IN ('User', 'Tenant')),
    format VARCHAR(10) NOT NULL CHECK (format IN ('csv', 'json')),
    status VARCHAR(20) NOT NULL DEFAULT 'Completed' CHECK (status IN ('Completed', 'Failed')),
    file_name VARCHAR(150) NOT NULL,
    record_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_data_export_requests_client_user
    ON habitflow.data_export_requests (client_id, user_id, created_at DESC);

-- Importacao em lote com simulacao previa, validacao e relatorio de erros
CREATE TABLE IF NOT EXISTS habitflow.data_import_batches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    entity_type VARCHAR(30) NOT NULL CHECK (entity_type IN ('habits', 'templates')),
    format VARCHAR(10) NOT NULL CHECK (format IN ('csv', 'json')),
    status VARCHAR(20) NOT NULL DEFAULT 'Completed' CHECK (status IN ('Simulated', 'Completed', 'Failed')),
    total_rows INTEGER NOT NULL DEFAULT 0,
    imported_rows INTEGER NOT NULL DEFAULT 0,
    failed_rows INTEGER NOT NULL DEFAULT 0,
    errors_json JSONB NOT NULL DEFAULT '[]'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_data_import_batches_client_user
    ON habitflow.data_import_batches (client_id, user_id, created_at DESC);

-- Recomendacoes e intervencoes de IA para mobile e integracoes (sempre sanitizadas)
CREATE TABLE IF NOT EXISTS habitflow.ai_mobile_recommendations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    recommendation_type VARCHAR(50) NOT NULL,
    provider VARCHAR(30) NOT NULL,
    model VARCHAR(50) NOT NULL,
    content TEXT NOT NULL,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_ai_mobile_rec_client_user
    ON habitflow.ai_mobile_recommendations (client_id, user_id, recommendation_type, created_at DESC);

-- Catalogo de novos recursos por plano para v6.25.0
INSERT INTO habitflow.plan_features (code, name, value_type, default_bool_value, default_int_value, default_string_value)
VALUES
    ('webhooks', 'Webhooks de Saída', 'boolean', false, null, null),
    ('public_api', 'API Pública Rest', 'boolean', false, null, null),
    ('data_portability', 'Portabilidade de Dados (Export/Import)', 'boolean', true, null, null),
    ('push_notifications', 'Notificações Web Push', 'boolean', false, null, null),
    ('advanced_offline', 'Modo Offline Avançado', 'boolean', false, null, null)
ON CONFLICT (code) DO NOTHING;

-- Configuracao de entitlements nos planos padrao
-- Plano Pro/Ritmo
INSERT INTO habitflow.plan_feature_entitlements (plan_code, feature_code, bool_value, int_value)
VALUES
    ('ritmo', 'webhooks', true, 2),
    ('ritmo', 'public_api', true, 3),
    ('ritmo', 'data_portability', true, null),
    ('ritmo', 'push_notifications', true, null),
    ('ritmo', 'advanced_offline', true, null),
    ('evolucao', 'webhooks', true, 10),
    ('evolucao', 'public_api', true, 10),
    ('evolucao', 'data_portability', true, null),
    ('evolucao', 'push_notifications', true, null),
    ('evolucao', 'advanced_offline', true, null),
    ('team', 'webhooks', true, 20),
    ('team', 'public_api', true, 20),
    ('team', 'data_portability', true, null),
    ('team', 'push_notifications', true, null),
    ('team', 'advanced_offline', true, null),
    ('enterprise', 'webhooks', true, -1),
    ('enterprise', 'public_api', true, -1),
    ('enterprise', 'data_portability', true, null),
    ('enterprise', 'push_notifications', true, null),
    ('enterprise', 'advanced_offline', true, null),
    ('free', 'data_portability', true, null),
    ('free', 'webhooks', false, 0),
    ('free', 'public_api', false, 0),
    ('free', 'push_notifications', false, null),
    ('free', 'advanced_offline', false, null)
ON CONFLICT (plan_code, feature_code) DO NOTHING;

COMMIT;
