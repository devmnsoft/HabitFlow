-- HabitFlow v6.18.2: assistant audit, safety and aggregate usage. Additive and rerunnable.
begin;
create table if not exists habitflow.assistant_events (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid references habitflow.users(id),
 event_code varchar(80) not null, correlation_id varchar(100) not null, status varchar(30) not null,
 provider varchar(40) not null, duration_ms integer not null default 0, safe_metadata jsonb not null default '{}', created_at timestamptz not null default now());
create index if not exists ix_assistant_events_tenant_date on habitflow.assistant_events(client_id,created_at desc,status);
create index if not exists ix_assistant_events_user_date on habitflow.assistant_events(client_id,user_id,created_at desc);

create table if not exists habitflow.assistant_safety_incidents (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid references habitflow.users(id),
 event_id uuid references habitflow.assistant_events(id) on delete set null, category varchar(50) not null,
 input_hash varchar(64) not null, review_status varchar(20) not null default 'Pending', created_at timestamptz not null default now());
create index if not exists ix_assistant_safety_review on habitflow.assistant_safety_incidents(client_id,review_status,created_at desc);

create table if not exists habitflow.assistant_usage_daily (
 client_id uuid not null references habitflow.clients(id), usage_date date not null, provider varchar(40) not null,
 request_count integer not null default 0, blocked_count integer not null default 0, failure_count integer not null default 0,
 updated_at timestamptz not null default now(), primary key(client_id,usage_date,provider));
commit;
