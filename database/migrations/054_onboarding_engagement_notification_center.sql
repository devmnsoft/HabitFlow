-- transaction-mode: runner-managed
alter table habitflow.notifications add column if not exists client_id uuid null references habitflow.clients(id);
alter table habitflow.notifications add column if not exists category varchar(40) null;
alter table habitflow.notifications add column if not exists deduplication_key varchar(160) null;
alter table habitflow.notifications add column if not exists is_archived boolean not null default false;
alter table habitflow.notifications add column if not exists archived_at timestamptz null;
alter table habitflow.notifications add column if not exists expires_at timestamptz null;
update habitflow.notifications n set client_id=u.client_id from habitflow.users u where u.id=n.user_id and n.client_id is null;
create index if not exists ix_notifications_center on habitflow.notifications(user_id,is_archived,is_read,created_at desc);
create unique index if not exists ux_notifications_deduplication on habitflow.notifications(client_id,user_id,deduplication_key) where deduplication_key is not null;

create table if not exists habitflow.reminder_dispatches(
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 habit_reminder_id uuid not null references habitflow.habit_reminders(id), habit_id uuid not null references habitflow.habits(id),
 scheduled_for_utc timestamptz not null, channel varchar(24) not null default 'in_app', status varchar(24) not null,
 attempt_count integer not null default 0, processed_at timestamptz null, error_code varchar(80) null, created_at timestamptz not null default now(),
 unique(habit_reminder_id,scheduled_for_utc,channel)
);
create index if not exists ix_reminder_dispatch_status on habitflow.reminder_dispatches(status,scheduled_for_utc);

alter table habitflow.user_summary_preferences add column if not exists client_id uuid null references habitflow.clients(id);
update habitflow.user_summary_preferences p set client_id=u.client_id from habitflow.users u where u.id=p.user_id and p.client_id is null;
