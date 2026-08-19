-- transaction-mode: runner-managed
alter table habitflow.habit_reminders add column if not exists locked_by varchar(160) null;
alter table habitflow.habit_reminders add column if not exists locked_until timestamptz null;
alter table habitflow.reminder_dispatches add column if not exists next_attempt_at timestamptz null;
alter table habitflow.reminder_dispatches add column if not exists locked_by varchar(160) null;
alter table habitflow.reminder_dispatches add column if not exists locked_until timestamptz null;
alter table habitflow.reminder_dispatches add column if not exists last_error_at timestamptz null;
alter table habitflow.reminder_dispatches add column if not exists correlation_id uuid null;
update habitflow.reminder_dispatches set correlation_id=gen_random_uuid() where correlation_id is null;
alter table habitflow.reminder_dispatches alter column correlation_id set not null;
create index if not exists ix_reminder_dispatch_retry on habitflow.reminder_dispatches(next_attempt_at,locked_until) where status in ('Pending','Retry','Processing');
create index if not exists ix_habit_reminder_lease on habitflow.habit_reminders(next_trigger_at,locked_until) where is_active;
