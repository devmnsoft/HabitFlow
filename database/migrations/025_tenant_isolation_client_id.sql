-- v5.9 tenant isolation: ensure client_id is present on client-owned data.
set search_path to habitflow;

alter table habitflow.users add column if not exists client_id uuid null;
alter table habitflow.habits add column if not exists client_id uuid null;
alter table habitflow.habit_completions add column if not exists client_id uuid null;
alter table habitflow.support_tickets add column if not exists client_id uuid null;
alter table habitflow.support_messages add column if not exists client_id uuid null;
alter table habitflow.notifications add column if not exists client_id uuid null;
alter table habitflow.user_reports add column if not exists client_id uuid null;
alter table habitflow.lgpd_requests add column if not exists client_id uuid null;
alter table habitflow.billing_events add column if not exists client_id uuid null;
alter table habitflow.subscriptions add column if not exists client_id uuid null;
alter table habitflow.payment_transactions add column if not exists client_id uuid null;
alter table habitflow.client_invoices add column if not exists client_id uuid null;
alter table habitflow.client_subscriptions add column if not exists client_id uuid null;

alter table habitflow.clients add column if not exists payment_status varchar(40) not null default 'Current';
alter table habitflow.clients add column if not exists subscription_status varchar(40) not null default 'Active';
alter table habitflow.clients add column if not exists benefits_status varchar(80) not null default 'FreeActive';
alter table habitflow.clients add column if not exists overdue_since date null;
alter table habitflow.clients add column if not exists grace_period_until date null;
alter table habitflow.clients add column if not exists blocked_paid_benefits_at timestamp null;
alter table habitflow.clients add column if not exists blocked_paid_benefits_reason text null;

alter table habitflow.users drop constraint if exists fk_habitflow_users_client_id;
alter table habitflow.users add constraint fk_habitflow_users_client_id foreign key (client_id) references habitflow.clients(id);
alter table habitflow.habits drop constraint if exists fk_habitflow_habits_client_id;
alter table habitflow.habits add constraint fk_habitflow_habits_client_id foreign key (client_id) references habitflow.clients(id);
alter table habitflow.habit_completions drop constraint if exists fk_habitflow_habit_completions_client_id;
alter table habitflow.habit_completions add constraint fk_habitflow_habit_completions_client_id foreign key (client_id) references habitflow.clients(id);

create index if not exists ix_habitflow_users_client_id on habitflow.users(client_id);
create index if not exists ix_habitflow_habits_client_id on habitflow.habits(client_id);
create index if not exists ix_habitflow_habit_completions_client_id on habitflow.habit_completions(client_id);
create index if not exists ix_habitflow_support_tickets_client_id on habitflow.support_tickets(client_id);
create index if not exists ix_habitflow_notifications_client_id on habitflow.notifications(client_id);
create index if not exists ix_habitflow_user_reports_client_id on habitflow.user_reports(client_id);
create index if not exists ix_habitflow_payment_transactions_client_id on habitflow.payment_transactions(client_id);
create index if not exists ix_habitflow_client_invoices_client_id on habitflow.client_invoices(client_id);
create index if not exists ix_habitflow_client_subscriptions_client_id on habitflow.client_subscriptions(client_id);
