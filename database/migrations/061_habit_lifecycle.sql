-- Habit lifecycle is reversible: pausing and archiving never remove completions.
begin;
alter table habitflow.habits add column if not exists is_paused boolean not null default false;
alter table habitflow.habits add column if not exists paused_at timestamptz null;
create index if not exists ix_habits_tenant_user_lifecycle
    on habitflow.habits(client_id, user_id, is_archived, is_paused, updated_at desc);
commit;
