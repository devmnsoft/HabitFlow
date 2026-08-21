-- v6.16.6: tenant-safe, idempotent report snapshots.
begin;
alter table habitflow.user_reports add column if not exists client_id uuid null;
alter table habitflow.user_reports add column if not exists algorithm_version integer not null default 1;
update habitflow.user_reports r set client_id=u.client_id from habitflow.users u where r.user_id=u.id and r.client_id is null;
alter table habitflow.user_reports alter column client_id set not null;
create index if not exists ix_user_reports_tenant_owner_period on habitflow.user_reports(client_id,user_id,period_start desc);
create unique index if not exists ux_user_reports_snapshot_version on habitflow.user_reports(client_id,user_id,report_type,period_start,algorithm_version);
commit;
