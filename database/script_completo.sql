\set ON_ERROR_STOP on
-- HabitFlow v6.8.2.1-DatabaseBootstrapParity-PlanSeedIntegrity
-- BOOTSTRAP EXCLUSIVO PARA BANCO NOVO. Bancos existentes usam
-- scripts/database/run-migrations.sh. Nunca execute os dois no mesmo banco.
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
    description text null,
    is_public boolean not null default false,
    created_at timestamp not null default now(),
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
-- HabitFlow v4.6 Premium Payments Billing Automation
create schema if not exists habitflow;

create table if not exists habitflow.plans (
 id uuid primary key, code varchar(80) not null unique, name varchar(120) not null, public_name varchar(120) not null,
 headline varchar(200) null, audience_text text null, badge_text varchar(100) null, description text null,
 price_monthly numeric(12,2) null, price_yearly numeric(12,2) null, currency varchar(10) not null default 'BRL',
 habit_limit integer null, reports_enabled boolean not null default false, advanced_reports_enabled boolean not null default false,
 challenges_enabled boolean not null default false, is_active boolean not null default true, is_public boolean not null default true,
 is_featured boolean not null default false, sort_order integer not null default 0,
 created_by_user_id uuid null, updated_by_user_id uuid null,
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

-- Keep the first seed compatible with the final plans contract. This block is
-- intentionally before any INSERT so a partially-created bootstrap can recover.
alter table habitflow.plans add column if not exists public_name varchar(120);
alter table habitflow.plans add column if not exists headline varchar(200);
alter table habitflow.plans add column if not exists audience_text text;
alter table habitflow.plans add column if not exists badge_text varchar(100);
alter table habitflow.plans add column if not exists is_featured boolean not null default false;
alter table habitflow.plans add column if not exists sort_order integer not null default 0;
alter table habitflow.plans add column if not exists created_by_user_id uuid null;
alter table habitflow.plans add column if not exists updated_by_user_id uuid null;
update habitflow.plans
set public_name = name
where public_name is null or btrim(public_name) = '';
alter table habitflow.plans alter column public_name set not null;

insert into habitflow.plans(id,code,name,public_name,headline,description,price_monthly,price_yearly,currency,habit_limit,reports_enabled,advanced_reports_enabled,challenges_enabled,is_active,is_public,is_featured,sort_order,created_at,updated_at) values
('00000000-0000-0000-0000-000000000461','free','Gratuito','Gratuito',null,'Plano gratuito com até 5 hábitos ativos.',0,0,'BRL',5,true,false,false,true,true,false,10,now(),now()),
('00000000-0000-0000-0000-000000000462','premium_monthly','Premium Mensal','Premium Mensal',null,'Hábitos ilimitados, relatórios avançados e recursos premium.',14.90,null,'BRL',null,true,true,true,true,true,false,20,now(),now()),
('00000000-0000-0000-0000-000000000463','premium_yearly','Premium Anual','Premium Anual',null,'Plano anual com melhor custo-benefício.',null,99.00,'BRL',null,true,true,true,true,true,false,30,now(),now())
on conflict(code) do update set
 name=excluded.name, public_name=excluded.public_name, headline=excluded.headline,
 description=excluded.description, price_monthly=excluded.price_monthly,
 price_yearly=excluded.price_yearly, currency=excluded.currency,
 habit_limit=excluded.habit_limit, reports_enabled=excluded.reports_enabled,
 advanced_reports_enabled=excluded.advanced_reports_enabled,
 challenges_enabled=excluded.challenges_enabled, is_active=excluded.is_active,
 is_public=excluded.is_public, is_featured=excluded.is_featured,
 sort_order=excluded.sort_order, updated_at=now();
-- v4.9 Guided Journey + Habit Library
create table if not exists habitflow.habit_objectives (
    id uuid primary key,
    slug varchar(80) not null unique,
    name varchar(120) not null,
    description text not null,
    icon varchar(80) null,
    sort_order integer not null default 0,
    is_active boolean not null default true,
    created_at timestamp not null default now()
);

create table if not exists habitflow.habit_templates (
    id uuid primary key,
    objective_id uuid not null references habitflow.habit_objectives(id) on delete cascade,
    name varchar(120) not null,
    description text not null,
    category varchar(80) not null,
    suggested_frequency varchar(50) not null default 'Daily',
    suggested_color varchar(20) not null default '#10B981',
    difficulty varchar(50) not null default 'Easy',
    estimated_time_minutes integer null,
    benefit_text text null,
    sort_order integer not null default 0,
    is_active boolean not null default true,
    created_at timestamp not null default now(),
    updated_at timestamp not null default now(),
    constraint ck_habitflow_habit_templates_frequency check (suggested_frequency in ('Daily','Weekdays','Weekends','CustomWeekly')),
    constraint ck_habitflow_habit_templates_difficulty check (difficulty in ('Easy','Medium','Hard')),
    constraint uq_habitflow_habit_templates_objective_name unique(objective_id, name)
);

create index if not exists ix_habitflow_habit_objectives_slug on habitflow.habit_objectives(slug);
create index if not exists ix_habitflow_habit_templates_objective_id on habitflow.habit_templates(objective_id);
create index if not exists ix_habitflow_habit_templates_category on habitflow.habit_templates(category);
create index if not exists ix_habitflow_habit_templates_is_active on habitflow.habit_templates(is_active);

insert into habitflow.habit_objectives(id, slug, name, description, icon, sort_order, is_active) values
('10000000-0000-0000-0000-000000000001','saude','Saúde','Hábitos simples para cuidar do corpo e ter mais energia.','♥',1,true),
('10000000-0000-0000-0000-000000000002','estudos','Estudos','Rotinas curtas para aprender com consistência.','✦',2,true),
('10000000-0000-0000-0000-000000000003','produtividade','Produtividade','Ações práticas para organizar prioridades e finalizar tarefas.','→',3,true),
('10000000-0000-0000-0000-000000000004','bem-estar','Bem-estar','Pausas e cuidados para reduzir tensão e melhorar o dia.','☼',4,true),
('10000000-0000-0000-0000-000000000005','organizacao','Organização','Pequenos hábitos para deixar sua rotina mais leve.','▣',5,true),
('10000000-0000-0000-0000-000000000006','sono','Sono','Rituais para noites mais consistentes e restauradoras.','☾',6,true),
('10000000-0000-0000-0000-000000000007','exercicio','Exercício','Movimentos simples para ganhar disposição.','✓',7,true),
('10000000-0000-0000-0000-000000000008','leitura','Leitura','Leitura leve e constante para evoluir todos os dias.','◇',8,true)
on conflict(slug) do update set name=excluded.name, description=excluded.description, icon=excluded.icon, sort_order=excluded.sort_order, is_active=excluded.is_active;

with data(slug,name,description,category,suggested_color,difficulty,estimated_time_minutes,benefit_text,sort_order) as (values
('saude','Beber água','Sugestão prática: beber água com constância e leveza.','Saúde','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('saude','Comer uma fruta','Sugestão prática: comer uma fruta com constância e leveza.','Saúde','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('saude','Alongar por 5 minutos','Sugestão prática: alongar por 5 minutos com constância e leveza.','Saúde','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('saude','Caminhar 20 minutos','Sugestão prática: caminhar 20 minutos com constância e leveza.','Saúde','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('saude','Evitar refrigerante','Sugestão prática: evitar refrigerante com constância e leveza.','Saúde','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('estudos','Estudar 30 minutos','Sugestão prática: estudar 30 minutos com constância e leveza.','Estudos','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('estudos','Revisar anotações','Sugestão prática: revisar anotações com constância e leveza.','Estudos','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('estudos','Ler 10 páginas','Sugestão prática: ler 10 páginas com constância e leveza.','Estudos','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('estudos','Resolver exercícios','Sugestão prática: resolver exercícios com constância e leveza.','Estudos','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('estudos','Organizar material','Sugestão prática: organizar material com constância e leveza.','Estudos','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('produtividade','Planejar o dia','Sugestão prática: planejar o dia com constância e leveza.','Produtividade','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('produtividade','Revisar prioridades','Sugestão prática: revisar prioridades com constância e leveza.','Produtividade','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('produtividade','Evitar celular por 30 minutos','Sugestão prática: evitar celular por 30 minutos com constância e leveza.','Produtividade','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('produtividade','Finalizar uma pendência','Sugestão prática: finalizar uma pendência com constância e leveza.','Produtividade','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('produtividade','Organizar tarefas','Sugestão prática: organizar tarefas com constância e leveza.','Produtividade','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('bem-estar','Meditar 5 minutos','Sugestão prática: meditar 5 minutos com constância e leveza.','Bem-estar','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('bem-estar','Respirar profundamente','Sugestão prática: respirar profundamente com constância e leveza.','Bem-estar','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('bem-estar','Escrever gratidão','Sugestão prática: escrever gratidão com constância e leveza.','Bem-estar','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('bem-estar','Fazer pausa consciente','Sugestão prática: fazer pausa consciente com constância e leveza.','Bem-estar','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('bem-estar','Ouvir música relaxante','Sugestão prática: ouvir música relaxante com constância e leveza.','Bem-estar','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('organizacao','Arrumar a cama','Sugestão prática: arrumar a cama com constância e leveza.','Organização','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('organizacao','Organizar mesa','Sugestão prática: organizar mesa com constância e leveza.','Organização','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('organizacao','Revisar agenda','Sugestão prática: revisar agenda com constância e leveza.','Organização','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('organizacao','Separar roupa do dia seguinte','Sugestão prática: separar roupa do dia seguinte com constância e leveza.','Organização','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('organizacao','Limpar caixa de entrada','Sugestão prática: limpar caixa de entrada com constância e leveza.','Organização','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('sono','Dormir antes das 23h','Sugestão prática: dormir antes das 23h com constância e leveza.','Sono','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('sono','Evitar telas antes de dormir','Sugestão prática: evitar telas antes de dormir com constância e leveza.','Sono','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('sono','Preparar ambiente','Sugestão prática: preparar ambiente com constância e leveza.','Sono','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('sono','Fazer rotina noturna','Sugestão prática: fazer rotina noturna com constância e leveza.','Sono','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('sono','Acordar no mesmo horário','Sugestão prática: acordar no mesmo horário com constância e leveza.','Sono','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('exercicio','Caminhar','Sugestão prática: caminhar com constância e leveza.','Exercício','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('exercicio','Fazer 10 flexões','Sugestão prática: fazer 10 flexões com constância e leveza.','Exercício','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('exercicio','Alongar','Sugestão prática: alongar com constância e leveza.','Exercício','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('exercicio','Subir escadas','Sugestão prática: subir escadas com constância e leveza.','Exercício','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('exercicio','Treino leve','Sugestão prática: treino leve com constância e leveza.','Exercício','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5),
('leitura','Ler 10 páginas','Sugestão prática: ler 10 páginas com constância e leveza.','Leitura','#10B981','Easy',5,'Ajuda a construir uma rotina sustentável.',1),
('leitura','Ler 15 minutos','Sugestão prática: ler 15 minutos com constância e leveza.','Leitura','#0EA5E9','Easy',5,'Ajuda a construir uma rotina sustentável.',2),
('leitura','Anotar uma ideia','Sugestão prática: anotar uma ideia com constância e leveza.','Leitura','#8B5CF6','Easy',10,'Ajuda a construir uma rotina sustentável.',3),
('leitura','Revisar leitura anterior','Sugestão prática: revisar leitura anterior com constância e leveza.','Leitura','#F59E0B','Easy',10,'Ajuda a construir uma rotina sustentável.',4),
('leitura','Separar próximo livro','Sugestão prática: separar próximo livro com constância e leveza.','Leitura','#2563EB','Easy',10,'Ajuda a construir uma rotina sustentável.',5)
)
insert into habitflow.habit_templates(id, objective_id, name, description, category, suggested_frequency, suggested_color, difficulty, estimated_time_minutes, benefit_text, sort_order, is_active)
select (substr(md5(o.slug || ':' || d.name),1,8)||'-'||substr(md5(o.slug || ':' || d.name),9,4)||'-'||substr(md5(o.slug || ':' || d.name),13,4)||'-'||substr(md5(o.slug || ':' || d.name),17,4)||'-'||substr(md5(o.slug || ':' || d.name),21,12))::uuid, o.id, d.name, d.description, d.category, 'Daily', d.suggested_color, d.difficulty, d.estimated_time_minutes, d.benefit_text, d.sort_order, true
from data d join habitflow.habit_objectives o on o.slug=d.slug
on conflict(objective_id, name) do update set description=excluded.description, category=excluded.category, suggested_color=excluded.suggested_color, difficulty=excluded.difficulty, estimated_time_minutes=excluded.estimated_time_minutes, benefit_text=excluded.benefit_text, sort_order=excluded.sort_order, is_active=true, updated_at=now();


-- 018_user_ui_preferences_accessibility
create table if not exists habitflow.user_ui_preferences (
  id uuid primary key,
  user_id uuid not null references habitflow.users(id) on delete cascade,
  contrast_mode varchar(50) not null default 'Default',
  font_scale varchar(50) not null default 'Normal',
  reduce_motion boolean not null default false,
  created_at timestamp not null default now(),
  updated_at timestamp not null default now(),
  constraint user_ui_preferences_user_unique unique(user_id),
  constraint user_ui_preferences_contrast_check check (contrast_mode in ('Default','HighContrast')),
  constraint user_ui_preferences_font_check check (font_scale in ('Normal','Large'))
);
create index if not exists ix_user_ui_preferences_user_id on habitflow.user_ui_preferences(user_id);

-- 019_notifications_feedback_preferences
create table if not exists habitflow.notifications(
  id uuid primary key,
  user_id uuid not null references habitflow.users(id) on delete cascade,
  type varchar(80) not null,
  title varchar(160) not null,
  message text not null,
  severity varchar(40) not null default 'Info',
  is_read boolean not null default false,
  action_url text null,
  related_entity_type varchar(80) null,
  related_entity_id uuid null,
  created_at timestamp not null default now(),
  read_at timestamp null
);
alter table habitflow.notifications add column if not exists severity varchar(40) not null default 'Info';
alter table habitflow.notifications add column if not exists action_url text null;
alter table habitflow.notifications add column if not exists related_entity_type varchar(80) null;
alter table habitflow.notifications add column if not exists related_entity_id uuid null;
create index if not exists ix_notifications_user_id on habitflow.notifications(user_id);
create index if not exists ix_notifications_unread on habitflow.notifications(user_id, is_read, created_at desc);

-- 020_popup_preferences
alter table habitflow.user_ui_preferences add column if not exists show_achievement_popups boolean not null default true;
alter table habitflow.user_ui_preferences add column if not exists show_tip_popups boolean not null default true;
alter table habitflow.user_ui_preferences add column if not exists enable_toasts boolean not null default true;
alter table habitflow.user_ui_preferences add column if not exists reduce_popups boolean not null default false;
create table if not exists habitflow.clients (
    id uuid primary key,
    name varchar(180) not null,
    legal_name varchar(220) null,
    document varchar(30) null,
    email varchar(200) null,
    phone varchar(40) null,
    contact_name varchar(160) null,
    plan varchar(80) not null default 'Free',
    status varchar(80) not null default 'Active',
    notes text null,
    is_active boolean not null default true,
    created_at timestamp not null default now(),
    updated_at timestamp not null default now(),
    constraint ck_habitflow_clients_status check (status in ('Active', 'Inactive', 'Blocked')),
    constraint ck_habitflow_clients_plan check (plan in ('Free', 'Premium', 'Enterprise'))
);
create unique index if not exists ux_habitflow_clients_document_not_empty on habitflow.clients(document) where document is not null and btrim(document) <> '';
create index if not exists ix_habitflow_clients_name on habitflow.clients(name);
create index if not exists ix_habitflow_clients_email on habitflow.clients(email);
create index if not exists ix_habitflow_clients_document on habitflow.clients(document);
create index if not exists ix_habitflow_clients_status on habitflow.clients(status);
create index if not exists ix_habitflow_clients_created_at on habitflow.clients(created_at);
alter table habitflow.users add column if not exists client_id uuid null references habitflow.clients(id);
create index if not exists ix_habitflow_users_client_id on habitflow.users(client_id);
set search_path to habitflow;

alter table habitflow.clients add column if not exists person_type varchar(20) not null default 'LegalPerson';
alter table habitflow.clients add column if not exists document_type varchar(10) not null default 'CNPJ';
alter table habitflow.clients add column if not exists document_raw varchar(30);
alter table habitflow.clients add column if not exists document_normalized varchar(20);
alter table habitflow.clients add column if not exists trade_name varchar(180);
alter table habitflow.clients add column if not exists state_registration varchar(40);
alter table habitflow.clients add column if not exists municipal_registration varchar(40);
alter table habitflow.clients add column if not exists billing_email varchar(200);
alter table habitflow.clients add column if not exists billing_phone varchar(40);
alter table habitflow.clients add column if not exists billing_responsible_name varchar(160);
alter table habitflow.clients add column if not exists address_zipcode varchar(20);
alter table habitflow.clients add column if not exists address_street varchar(200);
alter table habitflow.clients add column if not exists address_number varchar(40);
alter table habitflow.clients add column if not exists address_complement varchar(120);
alter table habitflow.clients add column if not exists address_district varchar(120);
alter table habitflow.clients add column if not exists address_city varchar(120);
alter table habitflow.clients add column if not exists address_state varchar(2);
alter table habitflow.clients add column if not exists subscription_status varchar(40) not null default 'Free';
alter table habitflow.clients add column if not exists benefits_status varchar(40) not null default 'Free';
alter table habitflow.clients add column if not exists payment_status varchar(40) not null default 'None';
alter table habitflow.clients add column if not exists last_payment_at timestamp;
alter table habitflow.clients add column if not exists next_due_date date;
alter table habitflow.clients add column if not exists overdue_since date;
alter table habitflow.clients add column if not exists grace_period_until date;
alter table habitflow.clients add column if not exists blocked_paid_benefits_at timestamp;
alter table habitflow.clients add column if not exists blocked_paid_benefits_reason text;

alter table habitflow.clients drop constraint if exists ck_habitflow_clients_person_type;
alter table habitflow.clients add constraint ck_habitflow_clients_person_type check (person_type in ('NaturalPerson','LegalPerson'));
alter table habitflow.clients drop constraint if exists ck_habitflow_clients_document_type;
alter table habitflow.clients add constraint ck_habitflow_clients_document_type check (document_type in ('CPF','CNPJ'));
alter table habitflow.clients drop constraint if exists ck_habitflow_clients_person_document_match;
alter table habitflow.clients add constraint ck_habitflow_clients_person_document_match check ((person_type = 'NaturalPerson' and document_type = 'CPF') or (person_type = 'LegalPerson' and document_type = 'CNPJ'));
alter table habitflow.clients drop constraint if exists ck_habitflow_clients_subscription_status;
alter table habitflow.clients add constraint ck_habitflow_clients_subscription_status check (subscription_status in ('Free','Trial','Active','PastDue','Canceled','Suspended'));
alter table habitflow.clients drop constraint if exists ck_habitflow_clients_benefits_status;
alter table habitflow.clients add constraint ck_habitflow_clients_benefits_status check (benefits_status in ('Free','PremiumActive','PremiumBlocked','EnterpriseActive','EnterpriseBlocked'));
alter table habitflow.clients drop constraint if exists ck_habitflow_clients_payment_status;
alter table habitflow.clients add constraint ck_habitflow_clients_payment_status check (payment_status in ('None','Pending','Approved','Rejected','Canceled','Expired','Overdue','Refunded'));

update habitflow.clients set document_normalized = regexp_replace(coalesce(document,''), '\\D', '', 'g') where document_normalized is null and document is not null;
create unique index if not exists ux_habitflow_clients_document_normalized_not_null on habitflow.clients(document_normalized) where document_normalized is not null;
create index if not exists ix_habitflow_clients_person_type on habitflow.clients(person_type);
create index if not exists ix_habitflow_clients_document_type on habitflow.clients(document_type);
create index if not exists ix_habitflow_clients_document_normalized on habitflow.clients(document_normalized);
create index if not exists ix_habitflow_clients_subscription_status on habitflow.clients(subscription_status);
create index if not exists ix_habitflow_clients_benefits_status on habitflow.clients(benefits_status);
create index if not exists ix_habitflow_clients_payment_status on habitflow.clients(payment_status);
create index if not exists ix_habitflow_clients_next_due_date on habitflow.clients(next_due_date);
create index if not exists ix_habitflow_clients_overdue_since on habitflow.clients(overdue_since);
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

-- v5.9-TenantIsolation-UserInvites-BillingAutomation-SaaSHardening
\i database/migrations/025_tenant_isolation_client_id.sql
\i database/migrations/026_backfill_client_id.sql
\i database/migrations/027_user_invites.sql
create table if not exists habitflow.client_onboarding (
  id uuid primary key,
  client_id uuid not null references habitflow.clients(id),
  company_data_completed boolean not null default false,
  billing_data_completed boolean not null default false,
  first_user_invited boolean not null default false,
  first_habit_created boolean not null default false,
  plan_reviewed boolean not null default false,
  completed boolean not null default false,
  completed_at timestamp null,
  created_at timestamp not null default now(),
  updated_at timestamp not null default now(),
  unique(client_id)
);

create table if not exists habitflow.billing_communication_rules (
  id uuid primary key,
  code varchar(80) not null unique,
  name varchar(160) not null,
  trigger_type varchar(80) not null,
  days_offset integer not null default 0,
  channel varchar(80) not null,
  title varchar(180) not null,
  message_template text not null,
  is_active boolean not null default true,
  created_at timestamp not null default now(),
  updated_at timestamp not null default now()
);

create table if not exists habitflow.client_communications (
  id uuid primary key,
  client_id uuid not null references habitflow.clients(id),
  user_id uuid null references habitflow.users(id),
  invoice_id uuid null,
  type varchar(80) not null,
  channel varchar(80) not null,
  title varchar(180) not null,
  message text not null,
  status varchar(80) not null default 'Created',
  sent_at timestamp null,
  read_at timestamp null,
  created_at timestamp not null default now()
);
create unique index if not exists ux_client_communications_no_duplicate_billing on habitflow.client_communications(client_id, invoice_id, type, channel) where invoice_id is not null and status <> 'Canceled';

create table if not exists habitflow.job_execution_logs (
  id uuid primary key,
  job_name varchar(120) not null,
  status varchar(80) not null,
  started_at timestamp not null,
  finished_at timestamp null,
  duration_ms bigint null,
  processed_count integer not null default 0,
  error_message text null,
  created_at timestamp not null default now()
);

alter table habitflow.support_tickets add column if not exists client_id uuid null references habitflow.clients(id);
alter table habitflow.support_tickets add column if not exists assigned_to_user_id uuid null references habitflow.users(id);
alter table habitflow.support_tickets add column if not exists priority varchar(40) not null default 'Normal';
alter table habitflow.support_tickets add column if not exists sla_due_at timestamp null;
alter table habitflow.support_tickets add column if not exists category varchar(80) not null default 'Dúvida';
alter table habitflow.support_tickets add column if not exists source varchar(80) not null default 'Web';

insert into habitflow.billing_communication_rules(id,code,name,trigger_type,days_offset,channel,title,message_template) values
(gen_random_uuid(),'due_minus_3','Aviso 3 dias antes','BeforeDueDate',-3,'Internal','Seu plano vence em breve','Identificamos uma cobrança com vencimento em {dueDate}. Regularize para manter seus benefícios Premium ativos.'),
(gen_random_uuid(),'due_today','Aviso no vencimento','OnDueDate',0,'Internal','Pagamento pendente','Seu pagamento está pendente. Você ainda pode usar o HabitFlow, mas os benefícios Premium podem ser suspensos após o período de tolerância.'),
(gen_random_uuid(),'due_plus_2','Aviso 2 dias após','AfterDueDate',2,'Internal','Pagamento pendente','Seu pagamento está pendente. Você ainda pode usar o HabitFlow, mas os benefícios Premium podem ser suspensos após o período de tolerância.'),
(gen_random_uuid(),'due_plus_5','Aviso 5 dias após','AfterDueDate',5,'Internal','Pagamento pendente','Seu pagamento está pendente. Você ainda pode usar o HabitFlow, mas os benefícios Premium podem ser suspensos após o período de tolerância.'),
(gen_random_uuid(),'benefits_blocked','Benefícios bloqueados','BenefitsBlocked',0,'Internal','Benefícios Premium suspensos','Os recursos pagos foram temporariamente suspensos. A área gratuita continua disponível.'),
(gen_random_uuid(),'payment_approved','Pagamento aprovado','PaymentApproved',0,'Internal','Pagamento confirmado','Seus benefícios pagos estão ativos novamente.'),
(gen_random_uuid(),'benefits_released','Benefícios liberados','BenefitsReleased',0,'Internal','Benefícios liberados','Seus benefícios pagos estão ativos novamente.'),
(gen_random_uuid(),'engagement_first_habit','Engajamento primeiro hábito','AfterDueDate',0,'Internal','Comece com seu primeiro hábito','Você ainda não criou hábitos. Use a biblioteca para começar em poucos segundos.'),
(gen_random_uuid(),'engagement_free_limit','Engajamento limite Free','AfterDueDate',0,'Internal','Você chegou ao limite gratuito','Considere revisar os hábitos arquivados ou conhecer o Premium.')
on conflict(code) do nothing;

-- v6.1 operational completeness baseline
\i database/migrations/029_operational_completeness_v61.sql

-- v6.1.2 Registration claims, onboarding activation and cadastral quality
\i database/migrations/031_registration_claims_onboarding_quality.sql
\i database/migrations/032_parameterized_plans_rbac_effective_access.sql
\i database/migrations/033_plan_prices_features.sql
\i database/migrations/034_roles_permissions.sql
\i database/migrations/035_effective_plan_payment_restrictions.sql

-- v6.3 personal journey
BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.user_goals (
 id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id),
 objective_slug varchar(100), title varchar(160) NOT NULL, description text, target_type varchar(80) NOT NULL,
 target_value integer NOT NULL CHECK(target_value > 0), current_value integer NOT NULL DEFAULT 0 CHECK(current_value >= 0),
 start_date date NOT NULL, end_date date, status varchar(40) NOT NULL DEFAULT 'Active', color varchar(20), icon varchar(80),
 created_at timestamp NOT NULL DEFAULT now(), updated_at timestamp NOT NULL DEFAULT now(), completed_at timestamp,
 CONSTRAINT ck_user_goals_target_type CHECK(target_type IN ('HabitCompletions','ActiveDays','StreakDays','WeeklyCompletions','Custom')),
 CONSTRAINT ck_user_goals_status CHECK(status IN ('Active','Completed','Paused','Canceled')), CONSTRAINT ck_user_goals_dates CHECK(end_date IS NULL OR end_date >= start_date)
);
CREATE INDEX IF NOT EXISTS ix_user_goals_client_user_status ON habitflow.user_goals(client_id,user_id,status);
CREATE TABLE IF NOT EXISTS habitflow.goal_habits (
 goal_id uuid NOT NULL REFERENCES habitflow.user_goals(id) ON DELETE CASCADE, habit_id uuid NOT NULL REFERENCES habitflow.habits(id) ON DELETE CASCADE,
 created_at timestamp NOT NULL DEFAULT now(), PRIMARY KEY(goal_id,habit_id)
);
COMMIT;

BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.milestones (id uuid PRIMARY KEY, code varchar(80) NOT NULL UNIQUE, title varchar(120) NOT NULL, description varchar(240) NOT NULL, threshold integer, is_active boolean NOT NULL DEFAULT true, created_at timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS habitflow.user_milestones (id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id), milestone_id uuid NOT NULL REFERENCES habitflow.milestones(id), achieved_at timestamp NOT NULL DEFAULT now(), metadata jsonb, UNIQUE(user_id,milestone_id));
CREATE INDEX IF NOT EXISTS ix_user_milestones_client_user ON habitflow.user_milestones(client_id,user_id,achieved_at DESC);
INSERT INTO habitflow.milestones(id,code,title,description,threshold) VALUES
('37100000-0000-0000-0000-000000000001','first_step','Primeiro passo','Você concluiu seu primeiro hábito.',1),
('37100000-0000-0000-0000-000000000003','present_3','3 dias presentes','Você esteve presente por 3 dias.',3),
('37100000-0000-0000-0000-000000000007','rhythm_7','7 dias de ritmo','Você manteve seu ritmo por 7 dias.',7),
('37100000-0000-0000-0000-000000000015','consistency_15','15 dias de constância','Sua constância chegou a 15 dias.',15),
('37100000-0000-0000-0000-000000000030','evolution_30','30 dias de evolução','Você cuidou da sua rotina por 30 dias.',30)
ON CONFLICT(code) DO UPDATE SET title=EXCLUDED.title,description=EXCLUDED.description,threshold=EXCLUDED.threshold;
COMMIT;
BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.habit_reminders (id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id), habit_id uuid NOT NULL REFERENCES habitflow.habits(id) ON DELETE CASCADE, reminder_time time NOT NULL, timezone varchar(80) NOT NULL DEFAULT 'America/Sao_Paulo', days_of_week integer[], is_active boolean NOT NULL DEFAULT true, last_triggered_at timestamp, next_trigger_at timestamp, created_at timestamp NOT NULL DEFAULT now(), updated_at timestamp NOT NULL DEFAULT now(), CHECK(days_of_week IS NULL OR days_of_week <@ ARRAY[0,1,2,3,4,5,6]));
CREATE INDEX IF NOT EXISTS ix_habit_reminders_due ON habitflow.habit_reminders(next_trigger_at) WHERE is_active;
CREATE INDEX IF NOT EXISTS ix_habit_reminders_scope ON habitflow.habit_reminders(client_id,user_id,habit_id);
CREATE TABLE IF NOT EXISTS habitflow.user_summary_preferences (user_id uuid PRIMARY KEY REFERENCES habitflow.users(id) ON DELETE CASCADE, daily_summary_enabled boolean NOT NULL DEFAULT true, daily_summary_time time NOT NULL DEFAULT '20:00', weekly_summary_enabled boolean NOT NULL DEFAULT true, weekly_summary_day integer NOT NULL DEFAULT 0 CHECK(weekly_summary_day BETWEEN 0 AND 6), weekly_summary_time time NOT NULL DEFAULT '18:00', timezone varchar(80) NOT NULL DEFAULT 'America/Sao_Paulo', updated_at timestamp NOT NULL DEFAULT now());
COMMIT;
BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.shared_routines (id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), created_by_user_id uuid NOT NULL REFERENCES habitflow.users(id), name varchar(160) NOT NULL, description text, color varchar(20), status varchar(40) NOT NULL DEFAULT 'Active' CHECK(status IN ('Active','Archived')), created_at timestamp NOT NULL DEFAULT now(), updated_at timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS habitflow.shared_routine_habits (routine_id uuid NOT NULL REFERENCES habitflow.shared_routines(id) ON DELETE CASCADE, habit_template_id uuid REFERENCES habitflow.habit_templates(id), name varchar(160) NOT NULL, category varchar(100), frequency_type varchar(40) NOT NULL, target_per_week integer CHECK(target_per_week BETWEEN 1 AND 7), reminder_time time, sort_order integer NOT NULL DEFAULT 0, PRIMARY KEY(routine_id,name));
CREATE TABLE IF NOT EXISTS habitflow.shared_routine_members (routine_id uuid NOT NULL REFERENCES habitflow.shared_routines(id) ON DELETE CASCADE, user_id uuid NOT NULL REFERENCES habitflow.users(id), role varchar(40) NOT NULL CHECK(role IN ('Owner','Participant','Viewer')), joined_at timestamp NOT NULL DEFAULT now(), left_at timestamp, PRIMARY KEY(routine_id,user_id));
CREATE INDEX IF NOT EXISTS ix_shared_routines_client ON habitflow.shared_routines(client_id,status);
CREATE INDEX IF NOT EXISTS ix_shared_routine_members_user ON habitflow.shared_routine_members(user_id) WHERE left_at IS NULL;
CREATE TABLE IF NOT EXISTS habitflow.shared_goals (id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), created_by_user_id uuid NOT NULL REFERENCES habitflow.users(id), title varchar(160) NOT NULL, description text, target_value integer NOT NULL CHECK(target_value > 0), status varchar(40) NOT NULL DEFAULT 'Active', start_date date NOT NULL, end_date date, created_at timestamp NOT NULL DEFAULT now(), updated_at timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS habitflow.shared_goal_members (goal_id uuid NOT NULL REFERENCES habitflow.shared_goals(id) ON DELETE CASCADE, user_id uuid NOT NULL REFERENCES habitflow.users(id), role varchar(40) NOT NULL DEFAULT 'Participant' CHECK(role IN ('Owner','Participant','Viewer')), joined_at timestamp NOT NULL DEFAULT now(), PRIMARY KEY(goal_id,user_id));
CREATE TABLE IF NOT EXISTS habitflow.shared_goal_progress (id uuid PRIMARY KEY, goal_id uuid NOT NULL REFERENCES habitflow.shared_goals(id) ON DELETE CASCADE, user_id uuid NOT NULL REFERENCES habitflow.users(id), value integer NOT NULL DEFAULT 0 CHECK(value >= 0), recorded_on date NOT NULL, created_at timestamp NOT NULL DEFAULT now(), UNIQUE(goal_id,user_id,recorded_on));
CREATE INDEX IF NOT EXISTS ix_shared_goals_client ON habitflow.shared_goals(client_id,status);
COMMIT;
BEGIN;
ALTER TABLE habitflow.habits ADD COLUMN IF NOT EXISTS visibility varchar(40) NOT NULL DEFAULT 'Private';
ALTER TABLE habitflow.habits DROP CONSTRAINT IF EXISTS ck_habits_visibility;
ALTER TABLE habitflow.habits ADD CONSTRAINT ck_habits_visibility CHECK(visibility IN ('Private','SharedWithRoutine','AggregateOnly'));
CREATE TABLE IF NOT EXISTS habitflow.product_events (id uuid PRIMARY KEY, client_id uuid REFERENCES habitflow.clients(id), user_id uuid REFERENCES habitflow.users(id), event_name varchar(120) NOT NULL, entity_type varchar(80), entity_id uuid, plan_code varchar(80), metadata jsonb, occurred_at timestamp NOT NULL DEFAULT now(), session_id varchar(120));
CREATE INDEX IF NOT EXISTS ix_product_events_occurred ON habitflow.product_events(occurred_at DESC,event_name);
CREATE INDEX IF NOT EXISTS ix_product_events_scope ON habitflow.product_events(client_id,user_id,occurred_at DESC);
COMMENT ON COLUMN habitflow.product_events.metadata IS 'Somente metadados operacionais; nunca documentos, credenciais ou conteúdo de hábitos.';
COMMIT;

-- v6.4 incremental product core
BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.job_locks (
 job_name varchar(120) PRIMARY KEY, locked_by varchar(200), locked_at timestamp,
 lock_expires_at timestamp, updated_at timestamp NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS habitflow.notification_deliveries (
 id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id),
 source_type varchar(80) NOT NULL, source_id varchar(160) NOT NULL, channel varchar(40) NOT NULL,
 scheduled_for timestamp NOT NULL, status varchar(40) NOT NULL, delivered_at timestamp, failure_reason text,
 created_at timestamp NOT NULL DEFAULT now(), UNIQUE(source_type,source_id,channel,scheduled_for)
);
CREATE INDEX IF NOT EXISTS ix_notification_deliveries_scope ON habitflow.notification_deliveries(client_id,user_id,scheduled_for DESC);
COMMIT;

BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.sharing_consents (
 id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
 consent_type varchar(80) NOT NULL CHECK(consent_type IN ('AggregateProgress','IndividualProgress','SharedGoals','SharedRoutines')),
 granted boolean NOT NULL DEFAULT false, granted_at timestamp, revoked_at timestamp, updated_at timestamp NOT NULL DEFAULT now(),
 UNIQUE(user_id,consent_type)
);
CREATE INDEX IF NOT EXISTS ix_sharing_consents_scope ON habitflow.sharing_consents(client_id,user_id);
COMMIT;

BEGIN;
ALTER TABLE habitflow.goal_habits ADD COLUMN IF NOT EXISTS client_id uuid REFERENCES habitflow.clients(id);
UPDATE habitflow.goal_habits gh SET client_id=g.client_id FROM habitflow.user_goals g WHERE g.id=gh.goal_id AND gh.client_id IS NULL;
ALTER TABLE habitflow.goal_habits ALTER COLUMN client_id SET NOT NULL;
CREATE INDEX IF NOT EXISTS ix_goal_habits_goal ON habitflow.goal_habits(client_id,goal_id);
CREATE INDEX IF NOT EXISTS ix_goal_habits_habit ON habitflow.goal_habits(client_id,habit_id);
CREATE TABLE IF NOT EXISTS habitflow.goal_progress_events (
 id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id),
 goal_id uuid NOT NULL REFERENCES habitflow.user_goals(id) ON DELETE CASCADE, previous_value integer NOT NULL, current_value integer NOT NULL,
 source_type varchar(60) NOT NULL, source_id varchar(160), created_at timestamp NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_goal_progress_events_scope ON habitflow.goal_progress_events(client_id,user_id,goal_id,created_at DESC);
COMMIT;

BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.report_snapshots (
 id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid REFERENCES habitflow.users(id),
 report_type varchar(80) NOT NULL, period_start date NOT NULL, period_end date NOT NULL, data jsonb NOT NULL,
 generated_at timestamp NOT NULL DEFAULT now(), CHECK(period_end>=period_start)
);
CREATE INDEX IF NOT EXISTS ix_report_snapshots_scope ON habitflow.report_snapshots(client_id,user_id,report_type,period_start,period_end);
COMMIT;

BEGIN;
CREATE UNIQUE INDEX IF NOT EXISTS ux_product_events_pwa_install_day
 ON habitflow.product_events(user_id,event_name,(occurred_at::date)) WHERE event_name='pwa_installed' AND user_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_product_events_analytics ON habitflow.product_events(event_name,occurred_at DESC,client_id);
COMMIT;
-- Password recovery is maintained in its canonical migration for new databases.
\i database/migrations/048_password_recovery_transactional_email.sql
\i database/migrations/049_billing_communication_rule_seed_integrity.sql


-- v6.9.2 forward migrations 051-052 (fresh database aggregate)
-- transaction-mode: runner-managed
alter table habitflow.system_settings add column if not exists description text null;
alter table habitflow.system_settings add column if not exists is_public boolean not null default false;
alter table habitflow.system_settings add column if not exists created_at timestamp null;
update habitflow.system_settings set created_at = coalesce(created_at, updated_at, now()) where created_at is null;
alter table habitflow.system_settings alter column created_at set default now();
alter table habitflow.system_settings alter column created_at set not null;
alter table habitflow.system_settings alter column is_public set default false;

-- transaction-mode: runner-managed
create extension if not exists unaccent;

alter table habitflow.habit_templates add column if not exists suggested_days smallint[] not null default '{}';
alter table habitflow.habit_templates add column if not exists suggested_target_per_week integer null;
alter table habitflow.habit_templates add column if not exists suggested_reminder_time time null;
alter table habitflow.habit_templates add column if not exists icon_code varchar(80) null;
alter table habitflow.habit_templates add column if not exists why_it_helps text null;
alter table habitflow.habit_templates add column if not exists how_to_start text null;
alter table habitflow.habit_templates add column if not exists first_action text null;
alter table habitflow.habit_templates add column if not exists tags text[] not null default '{}';
alter table habitflow.habit_templates add column if not exists minimum_plan_code varchar(40) not null default 'free';
alter table habitflow.habit_templates add column if not exists is_featured boolean not null default false;
alter table habitflow.habit_templates add column if not exists content_version integer not null default 1;
alter table habitflow.habit_templates add column if not exists published_at timestamp null;
alter table habitflow.habit_templates add constraint ck_habit_templates_days check (suggested_days <@ array[0,1,2,3,4,5,6]::smallint[]);
alter table habitflow.habit_templates add constraint ck_habit_templates_week_target check (suggested_target_per_week is null or suggested_target_per_week between 1 and 7);

alter table habitflow.habits add column if not exists client_id uuid null references habitflow.clients(id);
alter table habitflow.habits add column if not exists source_template_id uuid null references habitflow.habit_templates(id);
alter table habitflow.habits add column if not exists source_collection_id uuid null;
alter table habitflow.habits add column if not exists objective_id uuid null references habitflow.habit_objectives(id);
alter table habitflow.habits add column if not exists icon_code varchar(80) null;
alter table habitflow.habits add column if not exists difficulty varchar(50) null;
alter table habitflow.habits add column if not exists estimated_time_minutes integer null;
alter table habitflow.habits add column if not exists start_date date not null default current_date;
alter table habitflow.habits add column if not exists template_content_version integer null;
alter table habitflow.habits add column if not exists is_template_variation boolean not null default false;
alter table habitflow.habits add column if not exists template_idempotency_key uuid null;
create index if not exists ix_habits_source_template on habitflow.habits(client_id,user_id,source_template_id) where is_archived=false;
create unique index if not exists ux_habits_template_idempotency on habitflow.habits(client_id,user_id,template_idempotency_key) where template_idempotency_key is not null;

create table if not exists habitflow.habit_template_favorites (
 client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 template_id uuid not null references habitflow.habit_templates(id), created_at timestamptz not null default now(),
 primary key(client_id,user_id,template_id));

create table if not exists habitflow.habit_template_collections (
 id uuid primary key, slug varchar(100) not null unique, name varchar(140) not null, description text not null,
 objective_id uuid null references habitflow.habit_objectives(id), icon_code varchar(80), estimated_time_minutes integer,
 difficulty varchar(50) not null, minimum_plan_code varchar(40) not null default 'free', is_featured boolean not null default false,
 status varchar(20) not null default 'Draft', content_version integer not null default 1, sort_order integer not null default 0,
 created_at timestamp not null default now(), updated_at timestamp not null default now());
alter table habitflow.habits add constraint fk_habits_source_collection foreign key(source_collection_id) references habitflow.habit_template_collections(id);
create table if not exists habitflow.habit_template_collection_items (
 collection_id uuid not null references habitflow.habit_template_collections(id), template_id uuid not null references habitflow.habit_templates(id),
 sort_order integer not null default 0, is_required boolean not null default false, default_reminder_time time null,
 can_customize boolean not null default true, primary key(collection_id,template_id));

create table if not exists habitflow.user_onboarding_progress (
 client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id), current_step smallint not null default 1,
 selected_objective_slug varchar(80), available_minutes integer, preferred_frequency varchar(50), preferred_days smallint[] not null default '{}',
 preferred_time time, selected_template_ids uuid[] not null default '{}', selected_collection_id uuid null references habitflow.habit_template_collections(id),
 create_goal boolean not null default false, goal_target_type varchar(40), goal_target_value numeric(12,2), started_at timestamptz not null default now(),
 last_activity_at timestamptz not null default now(), completed_at timestamptz, skipped_at timestamptz, version integer not null default 1,
 primary key(client_id,user_id), constraint ck_onboarding_days check(preferred_days <@ array[0,1,2,3,4,5,6]::smallint[]),
 constraint ck_onboarding_terminal check(completed_at is null or skipped_at is null));

-- v6.10.0 additive legal/privacy/catalog governance
\i database/migrations/056_secure_admin_honest_plans_legal_privacy.sql
