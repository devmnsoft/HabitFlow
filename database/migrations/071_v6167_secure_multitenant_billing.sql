-- HabitFlow v6.16.7 - secure, idempotent, multi-tenant billing ledger.
begin;

create table if not exists habitflow.billing_customers (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 provider varchar(50) not null, provider_customer_id varchar(180) not null, email_hash varchar(64), status varchar(40) not null,
 created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 unique(provider,provider_customer_id), unique(client_id,user_id,provider));

create table if not exists habitflow.billing_subscriptions (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 plan_code varchar(80) not null references habitflow.plans(code), provider varchar(50) not null, provider_subscription_id varchar(180),
 status varchar(40) not null, billing_cycle varchar(20) not null, current_period_start timestamptz, current_period_end timestamptz,
 cancel_at_period_end boolean not null default false, grace_until timestamptz, created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 unique(provider,provider_subscription_id));

create table if not exists habitflow.billing_checkout_sessions (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 plan_code varchar(80) not null references habitflow.plans(code), billing_cycle varchar(20) not null, provider varchar(50) not null,
 provider_session_id varchar(180) not null, status varchar(40) not null, checkout_url text not null, expires_at timestamptz,
 created_at timestamptz not null default now(), completed_at timestamptz, unique(provider,provider_session_id));

create table if not exists habitflow.billing_invoices (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 subscription_id uuid references habitflow.billing_subscriptions(id), provider varchar(50) not null, provider_invoice_id varchar(180) not null,
 status varchar(40) not null, amount numeric(12,2) not null, currency varchar(10) not null default 'BRL', hosted_receipt_url text,
 due_at timestamptz, paid_at timestamptz, created_at timestamptz not null default now(), unique(provider,provider_invoice_id));

create table if not exists habitflow.billing_payments (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 subscription_id uuid references habitflow.billing_subscriptions(id), invoice_id uuid references habitflow.billing_invoices(id),
 provider varchar(50) not null, provider_payment_id varchar(180) not null, status varchar(40) not null,
 amount numeric(12,2) not null, currency varchar(10) not null default 'BRL', created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 unique(provider,provider_payment_id));

create table if not exists habitflow.billing_webhook_events (
 id uuid primary key, provider varchar(50) not null, provider_event_id varchar(180) not null, event_type varchar(100) not null,
 payload_hash varchar(64) not null, status varchar(40) not null, received_at timestamptz not null default now(), processed_at timestamptz,
 error_code varchar(100), attempt_count integer not null default 1, unique(provider,provider_event_id));

create table if not exists habitflow.billing_audit_events (
 id uuid primary key, client_id uuid references habitflow.clients(id), user_id uuid references habitflow.users(id), actor_user_id uuid references habitflow.users(id),
 action varchar(100) not null, reason text not null, metadata jsonb not null default '{}'::jsonb, created_at timestamptz not null default now());

alter table habitflow.payment_webhook_events add column if not exists payload_hash varchar(64);
alter table habitflow.payment_webhook_events add column if not exists attempt_count integer not null default 1;
update habitflow.payment_webhook_events set event_id=id::text where event_id is null;
delete from habitflow.payment_webhook_events newer using habitflow.payment_webhook_events older
where newer.provider=older.provider and newer.event_id=older.event_id and newer.received_at>older.received_at;
create unique index if not exists ux_payment_webhooks_provider_event on habitflow.payment_webhook_events(provider,event_id);

create index if not exists ix_billing_customers_tenant_user on habitflow.billing_customers(client_id,user_id,status);
create index if not exists ix_billing_subscriptions_tenant_user on habitflow.billing_subscriptions(client_id,user_id,status);
create index if not exists ix_billing_checkout_tenant_user on habitflow.billing_checkout_sessions(client_id,user_id,status);
create index if not exists ix_billing_invoices_tenant_user on habitflow.billing_invoices(client_id,user_id,status);
create index if not exists ix_billing_payments_tenant_user on habitflow.billing_payments(client_id,user_id,status);
create index if not exists ix_billing_webhooks_status on habitflow.billing_webhook_events(status,received_at);
create index if not exists ix_billing_audit_tenant_user on habitflow.billing_audit_events(client_id,user_id,created_at desc);
commit;
