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
