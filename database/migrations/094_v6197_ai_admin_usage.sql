-- HabitFlow v6.19.7: painel de IA. Sem prompt, sem API key.
BEGIN;
SET LOCAL search_path TO habitflow, public;

CREATE TABLE IF NOT EXISTS habitflow.ai_settings (
 id boolean PRIMARY KEY DEFAULT true CHECK (id),
 enabled boolean NOT NULL DEFAULT false,
 default_provider varchar(40) NOT NULL DEFAULT '',
 groq_enabled boolean NOT NULL DEFAULT false,
 gemini_enabled boolean NOT NULL DEFAULT false,
 deepseek_enabled boolean NOT NULL DEFAULT false,
 groq_models text NOT NULL DEFAULT '',
 gemini_models text NOT NULL DEFAULT '',
 deepseek_models text NOT NULL DEFAULT '',
 global_daily_limit integer NOT NULL DEFAULT 200,
 updated_by uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
 updated_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_ai_settings_global_limit CHECK (global_daily_limit BETWEEN 0 AND 100000),
 CONSTRAINT ck_ai_settings_provider CHECK (default_provider IN ('', 'Groq', 'Gemini', 'DeepSeek'))
);
INSERT INTO habitflow.ai_settings(id) VALUES (true) ON CONFLICT (id) DO NOTHING;

CREATE TABLE IF NOT EXISTS habitflow.ai_plan_limits (
 plan_code varchar(40) PRIMARY KEY,
 daily_limit integer NOT NULL,
 updated_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_ai_plan_limits_daily CHECK (daily_limit BETWEEN 0 AND 100000)
);
INSERT INTO habitflow.ai_plan_limits(plan_code, daily_limit) VALUES
 ('free', 5), ('ritmo', 40), ('team', 40), ('enterprise', 80)
ON CONFLICT (plan_code) DO NOTHING;

CREATE TABLE IF NOT EXISTS habitflow.ai_tenant_limits (
 client_id uuid PRIMARY KEY REFERENCES habitflow.clients(id) ON DELETE CASCADE,
 daily_limit integer NOT NULL,
 updated_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_ai_tenant_limits_daily CHECK (daily_limit BETWEEN 0 AND 100000)
);

CREATE TABLE IF NOT EXISTS habitflow.ai_usage_events (
 id uuid PRIMARY KEY,
 client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
 user_id uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
 provider varchar(40) NOT NULL,
 model varchar(80) NOT NULL DEFAULT '',
 status varchar(40) NOT NULL,
 event_code varchar(80) NOT NULL,
 correlation_id varchar(100) NOT NULL,
 duration_ms integer NOT NULL DEFAULT 0,
 created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_ai_usage_events_tenant_date ON habitflow.ai_usage_events(client_id, created_at DESC, status);
CREATE INDEX IF NOT EXISTS ix_ai_usage_events_user_date ON habitflow.ai_usage_events(client_id, user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_ai_usage_events_provider_date ON habitflow.ai_usage_events(provider, created_at DESC, status);
CREATE INDEX IF NOT EXISTS ix_ai_usage_events_status_date ON habitflow.ai_usage_events(status, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.ai_provider_events (
 id uuid PRIMARY KEY,
 client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
 user_id uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
 provider varchar(40) NOT NULL,
 model varchar(80) NOT NULL DEFAULT '',
 status varchar(40) NOT NULL,
 event_code varchar(80) NOT NULL,
 correlation_id varchar(100) NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_ai_provider_events_tenant_date ON habitflow.ai_provider_events(client_id, created_at DESC, status);
CREATE INDEX IF NOT EXISTS ix_ai_provider_events_provider_date ON habitflow.ai_provider_events(provider, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.ai_safety_events (
 id uuid PRIMARY KEY,
 client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
 user_id uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
 status varchar(40) NOT NULL,
 event_code varchar(80) NOT NULL,
 correlation_id varchar(100) NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_ai_safety_events_tenant_date ON habitflow.ai_safety_events(client_id, created_at DESC, status);

COMMIT;
