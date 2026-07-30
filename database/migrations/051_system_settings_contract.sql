-- transaction-mode: runner-managed
alter table habitflow.system_settings add column if not exists description text null;
alter table habitflow.system_settings add column if not exists is_public boolean not null default false;
alter table habitflow.system_settings add column if not exists created_at timestamp null;
update habitflow.system_settings set created_at = coalesce(created_at, updated_at, now()) where created_at is null;
alter table habitflow.system_settings alter column created_at set default now();
alter table habitflow.system_settings alter column created_at set not null;
alter table habitflow.system_settings alter column is_public set default false;

