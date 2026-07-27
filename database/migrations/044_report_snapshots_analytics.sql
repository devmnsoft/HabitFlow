BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.report_snapshots (
 id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid REFERENCES habitflow.users(id),
 report_type varchar(80) NOT NULL, period_start date NOT NULL, period_end date NOT NULL, data jsonb NOT NULL,
 generated_at timestamp NOT NULL DEFAULT now(), CHECK(period_end>=period_start)
);
CREATE INDEX IF NOT EXISTS ix_report_snapshots_scope ON habitflow.report_snapshots(client_id,user_id,report_type,period_start,period_end);
COMMIT;
