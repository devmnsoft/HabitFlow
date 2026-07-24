create schema if not exists habitflow;
create extension if not exists pgcrypto;

create table if not exists habitflow.schema_migrations (
    id varchar(120) primary key,
    name varchar(200) not null,
    applied_at timestamp not null default now(),
    checksum varchar(200) null
);

alter table if exists habitflow.users drop constraint if exists ck_habitflow_users_role;
alter table if exists habitflow.users add constraint ck_habitflow_users_role check (role in ('User','Admin','SuperAdmin'));

create table if not exists habitflow.client_onboarding (
    id uuid primary key default gen_random_uuid(), client_id uuid not null unique references habitflow.clients(id) on delete cascade,
    company_data_completed boolean not null default false, billing_data_completed boolean not null default false,
    first_user_invited boolean not null default false, first_habit_created boolean not null default false,
    plan_reviewed boolean not null default false, completed boolean not null default false, completed_at timestamp null,
    created_at timestamp not null default now(), updated_at timestamp not null default now()
);

create table if not exists habitflow.billing_communication_rules (
    id uuid primary key default gen_random_uuid(), code varchar(80) not null unique, name varchar(160) not null,
    trigger_type varchar(80) not null, days_offset integer not null default 0, channel varchar(40) not null default 'Internal',
    title varchar(200) not null, message_template text not null, is_active boolean not null default true,
    created_at timestamp not null default now(), updated_at timestamp not null default now()
);

create table if not exists habitflow.client_communications (
    id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id) on delete cascade,
    user_id uuid null references habitflow.users(id) on delete set null, invoice_id uuid null,
    type varchar(80) not null, channel varchar(40) not null default 'Internal', title varchar(200) not null,
    message text not null, status varchar(40) not null default 'Sent', sent_at timestamp null, read_at timestamp null,
    created_at timestamp not null default now()
);

create table if not exists habitflow.job_execution_logs (
    id uuid primary key default gen_random_uuid(), job_name varchar(120) not null, status varchar(40) not null,
    started_at timestamp not null default now(), finished_at timestamp null, duration_ms bigint null,
    processed_count integer not null default 0, error_message text null, created_at timestamp not null default now()
);

create table if not exists habitflow.client_invoices (
    id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id) on delete cascade,
    subscription_id uuid null, invoice_number varchar(80) null, amount numeric(12,2) not null default 0,
    due_date date not null, payment_method varchar(40) not null default 'Manual', status varchar(40) not null default 'Pending',
    paid_at timestamp null, checkout_url text null, provider_payment_id varchar(160) null, created_at timestamp not null default now(), updated_at timestamp not null default now()
);

create table if not exists habitflow.client_subscriptions (
    id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id) on delete cascade,
    plan_code varchar(80) not null, status varchar(40) not null default 'Pending', billing_cycle varchar(40) null,
    current_period_start timestamp null, current_period_end timestamp null, trial_ends_at timestamp null, canceled_at timestamp null,
    created_at timestamp not null default now(), updated_at timestamp not null default now()
);

create table if not exists habitflow.client_entitlement_events (
    id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id) on delete cascade,
    event_type varchar(80) not null, reason text null, created_at timestamp not null default now()
);

create table if not exists habitflow.superadmin_audit_logs (
    id uuid primary key default gen_random_uuid(), actor_user_id uuid null, actor_email varchar(200) null,
    action varchar(120) not null, target_type varchar(80) not null, target_id uuid null, reason text null,
    metadata jsonb not null default '{}'::jsonb, created_at timestamp not null default now()
);

alter table if exists habitflow.support_tickets add column if not exists sla_due_at timestamp null;
alter table if exists habitflow.support_tickets add column if not exists first_response_at timestamp null;
alter table if exists habitflow.support_tickets add column if not exists resolved_at timestamp null;
alter table if exists habitflow.clients add column if not exists billing_email varchar(200) null;
alter table if exists habitflow.clients add column if not exists payment_status varchar(40) not null default 'None';
alter table if exists habitflow.clients add column if not exists benefits_status varchar(40) not null default 'Free';
alter table if exists habitflow.clients add column if not exists subscription_status varchar(40) not null default 'Free';
alter table if exists habitflow.clients add column if not exists last_payment_at timestamp null;
alter table if exists habitflow.clients add column if not exists next_due_date date null;
alter table if exists habitflow.clients add column if not exists overdue_since date null;
alter table if exists habitflow.clients add column if not exists grace_period_until date null;

create index if not exists ix_client_communications_client_invoice_type_channel on habitflow.client_communications(client_id, invoice_id, type, channel);
create index if not exists ix_client_invoices_client_status_due on habitflow.client_invoices(client_id, status, due_date);
create index if not exists ix_client_subscriptions_client_status on habitflow.client_subscriptions(client_id, status);
create index if not exists ix_superadmin_audit_logs_created on habitflow.superadmin_audit_logs(created_at desc);
create index if not exists ix_job_execution_logs_job_started on habitflow.job_execution_logs(job_name, started_at desc);

insert into habitflow.schema_migrations(id,name) values ('029','operational_completeness_v61') on conflict (id) do update set name=excluded.name, applied_at=now();
