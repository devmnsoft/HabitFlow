-- habitflow:transaction=runner
-- Reminder instants were historically stored as timestamp without time zone.  The
-- application has always written UTC, so preserve the wall-clock value while
-- making that contract explicit to PostgreSQL.
alter table habitflow.habit_reminders
  alter column next_trigger_at type timestamptz using next_trigger_at at time zone 'UTC',
  alter column last_triggered_at type timestamptz using last_triggered_at at time zone 'UTC';

create index if not exists ix_habit_reminders_dispatch_due
  on habitflow.habit_reminders(next_trigger_at, id)
  where is_active and next_trigger_at is not null;
create index if not exists ix_reminder_dispatches_lease_recovery
  on habitflow.reminder_dispatches(locked_until)
  where locked_until is not null and status in ('Pending','Processing','Retry');
