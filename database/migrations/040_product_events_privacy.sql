BEGIN;
ALTER TABLE habitflow.habits ADD COLUMN IF NOT EXISTS visibility varchar(40) NOT NULL DEFAULT 'Private';
ALTER TABLE habitflow.habits DROP CONSTRAINT IF EXISTS ck_habits_visibility;
ALTER TABLE habitflow.habits ADD CONSTRAINT ck_habits_visibility CHECK(visibility IN ('Private','SharedWithRoutine','AggregateOnly'));
CREATE TABLE IF NOT EXISTS habitflow.product_events (id uuid PRIMARY KEY, client_id uuid REFERENCES habitflow.clients(id), user_id uuid REFERENCES habitflow.users(id), event_name varchar(120) NOT NULL, entity_type varchar(80), entity_id uuid, plan_code varchar(80), metadata jsonb, occurred_at timestamp NOT NULL DEFAULT now(), session_id varchar(120));
CREATE INDEX IF NOT EXISTS ix_product_events_occurred ON habitflow.product_events(occurred_at DESC,event_name);
CREATE INDEX IF NOT EXISTS ix_product_events_scope ON habitflow.product_events(client_id,user_id,occurred_at DESC);
COMMENT ON COLUMN habitflow.product_events.metadata IS 'Somente metadados operacionais; nunca documentos, credenciais ou conteúdo de hábitos.';
COMMIT;
