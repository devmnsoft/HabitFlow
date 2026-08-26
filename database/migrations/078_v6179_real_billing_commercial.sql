-- HabitFlow v6.17.9 - additive commercial billing governance.
-- Provider secrets and card data intentionally do not belong in this schema.
begin;

alter table habitflow.billing_subscriptions add column if not exists trial_ends_at timestamptz;
alter table habitflow.billing_subscriptions add column if not exists canceled_at timestamptz;
alter table habitflow.billing_subscriptions add column if not exists external_reference varchar(180);
alter table habitflow.billing_subscriptions add column if not exists amount numeric(12,2);
alter table habitflow.billing_subscriptions add column if not exists currency varchar(10) not null default 'BRL';
do $$ begin
 if not exists (select 1 from pg_constraint where conname='ck_billing_subscription_status_v6179') then
  alter table habitflow.billing_subscriptions add constraint ck_billing_subscription_status_v6179
   check (status in ('Free','Trialing','Active','PastDue','Canceled','Expired','PaymentPending','ManualReview','Pending','Trial','Failed','Inactive')) not valid;
 end if;
end $$;

create table if not exists habitflow.billing_manual_adjustments (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 subscription_id uuid references habitflow.billing_subscriptions(id), actor_user_id uuid not null references habitflow.users(id),
 previous_status varchar(40), new_status varchar(40) not null, reason text not null check(length(trim(reason)) >= 10),
 correlation_id uuid not null, created_at timestamptz not null default now());

create table if not exists habitflow.billing_entitlement_usage (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 plan_code varchar(80) not null references habitflow.plans(code), entitlement_code varchar(100) not null,
 used_quantity integer not null default 0 check(used_quantity >= 0), limit_quantity integer,
 period_start timestamptz not null, period_end timestamptz not null, updated_at timestamptz not null default now(),
 unique(client_id,user_id,entitlement_code,period_start), check(period_end > period_start));

create table if not exists habitflow.billing_event_log (
 id uuid primary key, client_id uuid references habitflow.clients(id), user_id uuid references habitflow.users(id),
 event_code varchar(100) not null, correlation_id uuid not null, status varchar(40) not null,
 provider varchar(50), plan_code varchar(80), sanitized_metadata jsonb not null default '{}'::jsonb,
 created_at timestamptz not null default now(),
 check(event_code in ('billing.plan.viewed','billing.checkout.started','billing.checkout.unavailable','billing.payment.approved',
 'billing.payment.pending','billing.payment.failed','billing.subscription.created','billing.subscription.updated',
 'billing.subscription.canceled','billing.webhook.received','billing.webhook.ignored_duplicate',
 'billing.entitlement.blocked','billing.manual_adjustment.created')));

create index if not exists ix_billing_subscriptions_status_period_v6179 on habitflow.billing_subscriptions(status,current_period_end);
create index if not exists ix_billing_subscriptions_provider_v6179 on habitflow.billing_subscriptions(provider,provider_subscription_id);
create index if not exists ix_billing_adjustments_tenant_created_v6179 on habitflow.billing_manual_adjustments(client_id,created_at desc);
create index if not exists ix_billing_usage_tenant_user_v6179 on habitflow.billing_entitlement_usage(client_id,user_id,period_end);
create index if not exists ix_billing_events_code_created_v6179 on habitflow.billing_event_log(event_code,created_at desc);

commit;
