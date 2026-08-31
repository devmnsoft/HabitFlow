begin;

create table if not exists habitflow.api_keys (
 id uuid primary key, client_id uuid not null, user_id uuid not null, name varchar(80) not null,
 key_prefix varchar(20) not null, key_hash char(64) not null unique, scopes text[] not null,
 created_at timestamptz not null default now(), last_used_at timestamptz, revoked_at timestamptz
);
create index if not exists ix_api_keys_tenant_user on habitflow.api_keys(client_id,user_id,created_at desc);

create table if not exists habitflow.integration_webhooks (
 id uuid primary key, client_id uuid not null, user_id uuid not null, name varchar(80) not null,
 url text not null check (url like 'https://%'), events text[] not null, secret_ciphertext text not null,
 enabled boolean not null default true, created_at timestamptz not null default now(), last_success_at timestamptz
);
create index if not exists ix_webhooks_tenant_user on habitflow.integration_webhooks(client_id,user_id,enabled);
create table if not exists habitflow.webhook_delivery_attempts (
 id uuid primary key, webhook_id uuid not null references habitflow.integration_webhooks(id), client_id uuid not null,
 event_id uuid not null, event_name varchar(80) not null, attempt smallint not null default 1,
 status varchar(24) not null, response_code integer, next_attempt_at timestamptz, created_at timestamptz not null default now(),
 unique(webhook_id,event_id,attempt)
);
create index if not exists ix_webhook_attempt_status on habitflow.webhook_delivery_attempts(client_id,status,next_attempt_at);

create table if not exists habitflow.calendar_feeds (
 id uuid primary key, client_id uuid not null, user_id uuid not null, token_hash char(64) not null unique,
 enabled boolean not null default false, include_habits boolean not null default true, include_routines boolean not null default false,
 created_at timestamptz not null default now(), last_used_at timestamptz, unique(client_id,user_id)
);
create table if not exists habitflow.integration_events (
 id uuid primary key, client_id uuid not null, user_id uuid not null, event_name varchar(100) not null,
 metadata jsonb not null default '{}', created_at timestamptz not null default now()
);
create index if not exists ix_integration_events_tenant on habitflow.integration_events(client_id,user_id,event_name,created_at desc);

create table if not exists habitflow.import_jobs (
 id uuid primary key, client_id uuid not null, user_id uuid not null, format varchar(8) not null,
 status varchar(24) not null, preview jsonb, row_count integer not null default 0, created_at timestamptz not null default now(), completed_at timestamptz
);
create table if not exists habitflow.export_jobs (like habitflow.import_jobs including all);
create index if not exists ix_import_jobs_tenant_status on habitflow.import_jobs(client_id,user_id,status,created_at desc);
create index if not exists ix_export_jobs_tenant_status on habitflow.export_jobs(client_id,user_id,status,created_at desc);

commit;
