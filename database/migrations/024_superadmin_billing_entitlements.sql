set search_path to habitflow;

create table if not exists habitflow.client_subscriptions(
 id uuid primary key, client_id uuid not null references habitflow.clients(id), plan_code varchar(80) not null, status varchar(40) not null, billing_cycle varchar(40) not null, started_at timestamp null, current_period_start date null, current_period_end date null, trial_ends_at date null, canceled_at timestamp null, created_at timestamp not null default now(), updated_at timestamp not null default now());
create table if not exists habitflow.client_invoices(
 id uuid primary key, client_id uuid not null references habitflow.clients(id), subscription_id uuid null references habitflow.client_subscriptions(id), invoice_number varchar(80) unique not null, amount numeric(12,2) not null, currency varchar(10) not null default 'BRL', due_date date not null, status varchar(40) not null default 'Pending', payment_method varchar(40) null, paid_at timestamp null, canceled_at timestamp null, mercado_pago_payment_id varchar(150) null, mercado_pago_preference_id varchar(150) null, checkout_url text null, pix_qr_code text null, boleto_url text null, created_at timestamp not null default now(), updated_at timestamp not null default now());
create table if not exists habitflow.client_entitlement_events(
 id uuid primary key, client_id uuid not null references habitflow.clients(id), action varchar(100) not null, previous_status varchar(80) null, new_status varchar(80) not null, reason text null, created_by_user_id uuid null, created_at timestamp not null default now());
create table if not exists habitflow.superadmin_audit_logs(
 id uuid primary key, super_admin_user_id uuid null, super_admin_email varchar(200) null, action varchar(120) not null, target_type varchar(80) null, target_id uuid null, reason text null, metadata jsonb null, created_at timestamp not null default now());
create index if not exists ix_habitflow_client_subscriptions_client_id on habitflow.client_subscriptions(client_id);
create index if not exists ix_habitflow_client_invoices_client_id on habitflow.client_invoices(client_id);
create index if not exists ix_habitflow_client_invoices_status_due_date on habitflow.client_invoices(status, due_date);
create index if not exists ix_habitflow_superadmin_audit_logs_created_at on habitflow.superadmin_audit_logs(created_at desc);
