alter table habitflow.habits add column if not exists frequency_type varchar(50) not null default 'Daily';
alter table habitflow.habits add column if not exists target_per_week integer null;
alter table habitflow.habits add column if not exists reminder_time time null;
alter table habitflow.habits add column if not exists notes text null;
alter table habitflow.habits add column if not exists sort_order integer not null default 0;
do $$ begin
  alter table habitflow.habits add constraint habits_frequency_type_check check (frequency_type in ('Daily','Weekdays','Weekends','CustomWeekly'));
exception when duplicate_object then null; end $$;
do $$ begin
  alter table habitflow.habits add constraint habits_target_per_week_check check (target_per_week is null or target_per_week between 1 and 7);
exception when duplicate_object then null; end $$;

create table if not exists habitflow.habit_week_days(
  id uuid primary key,
  habit_id uuid not null references habitflow.habits(id) on delete cascade,
  day_of_week integer not null check (day_of_week between 0 and 6),
  created_at timestamp not null,
  unique(habit_id, day_of_week)
);

create table if not exists habitflow.notifications(
  id uuid primary key,
  user_id uuid not null references habitflow.users(id),
  type varchar(80) not null,
  title varchar(160) not null,
  message text not null,
  is_read boolean not null default false,
  related_entity_type varchar(80) null,
  related_entity_id uuid null,
  created_at timestamp not null,
  read_at timestamp null
);

create table if not exists habitflow.user_reports(
  id uuid primary key,
  user_id uuid not null references habitflow.users(id),
  report_type varchar(80) not null,
  period_start date not null,
  period_end date not null,
  summary jsonb not null,
  created_at timestamp not null
);

create index if not exists ix_habit_week_days_habit_id on habitflow.habit_week_days(habit_id);
create index if not exists ix_notifications_user_id on habitflow.notifications(user_id);
create index if not exists ix_notifications_is_read on habitflow.notifications(is_read);
create index if not exists ix_notifications_created_at on habitflow.notifications(created_at);
create index if not exists ix_user_reports_user_id on habitflow.user_reports(user_id);
create index if not exists ix_user_reports_period_start on habitflow.user_reports(period_start);
create index if not exists ix_user_reports_period_end on habitflow.user_reports(period_end);
