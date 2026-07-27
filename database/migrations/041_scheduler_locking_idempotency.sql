BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.job_locks (
 job_name varchar(120) PRIMARY KEY, locked_by varchar(200), locked_at timestamp,
 lock_expires_at timestamp, updated_at timestamp NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS habitflow.notification_deliveries (
 id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id),
 source_type varchar(80) NOT NULL, source_id varchar(160) NOT NULL, channel varchar(40) NOT NULL,
 scheduled_for timestamp NOT NULL, status varchar(40) NOT NULL, delivered_at timestamp, failure_reason text,
 created_at timestamp NOT NULL DEFAULT now(), UNIQUE(source_type,source_id,channel,scheduled_for)
);
CREATE INDEX IF NOT EXISTS ix_notification_deliveries_scope ON habitflow.notification_deliveries(client_id,user_id,scheduled_for DESC);
COMMIT;
