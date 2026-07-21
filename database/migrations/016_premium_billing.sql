-- HabitFlow v4.6 Premium Payments Billing Automation
create schema if not exists habitflow;

create table if not exists habitflow.plans (
 id uuid primary key, code varchar(80) not null unique, name varchar(120) not null, description text null,
 price_monthly numeric(12,2) null, price_yearly numeric(12,2) null, currency varchar(10) not null default 'BRL',
 habit_limit integer null, reports_enabled boolean not null default false, advanced_reports_enabled boolean not null default false,
 challenges_enabled boolean not null default false, is_active boolean not null default true, is_public boolean not null default true,
 created_at timestamp not null default now(), updated_at timestamp not null default now()
);
create table if not exists habitflow.subscriptions (
 id uuid primary key, user_id uuid not null references habitflow.users(id) on delete cascade, plan_code varchar(80) not null,
 status varchar(50) not null, billing_cycle varchar(50) null, provider varchar(50) not null,
 provider_customer_id varchar(150) null, provider_subscription_id varchar(150) null, provider_payment_id varchar(150) null,
 checkout_url text null, current_period_start timestamp null, current_period_end timestamp null, trial_ends_at timestamp null,
 canceled_at timestamp null, created_at timestamp not null default now(), updated_at timestamp not null default now(),
 constraint ck_habitflow_subscriptions_status check(status in ('Pending','Active','Trial','PastDue','Canceled','Expired','Failed','Inactive')),
 constraint ck_habitflow_subscriptions_billing_cycle check(billing_cycle is null or billing_cycle in ('Monthly','Yearly')),
 constraint ck_habitflow_subscriptions_provider check(provider in ('MercadoPago','Stripe','Manual','Dev'))
);
create table if not exists habitflow.payment_transactions (
 id uuid primary key, user_id uuid null references habitflow.users(id) on delete set null, subscription_id uuid null references habitflow.subscriptions(id) on delete set null,
 provider varchar(50) not null, provider_payment_id varchar(150) null, provider_preference_id varchar(150) null, event_type varchar(100) null,
 status varchar(80) not null, amount numeric(12,2) null, currency varchar(10) not null default 'BRL', raw_status varchar(100) null,
 sanitized_metadata jsonb null, created_at timestamp not null default now(), updated_at timestamp not null default now(),
 constraint ck_habitflow_payment_transactions_provider check(provider in ('MercadoPago','Stripe','Manual','Dev')),
 constraint ck_habitflow_payment_transactions_status check(status in ('Pending','Approved','Rejected','Canceled','Refunded','Failed','Unknown'))
);
create table if not exists habitflow.payment_webhook_events (
 id uuid primary key, provider varchar(50) not null, event_id varchar(150) null, event_type varchar(100) null, status varchar(80) not null,
 received_at timestamp not null default now(), processed_at timestamp null, user_id uuid null, subscription_id uuid null,
 payment_transaction_id uuid null, sanitized_payload jsonb null, processing_error text null
);
create table if not exists habitflow.payment_audit_logs (
 id uuid primary key, user_id uuid null, subscription_id uuid null, action varchar(100) not null, message text not null,
 severity varchar(50) not null, metadata jsonb null, created_at timestamp not null default now()
);
create index if not exists ix_habitflow_plans_code on habitflow.plans(code);
create index if not exists ix_habitflow_subscriptions_user_id on habitflow.subscriptions(user_id);
create index if not exists ix_habitflow_subscriptions_status on habitflow.subscriptions(status);
create index if not exists ix_habitflow_subscriptions_provider_payment_id on habitflow.subscriptions(provider_payment_id);
create index if not exists ix_habitflow_payment_transactions_user_id on habitflow.payment_transactions(user_id);
create index if not exists ix_habitflow_payment_transactions_provider_payment_id on habitflow.payment_transactions(provider_payment_id);
create index if not exists ix_habitflow_payment_webhook_events_event_id on habitflow.payment_webhook_events(event_id);
create index if not exists ix_habitflow_payment_webhook_events_received_at on habitflow.payment_webhook_events(received_at);

insert into habitflow.plans(id,code,name,description,price_monthly,price_yearly,currency,habit_limit,reports_enabled,advanced_reports_enabled,challenges_enabled,is_active,is_public,created_at,updated_at) values
('00000000-0000-0000-0000-000000000461','free','Gratuito','Plano gratuito com até 5 hábitos ativos.',0,0,'BRL',5,true,false,false,true,true,now(),now()),
('00000000-0000-0000-0000-000000000462','premium_monthly','Premium Mensal','Hábitos ilimitados, relatórios avançados e recursos premium.',14.90,null,'BRL',null,true,true,true,true,true,now(),now()),
('00000000-0000-0000-0000-000000000463','premium_yearly','Premium Anual','Plano anual com melhor custo-benefício.',null,99.00,'BRL',null,true,true,true,true,true,now(),now())
on conflict(code) do update set name=excluded.name, description=excluded.description, price_monthly=excluded.price_monthly, price_yearly=excluded.price_yearly, currency=excluded.currency, habit_limit=excluded.habit_limit, reports_enabled=excluded.reports_enabled, advanced_reports_enabled=excluded.advanced_reports_enabled, challenges_enabled=excluded.challenges_enabled, is_active=excluded.is_active, is_public=excluded.is_public, updated_at=now();
