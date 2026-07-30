-- transaction-mode: transactional
BEGIN;

ALTER TABLE habitflow.goal_progress_events
    ADD COLUMN IF NOT EXISTS event_type varchar(60),
    ADD COLUMN IF NOT EXISTS new_value integer,
    ADD COLUMN IF NOT EXISTS local_date date,
    ADD COLUMN IF NOT EXISTS source_completion_id uuid,
    ADD COLUMN IF NOT EXISTS idempotency_key varchar(240),
    ADD COLUMN IF NOT EXISTS correlation_id varchar(160),
    ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;

UPDATE habitflow.goal_progress_events
   SET event_type=coalesce(event_type,source_type),
       new_value=coalesce(new_value,current_value),
       local_date=coalesce(local_date,created_at::date),
       idempotency_key=coalesce(idempotency_key,'legacy:' || id::text),
       correlation_id=coalesce(correlation_id,'legacy:' || id::text)
 WHERE event_type IS NULL OR new_value IS NULL OR local_date IS NULL
    OR idempotency_key IS NULL OR correlation_id IS NULL;

ALTER TABLE habitflow.goal_progress_events
    ALTER COLUMN event_type SET NOT NULL,
    ALTER COLUMN new_value SET NOT NULL,
    ALTER COLUMN local_date SET NOT NULL,
    ALTER COLUMN idempotency_key SET NOT NULL,
    ALTER COLUMN correlation_id SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_goal_progress_events_idempotency
    ON habitflow.goal_progress_events(idempotency_key);
CREATE INDEX IF NOT EXISTS ix_goal_progress_events_tenant_date
    ON habitflow.goal_progress_events(client_id,user_id,local_date DESC);

INSERT INTO habitflow.milestones(id,code,title,description,threshold)
VALUES ('50100000-0000-0000-0000-000000000001','first_goal_completed','Primeiro objetivo concluído','Você concluiu seu primeiro objetivo.',1)
ON CONFLICT(code) DO UPDATE SET title=excluded.title,description=excluded.description,threshold=excluded.threshold,is_active=true;

COMMIT;
