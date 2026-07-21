-- HabitFlow - Script completo de produção
-- Schema oficial: habitflow
-- Este script não cria tabelas no schema public
-- Este script não cria usuários de teste
-- Execute em banco PostgreSQL limpo ou controlado

create schema if not exists habitflow;

create table if not exists habitflow.users (
    id uuid primary key,
    name varchar(150) not null,
    email varchar(200) not null unique,
    password_hash text not null,
    photo_url text null,
    role varchar(50) not null default 'User',
    account_status varchar(50) not null default 'Active',
    risk_status varchar(50) not null default 'Normal',
    plan varchar(50) not null default 'Free',
    plan_status varchar(50) not null default 'Active',
    wants_premium_notice boolean not null default false,
    onboarding_completed boolean not null default false,
    accepted_terms_at timestamp null,
    accepted_privacy_at timestamp null,
    last_login_at timestamp null,
    last_activity_at timestamp null,
    created_at timestamp not null default now(),
    updated_at timestamp not null default now(),
    constraint ck_habitflow_users_role check (role in ('User', 'Admin')),
    constraint ck_habitflow_users_account_status check (account_status in ('Active', 'Blocked', 'Suspended', 'DeletedPending')),
    constraint ck_habitflow_users_risk_status check (risk_status in ('Normal', 'Watchlist', 'Suspicious')),
    constraint ck_habitflow_users_plan check (plan in ('Free', 'Premium')),
    constraint ck_habitflow_users_plan_status check (plan_status in ('Active', 'Trial', 'Canceled', 'Inactive', 'PastDue'))
);

create table if not exists habitflow.login_attempts (
    id uuid primary key,
    email varchar(200) null,
    success boolean not null,
    ip_address varchar(100) null,
    user_agent text null,
    created_at timestamp not null default now()
);

create table if not exists habitflow.habits (
    id uuid primary key,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    name varchar(120) not null,
    color varchar(20) not null,
    category varchar(80) null,
    is_archived boolean not null default false,
    archived_at timestamp null,
    created_at timestamp not null default now(),
    updated_at timestamp not null default now()
);

create table if not exists habitflow.habit_completions (
    id uuid primary key,
    habit_id uuid not null references habitflow.habits(id) on delete cascade,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    completed_date date not null,
    created_at timestamp not null default now(),
    constraint uq_habitflow_habit_completions_habit_date unique (habit_id, completed_date)
);

create table if not exists habitflow.support_tickets (
    id uuid primary key,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    protocol varchar(50) not null unique,
    type varchar(50) not null,
    status varchar(50) not null,
    priority varchar(50) not null,
    title varchar(200) not null,
    description text null,
    source varchar(50) null,
    created_at timestamp not null default now(),
    updated_at timestamp not null default now(),
    resolved_at timestamp null,
    constraint ck_habitflow_support_tickets_status check (status in ('Open', 'InProgress', 'Resolved', 'Closed'))
);

create table if not exists habitflow.support_messages (
    id uuid primary key,
    ticket_id uuid not null references habitflow.support_tickets(id) on delete cascade,
    user_id uuid null references habitflow.users(id) on delete set null,
    role varchar(50) not null,
    message text not null,
    is_sensitive_blocked boolean not null default false,
    created_at timestamp not null default now()
);

create table if not exists habitflow.system_audit_logs (
    id uuid primary key,
    user_id uuid null,
    user_email varchar(200) null,
    severity varchar(50) not null,
    source varchar(50) not null,
    action varchar(100) not null,
    message text not null,
    metadata jsonb null,
    error_code varchar(100) null,
    error_fingerprint varchar(200) null,
    created_at timestamp not null default now(),
    read_by_admin boolean not null default false,
    constraint ck_system_audit_logs_severity check (severity in ('Info', 'Warning', 'Error', 'Critical'))
);

create table if not exists habitflow.admin_audit_logs (
    id uuid primary key,
    admin_user_id uuid null,
    admin_email varchar(200) null,
    action varchar(100) not null,
    target_user_id uuid null,
    target_user_email varchar(200) null,
    reason text null,
    metadata jsonb null,
    created_at timestamp not null default now()
);

create table if not exists habitflow.system_settings (
    key varchar(100) primary key,
    value jsonb not null,
    updated_at timestamp not null default now(),
    updated_by uuid null
);

create table if not exists habitflow.lgpd_requests (
    id uuid primary key,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    protocol varchar(50) not null unique,
    type varchar(50) not null,
    status varchar(50) not null,
    notes text null,
    rejection_reason text null,
    handled_by uuid null,
    created_at timestamp not null default now(),
    updated_at timestamp not null default now(),
    completed_at timestamp null,
    constraint ck_habitflow_lgpd_requests_type check (type in ('Export', 'Delete')),
    constraint ck_habitflow_lgpd_requests_status check (status in ('Requested', 'InReview', 'Processing', 'Completed', 'Rejected', 'Canceled'))
);

create table if not exists habitflow.billing_events (
    id uuid primary key,
    user_id uuid null references habitflow.users(id) on delete set null,
    provider varchar(50) null,
    event_type varchar(100) not null,
    plan varchar(50) null,
    status varchar(50) null,
    amount numeric(12,2) null,
    metadata jsonb null,
    created_at timestamp not null default now(),
    constraint ck_habitflow_billing_events_plan check (plan is null or plan in ('Free', 'Premium'))
);

create index if not exists ix_habitflow_users_email on habitflow.users(email);
create index if not exists ix_habitflow_users_role on habitflow.users(role);
create index if not exists ix_habitflow_users_account_status on habitflow.users(account_status);
create index if not exists ix_habitflow_users_plan on habitflow.users(plan);
create index if not exists ix_users_created_at on habitflow.users(created_at);
create index if not exists ix_habitflow_habits_user_id on habitflow.habits(user_id);
create index if not exists ix_habitflow_habit_completions_user_id on habitflow.habit_completions(user_id);
create index if not exists ix_habit_completions_habit_id on habitflow.habit_completions(habit_id);
create index if not exists ix_habit_completions_completed_date on habitflow.habit_completions(completed_date);
create index if not exists ix_habitflow_support_tickets_user_id on habitflow.support_tickets(user_id);
create index if not exists ix_habitflow_lgpd_requests_user_id on habitflow.lgpd_requests(user_id);
create index if not exists ix_habitflow_system_audit_logs_created_at on habitflow.system_audit_logs(created_at);
create index if not exists ix_system_audit_logs_severity on habitflow.system_audit_logs(severity);
create index if not exists ix_habitflow_admin_audit_logs_created_at on habitflow.admin_audit_logs(created_at);

insert into habitflow.system_settings(key, value, updated_at)
values
    ('companyName', '"MNSOFT"', now()),
    ('companyLegalName', '"MNSOLUÇÕES TECNOLÓGICAS & CONSULTORIA LTDA"', now()),
    ('companyCnpj', '"18.160.057/0001-13"', now()),
    ('commercialEmail', '"comercial@mnsoft.com.br"', now()),
    ('supportEmail', '"comercial@mnsoft.com.br"', now()),
    ('whatsappEnabled', 'false', now())
on conflict(key) do nothing;


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

create index if not exists ix_habitflow_users_account_status on habitflow.users(account_status);
create index if not exists ix_users_risk_status on habitflow.users(risk_status);
create index if not exists ix_habitflow_users_plan on habitflow.users(plan);
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
