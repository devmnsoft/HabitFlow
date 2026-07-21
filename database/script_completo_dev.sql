-- HabitFlow - complemento de desenvolvimento. Execute após database/script_completo.sql.
-- Usuários dev: admin@habitflow.local e user@habitflow.local. Senha documentada: Admin@123

insert into habitflow.users(id, name, email, password_hash, role, account_status, risk_status, plan, plan_status, wants_premium_notice, onboarding_completed, accepted_terms_at, accepted_privacy_at, created_at, updated_at)
values
    ('00000000-0000-0000-0000-000000000001', 'Admin Dev', 'admin@habitflow.local', '$2a$11$CwTycUXWue0Thq9StjUM0uJ8wQzJsGMjFbc3ziqE9K28dEe/O8RQq', 'Admin', 'Active', 'Normal', 'Premium', 'Active', false, true, now(), now(), now(), now()),
    ('00000000-0000-0000-0000-000000000002', 'Usuário Dev', 'user@habitflow.local', '$2a$11$CwTycUXWue0Thq9StjUM0uJ8wQzJsGMjFbc3ziqE9K28dEe/O8RQq', 'User', 'Active', 'Normal', 'Free', 'Active', false, true, now(), now(), now(), now())
on conflict(email) do nothing;


-- v4.2 habit recurrence, notifications and reports
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
-- HabitFlow v4.3 Admin Operacional, Métricas, LGPD e Suporte
create schema if not exists habitflow;

alter table habitflow.users add column if not exists blocked_at timestamp null;
alter table habitflow.users add column if not exists blocked_reason text null;
alter table habitflow.users add column if not exists suspended_at timestamp null;
alter table habitflow.users add column if not exists suspended_reason text null;
alter table habitflow.users add column if not exists admin_notes_count integer not null default 0;
alter table habitflow.users add column if not exists support_tickets_count integer not null default 0;
alter table habitflow.users add column if not exists premium_interest_at timestamp null;
alter table habitflow.users add column if not exists last_admin_review_at timestamp null;

create table if not exists habitflow.admin_user_notes (
    id uuid primary key,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    admin_user_id uuid not null references habitflow.users(id) on delete restrict,
    admin_email varchar(200) not null,
    note text not null,
    created_at timestamp not null default now()
);

create table if not exists habitflow.admin_exports (
    id uuid primary key,
    admin_user_id uuid null,
    admin_email varchar(200) null,
    export_type varchar(80) not null,
    file_name varchar(200) null,
    filters jsonb null,
    rows_count integer not null default 0,
    created_at timestamp not null default now()
);

create table if not exists habitflow.admin_dashboard_snapshots (
    id uuid primary key,
    snapshot_date date not null,
    metrics jsonb not null,
    created_at timestamp not null default now(),
    constraint uq_admin_dashboard_snapshots_snapshot_date unique(snapshot_date)
);

create index if not exists ix_users_account_status on habitflow.users(account_status);
create index if not exists ix_users_risk_status on habitflow.users(risk_status);
create index if not exists ix_users_plan on habitflow.users(plan);
create index if not exists ix_users_wants_premium_notice on habitflow.users(wants_premium_notice);
create index if not exists ix_users_last_login_at on habitflow.users(last_login_at);
create index if not exists ix_admin_user_notes_user_id on habitflow.admin_user_notes(user_id);
create index if not exists ix_admin_exports_created_at on habitflow.admin_exports(created_at);
create index if not exists ix_admin_dashboard_snapshots_snapshot_date on habitflow.admin_dashboard_snapshots(snapshot_date);

-- v4.4 Windows/IIS operations
create table if not exists habitflow.deployment_events (
    id uuid primary key,
    version varchar(80) not null,
    environment varchar(80) not null,
    hosting_mode varchar(80) null,
    action varchar(80) not null,
    status varchar(80) not null,
    notes text null,
    created_at timestamp not null default now()
);
create index if not exists ix_deployment_events_created_at on habitflow.deployment_events(created_at desc);
create index if not exists ix_deployment_events_action on habitflow.deployment_events(action);
