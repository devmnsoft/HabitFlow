begin;
create schema if not exists habitflow;
create table if not exists habitflow.structured_log_events(
 id uuid primary key default gen_random_uuid(), client_id uuid null references habitflow.clients(id) on delete set null,
 tenant_id uuid null references habitflow.clients(id) on delete set null, user_id uuid null references habitflow.users(id) on delete set null,
 severity varchar(16) not null check(severity in('Debug','Info','Warning','Error','Critical')), event_name varchar(160) not null,
 module varchar(80) not null, correlation_id varchar(100) not null, message varchar(1000) not null, details jsonb not null default '{}'::jsonb,
 created_at timestamptz not null default now());
create table if not exists habitflow.operational_alerts(
 id uuid primary key default gen_random_uuid(), client_id uuid null references habitflow.clients(id) on delete cascade,
 tenant_id uuid null references habitflow.clients(id) on delete cascade, user_id uuid null references habitflow.users(id) on delete set null,
 type varchar(80) not null, severity varchar(16) not null check(severity in('Info','Warning','Critical')), title varchar(240) not null,
 deduplication_key varchar(240) not null, occurrences integer not null default 1 check(occurrences>0), status varchar(16) not null default 'Active' check(status in('Active','Resolved')),
 first_occurred_at timestamptz not null default now(), last_occurred_at timestamptz not null default now(), resolved_at timestamptz null,
 resolved_by uuid null references habitflow.users(id) on delete set null, created_at timestamptz not null default now(), updated_at timestamptz not null default now());
create unique index if not exists ux_operational_alert_active_dedup on habitflow.operational_alerts(deduplication_key) where status='Active';
create index if not exists ix_operational_alert_tenant_status_date on habitflow.operational_alerts(tenant_id,status,last_occurred_at desc);
create index if not exists ix_operational_alert_severity_status on habitflow.operational_alerts(severity,status,last_occurred_at desc);
create index if not exists ix_structured_log_tenant_date on habitflow.structured_log_events(tenant_id,created_at desc);
create index if not exists ix_structured_log_severity_date on habitflow.structured_log_events(severity,created_at desc);
create index if not exists ix_structured_log_correlation on habitflow.structured_log_events(correlation_id);
create table if not exists habitflow.operational_alert_history(id uuid primary key default gen_random_uuid(),alert_id uuid not null references habitflow.operational_alerts(id) on delete cascade,tenant_id uuid null references habitflow.clients(id) on delete set null,user_id uuid null references habitflow.users(id) on delete set null,action varchar(32) not null,occurred_at timestamptz not null default now());
create index if not exists ix_operational_alert_history_tenant_date on habitflow.operational_alert_history(tenant_id,occurred_at desc);
create table if not exists habitflow.system_health_history(id uuid primary key default gen_random_uuid(),client_id uuid null references habitflow.clients(id),tenant_id uuid null references habitflow.clients(id),user_id uuid null references habitflow.users(id),check_name varchar(100) not null,status varchar(20) not null,severity varchar(16) not null,message text not null,checked_at timestamptz not null default now());
create index if not exists ix_system_health_status_date on habitflow.system_health_history(status,checked_at desc);
commit;
