-- HabitFlow complete schema generated from the ordered migration runner.
-- Do not add schema changes here; update database/migrations and database/migrate.sql.

-- BEGIN include database/migrations/001_create_schema.sql
create schema if not exists habitflow;
-- END include database/migrations/001_create_schema.sql

-- BEGIN include database/migrations/002_create_users.sql
create table if not exists habitflow.users(id uuid primary key,name varchar(150) not null,email varchar(200) unique not null,password_hash text not null,photo_url text null,role varchar(50) not null default 'User',account_status varchar(50) not null default 'Active',risk_status varchar(50) not null default 'Normal',plan varchar(50) not null default 'Free',plan_status varchar(50) not null default 'Active',wants_premium_notice boolean not null default false,onboarding_completed boolean not null default false,accepted_terms_at timestamp null,accepted_privacy_at timestamp null,last_login_at timestamp null,last_activity_at timestamp null,created_at timestamp not null,updated_at timestamp not null);
create table if not exists habitflow.login_attempts(id uuid primary key,email varchar(200) null,success boolean not null,ip_address varchar(100) null,user_agent text null,created_at timestamp not null);
-- END include database/migrations/002_create_users.sql

-- BEGIN include database/migrations/003_create_habits.sql
create table if not exists habitflow.habits(id uuid primary key,user_id uuid not null references habitflow.users(id),name varchar(120) not null,color varchar(20) not null,category varchar(80) null,is_archived boolean not null default false,archived_at timestamp null,created_at timestamp not null,updated_at timestamp not null);
-- END include database/migrations/003_create_habits.sql

-- BEGIN include database/migrations/004_create_habit_completions.sql
create table if not exists habitflow.habit_completions(id uuid primary key,habit_id uuid not null references habitflow.habits(id),user_id uuid not null references habitflow.users(id),completed_date date not null,created_at timestamp not null,unique(habit_id, completed_date));
-- END include database/migrations/004_create_habit_completions.sql

-- BEGIN include database/migrations/005_create_support.sql
create table if not exists habitflow.support_tickets(id uuid primary key,user_id uuid not null references habitflow.users(id),protocol varchar(50) unique not null,type varchar(50) not null,status varchar(50) not null,priority varchar(50) not null,title varchar(200) not null,description text null,source varchar(50) null,created_at timestamp not null,updated_at timestamp not null,resolved_at timestamp null);
create table if not exists habitflow.support_messages(id uuid primary key,ticket_id uuid not null references habitflow.support_tickets(id),user_id uuid null references habitflow.users(id),role varchar(50) not null,message text not null,is_sensitive_blocked boolean not null default false,created_at timestamp not null);
-- END include database/migrations/005_create_support.sql

-- BEGIN include database/migrations/006_create_audit.sql
create table if not exists habitflow.system_audit_logs(id uuid primary key,user_id uuid null,user_email varchar(200) null,severity varchar(50) not null,source varchar(50) not null,action varchar(100) not null,message text not null,metadata jsonb null,error_code varchar(100) null,error_fingerprint varchar(200) null,created_at timestamp not null,read_by_admin boolean not null default false);
create table if not exists habitflow.admin_audit_logs(id uuid primary key,admin_user_id uuid null,admin_email varchar(200) null,action varchar(100) not null,target_user_id uuid null,target_user_email varchar(200) null,reason text null,metadata jsonb null,created_at timestamp not null);
-- END include database/migrations/006_create_audit.sql

-- BEGIN include database/migrations/007_create_settings.sql
create table if not exists habitflow.system_settings(key varchar(100) primary key,value jsonb not null,updated_at timestamp not null,updated_by uuid null);
-- END include database/migrations/007_create_settings.sql

-- BEGIN include database/migrations/008_create_lgpd.sql
create table if not exists habitflow.lgpd_requests(id uuid primary key,user_id uuid not null references habitflow.users(id),protocol varchar(50) unique not null,type varchar(50) not null,status varchar(50) not null,notes text null,rejection_reason text null,handled_by uuid null,created_at timestamp not null,updated_at timestamp not null,completed_at timestamp null);
-- END include database/migrations/008_create_lgpd.sql

-- BEGIN include database/migrations/009_create_billing.sql
create table if not exists habitflow.billing_events(id uuid primary key,user_id uuid null references habitflow.users(id),provider varchar(50) null,event_type varchar(100) not null,plan varchar(50) null,status varchar(50) null,amount numeric(12,2) null,metadata jsonb null,created_at timestamp not null);
-- END include database/migrations/009_create_billing.sql

-- BEGIN include database/migrations/010_create_indexes.sql
create index if not exists ix_users_email on habitflow.users(email); create index if not exists ix_users_role on habitflow.users(role); create index if not exists ix_users_account_status on habitflow.users(account_status); create index if not exists ix_users_plan on habitflow.users(plan); create index if not exists ix_users_created_at on habitflow.users(created_at); create index if not exists ix_habits_user_id on habitflow.habits(user_id); create index if not exists ix_habit_completions_user_id on habitflow.habit_completions(user_id); create index if not exists ix_habit_completions_habit_id on habitflow.habit_completions(habit_id); create index if not exists ix_habit_completions_completed_date on habitflow.habit_completions(completed_date); create index if not exists ix_support_tickets_user_id on habitflow.support_tickets(user_id); create index if not exists ix_lgpd_requests_user_id on habitflow.lgpd_requests(user_id); create index if not exists ix_system_audit_logs_created_at on habitflow.system_audit_logs(created_at); create index if not exists ix_system_audit_logs_severity on habitflow.system_audit_logs(severity); create index if not exists ix_admin_audit_logs_created_at on habitflow.admin_audit_logs(created_at);
-- END include database/migrations/010_create_indexes.sql

-- BEGIN include database/migrations/011_seed_initial_settings.sql
insert into habitflow.system_settings(key,value,updated_at) values ('companyName','"MNSOFT"',now()),('companyLegalName','"MNSOLUÇÕES TECNOLÓGICAS & CONSULTORIA LTDA"',now()),('companyCnpj','"18.160.057/0001-13"',now()),('commercialEmail','"comercial@mnsoft.com.br"',now()),('supportEmail','"comercial@mnsoft.com.br"',now()),('whatsappEnabled','false',now()) on conflict(key) do nothing;
-- END include database/migrations/011_seed_initial_settings.sql

-- BEGIN include database/migrations/012_habit_recurrence_reports_notifications.sql
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
-- END include database/migrations/012_habit_recurrence_reports_notifications.sql

-- BEGIN include database/migrations/013_admin_operacional.sql
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

create index if not exists ix_users_account_status on habitflow.users(account_status);
create index if not exists ix_users_risk_status on habitflow.users(risk_status);
create index if not exists ix_users_plan on habitflow.users(plan);
create index if not exists ix_users_wants_premium_notice on habitflow.users(wants_premium_notice);
create index if not exists ix_users_last_login_at on habitflow.users(last_login_at);
create index if not exists ix_admin_user_notes_user_id on habitflow.admin_user_notes(user_id);
create index if not exists ix_admin_exports_created_at on habitflow.admin_exports(created_at);
create index if not exists ix_admin_dashboard_snapshots_snapshot_date on habitflow.admin_dashboard_snapshots(snapshot_date);
-- END include database/migrations/013_admin_operacional.sql

-- BEGIN include database/migrations/014_windows_iis_operations.sql
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
-- END include database/migrations/014_windows_iis_operations.sql

-- BEGIN include database/migrations/015_schema_hardening.sql
-- HabitFlow v4.5 - hardening do schema PostgreSQL.
-- Não apaga, move ou corrige automaticamente tabelas existentes em public.

create schema if not exists habitflow;

do $$
declare conflict_count integer;
begin
    select count(*) into conflict_count
    from information_schema.tables
    where table_schema = 'public'
      and table_name in ('users','habits','habit_completions','support_tickets','support_messages','system_audit_logs','admin_audit_logs','system_settings','lgpd_requests','billing_events','notifications','user_reports');

    if conflict_count > 0 then
        raise warning 'HabitFlow v4.5: existem % tabelas HabitFlow no schema public. Revise manualmente; a migration não move/apaga dados.', conflict_count;
    end if;
end $$;

do $$
declare item text[];
begin
    foreach item slice 1 in array array[
        array['habitflow.users','ck_users_role','ck_habitflow_users_role'],
        array['habitflow.users','ck_users_account_status','ck_habitflow_users_account_status'],
        array['habitflow.users','ck_users_risk_status','ck_habitflow_users_risk_status'],
        array['habitflow.users','ck_users_plan','ck_habitflow_users_plan'],
        array['habitflow.users','ck_users_plan_status','ck_habitflow_users_plan_status'],
        array['habitflow.habit_completions','uq_habit_completions_habit_date','uq_habitflow_habit_completions_habit_date'],
        array['habitflow.support_tickets','ck_support_tickets_status','ck_habitflow_support_tickets_status'],
        array['habitflow.lgpd_requests','ck_lgpd_requests_type','ck_habitflow_lgpd_requests_type'],
        array['habitflow.lgpd_requests','ck_lgpd_requests_status','ck_habitflow_lgpd_requests_status'],
        array['habitflow.billing_events','ck_billing_events_plan','ck_habitflow_billing_events_plan']
    ] loop
        if to_regclass(item[1]) is not null and exists(select 1 from pg_constraint where conrelid = item[1]::regclass and conname = item[2]) and not exists(select 1 from pg_constraint where conrelid = item[1]::regclass and conname = item[3]) then
            execute format('alter table %s rename constraint %I to %I', item[1], item[2], item[3]);
        end if;
    end loop;
end $$;

alter index if exists habitflow.ix_users_email rename to ix_habitflow_users_email;
alter index if exists habitflow.ix_users_role rename to ix_habitflow_users_role;
alter index if exists habitflow.ix_users_account_status rename to ix_habitflow_users_account_status;
alter index if exists habitflow.ix_users_plan rename to ix_habitflow_users_plan;
alter index if exists habitflow.ix_habits_user_id rename to ix_habitflow_habits_user_id;
alter index if exists habitflow.ix_habit_completions_user_id rename to ix_habitflow_habit_completions_user_id;
alter index if exists habitflow.ix_support_tickets_user_id rename to ix_habitflow_support_tickets_user_id;
alter index if exists habitflow.ix_lgpd_requests_user_id rename to ix_habitflow_lgpd_requests_user_id;
alter index if exists habitflow.ix_system_audit_logs_created_at rename to ix_habitflow_system_audit_logs_created_at;
alter index if exists habitflow.ix_admin_audit_logs_created_at rename to ix_habitflow_admin_audit_logs_created_at;

create index if not exists ix_habitflow_users_email on habitflow.users(email);
create index if not exists ix_habitflow_users_role on habitflow.users(role);
create index if not exists ix_habitflow_users_account_status on habitflow.users(account_status);
create index if not exists ix_habitflow_users_plan on habitflow.users(plan);
create index if not exists ix_habitflow_habits_user_id on habitflow.habits(user_id);
create index if not exists ix_habitflow_habit_completions_user_id on habitflow.habit_completions(user_id);
create index if not exists ix_habitflow_support_tickets_user_id on habitflow.support_tickets(user_id);
create index if not exists ix_habitflow_lgpd_requests_user_id on habitflow.lgpd_requests(user_id);
create index if not exists ix_habitflow_system_audit_logs_created_at on habitflow.system_audit_logs(created_at);
create index if not exists ix_habitflow_admin_audit_logs_created_at on habitflow.admin_audit_logs(created_at);
-- END include database/migrations/015_schema_hardening.sql

-- BEGIN include database/migrations/016_premium_billing.sql
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
-- END include database/migrations/016_premium_billing.sql

-- BEGIN include database/migrations/017_habit_templates_guided_journey.sql
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
-- END include database/migrations/017_habit_templates_guided_journey.sql

-- BEGIN include database/migrations/018_user_ui_preferences_accessibility.sql
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
-- END include database/migrations/018_user_ui_preferences_accessibility.sql

-- BEGIN include database/migrations/019_notifications_feedback_preferences.sql
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
-- END include database/migrations/019_notifications_feedback_preferences.sql

-- BEGIN include database/migrations/020_popup_preferences.sql
alter table habitflow.user_ui_preferences add column if not exists show_achievement_popups boolean not null default true;
alter table habitflow.user_ui_preferences add column if not exists show_tip_popups boolean not null default true;
alter table habitflow.user_ui_preferences add column if not exists enable_toasts boolean not null default true;
alter table habitflow.user_ui_preferences add column if not exists reduce_popups boolean not null default false;
-- END include database/migrations/020_popup_preferences.sql

-- BEGIN include database/migrations/021_clients_management.sql
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
-- END include database/migrations/021_clients_management.sql

-- BEGIN include database/migrations/022_users_clients_link.sql
alter table habitflow.users add column if not exists client_id uuid null references habitflow.clients(id);
create index if not exists ix_habitflow_users_client_id on habitflow.users(client_id);
-- END include database/migrations/022_users_clients_link.sql

-- BEGIN include database/migrations/023_clients_cpf_cnpj_superadmin.sql
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
-- END include database/migrations/023_clients_cpf_cnpj_superadmin.sql

-- BEGIN include database/migrations/024_superadmin_billing_entitlements.sql
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
-- END include database/migrations/024_superadmin_billing_entitlements.sql

-- BEGIN include database/migrations/025_tenant_isolation_client_id.sql
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
-- END include database/migrations/025_tenant_isolation_client_id.sql

-- BEGIN include database/migrations/026_backfill_client_id.sql
-- Development-oriented backfill. Review manually before production use.
set search_path to habitflow;

insert into habitflow.clients(id, name, legal_name, document, plan, status, is_active, created_at, updated_at)
select gen_random_uuid(), 'Cliente Demonstração HabitFlow', 'Cliente Demonstração HabitFlow', '00000000000000', 'Free', 'Active', true, now(), now()
where not exists (select 1 from habitflow.clients where document = '00000000000000');

with demo as (select id from habitflow.clients where document = '00000000000000' limit 1)
update habitflow.users u set client_id = demo.id, updated_at = now()
from demo
where u.client_id is null and u.role <> 'SuperAdmin';

update habitflow.habits h set client_id = u.client_id, updated_at = now()
from habitflow.users u where h.user_id = u.id and h.client_id is null and u.client_id is not null;
update habitflow.habit_completions c set client_id = u.client_id
from habitflow.users u where c.user_id = u.id and c.client_id is null and u.client_id is not null;
update habitflow.notifications n set client_id = u.client_id
from habitflow.users u where n.user_id = u.id and n.client_id is null and u.client_id is not null;
update habitflow.user_reports r set client_id = u.client_id
from habitflow.users u where r.user_id = u.id and r.client_id is null and u.client_id is not null;
update habitflow.support_tickets t set client_id = u.client_id
from habitflow.users u where t.user_id = u.id and t.client_id is null and u.client_id is not null;
-- END include database/migrations/026_backfill_client_id.sql

-- BEGIN include database/migrations/027_user_invites.sql
set search_path to habitflow;

create table if not exists habitflow.user_invites(
    id uuid primary key,
    client_id uuid not null references habitflow.clients(id),
    email varchar(200) not null,
    role varchar(80) not null default 'User',
    token_hash text not null,
    status varchar(40) not null default 'Pending',
    invited_by_user_id uuid null references habitflow.users(id),
    accepted_by_user_id uuid null references habitflow.users(id),
    expires_at timestamp not null,
    accepted_at timestamp null,
    canceled_at timestamp null,
    created_at timestamp not null default now(),
    updated_at timestamp not null default now(),
    constraint ck_habitflow_user_invites_status check (status in ('Pending','Accepted','Expired','Canceled')),
    constraint ck_habitflow_user_invites_role check (role in ('User','Admin'))
);

create unique index if not exists ux_habitflow_user_invites_token_hash on habitflow.user_invites(token_hash);
create index if not exists ix_habitflow_user_invites_client_id on habitflow.user_invites(client_id);
create index if not exists ix_habitflow_user_invites_email_status on habitflow.user_invites(email, status);
-- END include database/migrations/027_user_invites.sql

-- BEGIN include database/migrations/028_client_onboarding.sql
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
-- END include database/migrations/028_client_onboarding.sql

-- BEGIN include database/migrations/029_operational_completeness_v61.sql
create schema if not exists habitflow;
create extension if not exists pgcrypto;

create table if not exists habitflow.schema_migrations (
    id varchar(120) primary key,
    name varchar(200) not null,
    applied_at timestamp not null default now(),
    checksum varchar(200) null
);

alter table if exists habitflow.users drop constraint if exists ck_habitflow_users_role;
alter table if exists habitflow.users add constraint ck_habitflow_users_role check (role in ('User','Admin','SuperAdmin'));

create table if not exists habitflow.client_onboarding (
    id uuid primary key default gen_random_uuid(), client_id uuid not null unique references habitflow.clients(id) on delete cascade,
    company_data_completed boolean not null default false, billing_data_completed boolean not null default false,
    first_user_invited boolean not null default false, first_habit_created boolean not null default false,
    plan_reviewed boolean not null default false, completed boolean not null default false, completed_at timestamp null,
    created_at timestamp not null default now(), updated_at timestamp not null default now()
);

create table if not exists habitflow.billing_communication_rules (
    id uuid primary key default gen_random_uuid(), code varchar(80) not null unique, name varchar(160) not null,
    trigger_type varchar(80) not null, days_offset integer not null default 0, channel varchar(40) not null default 'Internal',
    title varchar(200) not null, message_template text not null, is_active boolean not null default true,
    created_at timestamp not null default now(), updated_at timestamp not null default now()
);

create table if not exists habitflow.client_communications (
    id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id) on delete cascade,
    user_id uuid null references habitflow.users(id) on delete set null, invoice_id uuid null,
    type varchar(80) not null, channel varchar(40) not null default 'Internal', title varchar(200) not null,
    message text not null, status varchar(40) not null default 'Sent', sent_at timestamp null, read_at timestamp null,
    created_at timestamp not null default now()
);

create table if not exists habitflow.job_execution_logs (
    id uuid primary key default gen_random_uuid(), job_name varchar(120) not null, status varchar(40) not null,
    started_at timestamp not null default now(), finished_at timestamp null, duration_ms bigint null,
    processed_count integer not null default 0, error_message text null, created_at timestamp not null default now()
);

create table if not exists habitflow.client_invoices (
    id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id) on delete cascade,
    subscription_id uuid null, invoice_number varchar(80) null, amount numeric(12,2) not null default 0,
    due_date date not null, payment_method varchar(40) not null default 'Manual', status varchar(40) not null default 'Pending',
    paid_at timestamp null, checkout_url text null, provider_payment_id varchar(160) null, created_at timestamp not null default now(), updated_at timestamp not null default now()
);

create table if not exists habitflow.client_subscriptions (
    id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id) on delete cascade,
    plan_code varchar(80) not null, status varchar(40) not null default 'Pending', billing_cycle varchar(40) null,
    current_period_start timestamp null, current_period_end timestamp null, trial_ends_at timestamp null, canceled_at timestamp null,
    created_at timestamp not null default now(), updated_at timestamp not null default now()
);

create table if not exists habitflow.client_entitlement_events (
    id uuid primary key default gen_random_uuid(), client_id uuid not null references habitflow.clients(id) on delete cascade,
    event_type varchar(80) not null, reason text null, created_at timestamp not null default now()
);

create table if not exists habitflow.superadmin_audit_logs (
    id uuid primary key default gen_random_uuid(), actor_user_id uuid null, actor_email varchar(200) null,
    action varchar(120) not null, target_type varchar(80) not null, target_id uuid null, reason text null,
    metadata jsonb not null default '{}'::jsonb, created_at timestamp not null default now()
);

alter table if exists habitflow.support_tickets add column if not exists sla_due_at timestamp null;
alter table if exists habitflow.support_tickets add column if not exists first_response_at timestamp null;
alter table if exists habitflow.support_tickets add column if not exists resolved_at timestamp null;
alter table if exists habitflow.clients add column if not exists billing_email varchar(200) null;
alter table if exists habitflow.clients add column if not exists payment_status varchar(40) not null default 'None';
alter table if exists habitflow.clients add column if not exists benefits_status varchar(40) not null default 'Free';
alter table if exists habitflow.clients add column if not exists subscription_status varchar(40) not null default 'Free';
alter table if exists habitflow.clients add column if not exists last_payment_at timestamp null;
alter table if exists habitflow.clients add column if not exists next_due_date date null;
alter table if exists habitflow.clients add column if not exists overdue_since date null;
alter table if exists habitflow.clients add column if not exists grace_period_until date null;

create index if not exists ix_client_communications_client_invoice_type_channel on habitflow.client_communications(client_id, invoice_id, type, channel);
create index if not exists ix_client_invoices_client_status_due on habitflow.client_invoices(client_id, status, due_date);
create index if not exists ix_client_subscriptions_client_status on habitflow.client_subscriptions(client_id, status);
create index if not exists ix_superadmin_audit_logs_created on habitflow.superadmin_audit_logs(created_at desc);
create index if not exists ix_job_execution_logs_job_started on habitflow.job_execution_logs(job_name, started_at desc);

insert into habitflow.schema_migrations(id,name) values ('029','operational_completeness_v61') on conflict (id) do update set name=excluded.name, applied_at=now();
-- END include database/migrations/029_operational_completeness_v61.sql

-- BEGIN include database/migrations/030_client_registration_cpf_cnpj_real_flow.sql
-- v6.1.1 - Public SaaS registration with CPF/CNPJ.
set search_path to habitflow;

alter table habitflow.clients add column if not exists person_type varchar(20) not null default 'LegalPerson';
alter table habitflow.clients add column if not exists document_type varchar(10) not null default 'CNPJ';
alter table habitflow.clients add column if not exists document_raw varchar(30);
alter table habitflow.clients add column if not exists document_normalized varchar(20);
alter table habitflow.clients add column if not exists legal_name varchar(180);
alter table habitflow.clients add column if not exists trade_name varchar(180);
alter table habitflow.clients add column if not exists billing_responsible_name varchar(160);
alter table habitflow.clients add column if not exists billing_email varchar(200);
alter table habitflow.clients add column if not exists billing_phone varchar(40);
alter table habitflow.users add column if not exists client_id uuid null references habitflow.clients(id);

alter table habitflow.clients drop constraint if exists ck_habitflow_clients_person_type;
alter table habitflow.clients add constraint ck_habitflow_clients_person_type check (person_type in ('NaturalPerson','LegalPerson'));
alter table habitflow.clients drop constraint if exists ck_habitflow_clients_document_type;
alter table habitflow.clients add constraint ck_habitflow_clients_document_type check (document_type in ('CPF','CNPJ'));
alter table habitflow.clients drop constraint if exists ck_habitflow_clients_person_document_match;
alter table habitflow.clients add constraint ck_habitflow_clients_person_document_match check ((person_type = 'NaturalPerson' and document_type = 'CPF') or (person_type = 'LegalPerson' and document_type = 'CNPJ'));

alter table habitflow.users drop constraint if exists ck_habitflow_users_role;
alter table habitflow.users add constraint ck_habitflow_users_role check (role in ('User','Admin','SuperAdmin'));

update habitflow.clients set document_normalized = regexp_replace(coalesce(document_raw, document, ''), '\D', '', 'g') where document_normalized is null and coalesce(document_raw, document) is not null;
create unique index if not exists ux_habitflow_clients_document_normalized_not_null on habitflow.clients(document_normalized) where document_normalized is not null and btrim(document_normalized) <> '';
create index if not exists ix_habitflow_clients_document_normalized on habitflow.clients(document_normalized);
create index if not exists ix_habitflow_clients_person_type on habitflow.clients(person_type);
create index if not exists ix_habitflow_clients_document_type on habitflow.clients(document_type);

insert into habitflow.schema_migrations(id, name, applied_at) values ('030','client_registration_cpf_cnpj_real_flow', now()) on conflict (id) do nothing;
-- END include database/migrations/030_client_registration_cpf_cnpj_real_flow.sql

-- BEGIN include database/migrations/031_registration_claims_onboarding_quality.sql
create schema if not exists habitflow;

create index if not exists ix_habitflow_clients_created_at on habitflow.clients(created_at);
create index if not exists ix_habitflow_clients_person_type on habitflow.clients(person_type);
create index if not exists ix_habitflow_clients_document_normalized on habitflow.clients(document_normalized);
create index if not exists ix_habitflow_users_client_id on habitflow.users(client_id);
create index if not exists ix_habitflow_users_role on habitflow.users(role);

create or replace view habitflow.vw_clients_without_admin as
select c.* from habitflow.clients c
where not exists (select 1 from habitflow.users u where u.client_id = c.id and u.role = 'Admin');

create or replace view habitflow.vw_users_without_client as
select u.id, u.name, u.email, u.role, u.created_at from habitflow.users u
where u.role <> 'SuperAdmin' and u.client_id is null;

create or replace view habitflow.vw_client_registration_quality as
select c.id client_id, c.created_at, c.person_type, c.name, c.document, c.document_normalized, c.email, c.plan, c.benefits_status, c.payment_status,
       exists(select 1 from habitflow.users u where u.client_id = c.id and u.role = 'Admin') has_admin,
       (c.document_normalized ~ '^[0-9]{11}$|^[0-9]{14}$') document_shape_valid
from habitflow.clients c;

insert into habitflow.schema_migrations(id, name, applied_at)
values ('031','registration_claims_onboarding_quality',now())
on conflict (id) do nothing;
-- END include database/migrations/031_registration_claims_onboarding_quality.sql

-- BEGIN include database/migrations/032_parameterized_plans_rbac_effective_access.sql
-- v6.2: catálogo de produtos separado do ciclo de cobrança.
set search_path to habitflow, public;

alter table habitflow.plans add column if not exists public_name varchar(120);
alter table habitflow.plans add column if not exists headline varchar(200);
alter table habitflow.plans add column if not exists audience_text text;
alter table habitflow.plans add column if not exists badge_text varchar(100);
alter table habitflow.plans add column if not exists is_featured boolean not null default false;
alter table habitflow.plans add column if not exists sort_order integer not null default 0;
alter table habitflow.plans add column if not exists created_by_user_id uuid null;
alter table habitflow.plans add column if not exists updated_by_user_id uuid null;
update habitflow.plans set public_name=coalesce(public_name,name);
alter table habitflow.plans alter column public_name set not null;

insert into habitflow.plans(id,code,name,public_name,headline,description,is_active,is_public,is_featured,sort_order,created_at,updated_at)
values
 ('10000000-0000-0000-0000-000000000001','free','Gratuito','Gratuito','Comece com leveza.','O essencial para cuidar da sua rotina.',true,true,false,10,now(),now()),
 ('10000000-0000-0000-0000-000000000002','ritmo','Ritmo','Ritmo','Tudo o que você precisa para manter sua rotina em movimento.','Mais liberdade para criar constância.',true,true,true,20,now(),now()),
 ('10000000-0000-0000-0000-000000000003','evolucao','Evolução','Evolução','Para evoluir junto com sua família, grupo ou pequena equipe.','Uma jornada compartilhada, no ritmo de vocês.',true,true,false,30,now(),now())
on conflict(code) do update set public_name=excluded.public_name, headline=excluded.headline, sort_order=excluded.sort_order;

alter table habitflow.clients add column if not exists contracted_plan_code varchar(80);
alter table habitflow.clients add column if not exists effective_plan_code varchar(80);
alter table habitflow.clients add column if not exists access_restriction_reason text;
alter table habitflow.clients add column if not exists access_restricted_at timestamp null;
alter table habitflow.clients add column if not exists access_restored_at timestamp null;
update habitflow.clients set contracted_plan_code=case plan::text when 'Premium' then 'ritmo' when 'Enterprise' then 'evolucao' else 'free' end where contracted_plan_code is null;
update habitflow.clients set effective_plan_code=case when benefits_status::text in ('PremiumBlocked','EnterpriseBlocked','RestrictedByPayment') then 'free' else contracted_plan_code end where effective_plan_code is null;
alter table habitflow.clients alter column contracted_plan_code set default 'free';
alter table habitflow.clients alter column effective_plan_code set default 'free';

alter table habitflow.client_subscriptions add column if not exists plan_code varchar(80);
alter table habitflow.client_subscriptions add column if not exists billing_cycle varchar(40);
update habitflow.client_subscriptions set plan_code=case when lower(coalesce(plan_code,'')) in ('premium_monthly','premium_yearly','premium') then 'ritmo' when lower(coalesce(plan_code,''))='enterprise' then 'evolucao' else coalesce(nullif(lower(plan_code),''),'free') end;
update habitflow.client_subscriptions set billing_cycle=case when lower(coalesce(billing_cycle,''))='yearly' or lower(coalesce(plan_code,''))='premium_yearly' then 'Yearly' else 'Monthly' end where billing_cycle is null;
comment on column habitflow.plans.price_monthly is 'LEGADO: remover somente após validação da migração em produção.';
comment on column habitflow.plans.price_yearly is 'LEGADO: usar habitflow.plan_prices.';
-- END include database/migrations/032_parameterized_plans_rbac_effective_access.sql

-- BEGIN include database/migrations/033_plan_prices_features.sql
set search_path to habitflow, public;

create table if not exists habitflow.plan_prices(
 id uuid primary key, plan_id uuid not null references habitflow.plans(id), billing_cycle varchar(40) not null check(billing_cycle in ('Monthly','Yearly')),
 amount numeric(12,2) not null check(amount>=0), currency varchar(10) not null default 'BRL', is_active boolean not null default true,
 valid_from timestamp not null default now(), valid_until timestamp null, created_at timestamp not null default now(), unique(plan_id,billing_cycle,valid_from));
create table if not exists habitflow.feature_catalog(
 code varchar(120) primary key, name varchar(160) not null, description text null, value_type varchar(30) not null check(value_type in ('Boolean','Integer','String')),
 category varchar(80) not null, is_active boolean not null default true, created_at timestamp not null default now());
create table if not exists habitflow.plan_features(
 plan_id uuid not null references habitflow.plans(id), feature_code varchar(120) not null references habitflow.feature_catalog(code), bool_value boolean null,
 int_value integer null, string_value text null, created_at timestamp not null default now(), updated_at timestamp not null default now(), primary key(plan_id,feature_code));

insert into habitflow.plan_prices(id,plan_id,billing_cycle,amount,currency,valid_from)
select v.id,p.id,v.cycle,v.amount,'BRL',timestamp '2026-01-01' from (values
 ('20000000-0000-0000-0000-000000000001'::uuid,'free','Monthly',0.00),('20000000-0000-0000-0000-000000000002'::uuid,'free','Yearly',0.00),
 ('20000000-0000-0000-0000-000000000003'::uuid,'ritmo','Monthly',19.90),('20000000-0000-0000-0000-000000000004'::uuid,'ritmo','Yearly',199.00),
 ('20000000-0000-0000-0000-000000000005'::uuid,'evolucao','Monthly',49.90),('20000000-0000-0000-0000-000000000006'::uuid,'evolucao','Yearly',499.00)) v(id,code,cycle,amount)
join habitflow.plans p on p.code=v.code on conflict do nothing;

insert into habitflow.feature_catalog(code,name,value_type,category) values
 ('active_habits_limit','Hábitos ativos','Integer','Limites'),('users_limit','Pessoas da conta','Integer','Limites'),('full_habit_library','Biblioteca completa','Boolean','Hábitos'),
 ('reminders_per_habit','Lembretes por hábito','Integer','Hábitos'),('active_goals_limit','Objetivos ativos','Integer','Objetivos'),('custom_categories','Categorias personalizadas','Boolean','Hábitos'),
 ('basic_reports','Resumo semanal','Boolean','Relatórios'),('advanced_reports','Relatórios avançados','Boolean','Relatórios'),('report_export_csv','Exportação CSV','Boolean','Relatórios'),
 ('report_print','Impressão de relatórios','Boolean','Relatórios'),('full_history','Histórico completo','Boolean','Histórico'),('shared_routines','Rotinas compartilhadas','Boolean','Compartilhamento'),
 ('shared_goals','Objetivos compartilhados','Boolean','Compartilhamento'),('client_admin_dashboard','Painel da conta','Boolean','Conta'),('consolidated_reports','Relatórios consolidados','Boolean','Relatórios'),
 ('user_invitations','Convites de pessoas','Boolean','Conta'),('priority_support','Suporte prioritário','Boolean','Suporte'),('internal_communications','Comunicações internas','Boolean','Conta')
on conflict(code) do update set name=excluded.name,value_type=excluded.value_type,category=excluded.category;

insert into habitflow.plan_features(plan_id,feature_code,bool_value,int_value)
select p.id,f.code,case when f.value_type='Boolean' then (case when p.code='free' then f.code in ('basic_reports','internal_communications') when p.code='ritmo' then f.code not in ('shared_routines','shared_goals','client_admin_dashboard','consolidated_reports','user_invitations','priority_support') else true end) end,
case when f.value_type='Integer' then case f.code when 'users_limit' then case when p.code='evolucao' then 5 else 1 end when 'active_habits_limit' then case when p.code='free' then 5 else -1 end when 'reminders_per_habit' then case when p.code='free' then 1 else -1 end when 'active_goals_limit' then case when p.code='free' then 1 else -1 end end end
from habitflow.plans p cross join habitflow.feature_catalog f where p.code in ('free','ritmo','evolucao') on conflict(plan_id,feature_code) do update set bool_value=excluded.bool_value,int_value=excluded.int_value,updated_at=now();
-- END include database/migrations/033_plan_prices_features.sql

-- BEGIN include database/migrations/034_roles_permissions.sql
set search_path to habitflow, public;
create table if not exists habitflow.roles(id uuid primary key,code varchar(80) unique not null,name varchar(120) not null,scope varchar(30) not null check(scope in ('Platform','Client')),description text,is_system boolean not null default false,is_active boolean not null default true);
create table if not exists habitflow.permissions(code varchar(120) primary key,name varchar(160) not null,description text,category varchar(80) not null);
create table if not exists habitflow.role_permissions(role_id uuid not null references habitflow.roles(id),permission_code varchar(120) not null references habitflow.permissions(code),primary key(role_id,permission_code));
create table if not exists habitflow.user_role_assignments(id uuid primary key,user_id uuid not null references habitflow.users(id),role_id uuid not null references habitflow.roles(id),client_id uuid null references habitflow.clients(id),assigned_by_user_id uuid null references habitflow.users(id),created_at timestamp not null default now(),revoked_at timestamp null);
create index if not exists ix_user_role_assignments_active on habitflow.user_role_assignments(user_id,client_id) where revoked_at is null;
insert into habitflow.roles(id,code,name,scope,is_system) values
('30000000-0000-0000-0000-000000000001','super_admin','Super Administrador','Platform',true),('30000000-0000-0000-0000-000000000002','finance_manager','Gestor Financeiro','Platform',true),('30000000-0000-0000-0000-000000000003','support_manager','Atendimento','Platform',true),('30000000-0000-0000-0000-000000000004','customer_success','Relacionamento','Platform',true),('30000000-0000-0000-0000-000000000005','auditor_readonly','Auditor','Platform',true),('30000000-0000-0000-0000-000000000006','account_owner','Proprietário da conta','Client',true),('30000000-0000-0000-0000-000000000007','account_admin','Administrador da conta','Client',true),('30000000-0000-0000-0000-000000000008','member','Membro','Client',true) on conflict(code) do update set name=excluded.name;
insert into habitflow.permissions(code,name,category) select code,replace(code,'.',' '),split_part(code,'.',1) from unnest(array['Platform.FullAccess','Platform.Clients.View','Platform.Clients.Manage','Platform.Users.View','Platform.Users.Manage','Platform.Plans.View','Platform.Plans.Manage','Platform.Billing.View','Platform.Billing.Manage','Platform.Support.Manage','Platform.Audit.View','Platform.Settings.Manage','Client.Account.Manage','Client.Users.Manage','Client.Billing.View','Client.Billing.Manage','Client.Routines.Use','Client.Reports.View','Client.SharedFeatures.Use']) code on conflict do nothing;
insert into habitflow.role_permissions select r.id,p.code from habitflow.roles r cross join habitflow.permissions p where r.code='super_admin' or (r.code='finance_manager' and p.code like 'Platform.Billing.%') or (r.code='support_manager' and p.code in ('Platform.Clients.View','Platform.Users.View','Platform.Support.Manage')) or (r.code='auditor_readonly' and p.code in ('Platform.Audit.View','Platform.Clients.View','Platform.Users.View')) or (r.code='account_owner' and p.code like 'Client.%') on conflict do nothing;
-- END include database/migrations/034_roles_permissions.sql

-- BEGIN include database/migrations/035_effective_plan_payment_restrictions.sql
set search_path to habitflow, public;
-- correção v6.19.6: colunas do contrato de system_settings criadas em 051, usadas já em 035;
-- guardas idempotentes mantêm o histórico aplicável em banco novo (ordem cronológica).
alter table habitflow.system_settings add column if not exists description text null;
alter table habitflow.system_settings add column if not exists is_public boolean not null default false;
alter table habitflow.system_settings add column if not exists created_at timestamp null;
alter table habitflow.client_subscriptions add column if not exists plan_id uuid null references habitflow.plans(id);
alter table habitflow.client_subscriptions add column if not exists plan_price_id uuid null references habitflow.plan_prices(id);
alter table habitflow.client_subscriptions add column if not exists price_snapshot numeric(12,2);
alter table habitflow.client_subscriptions add column if not exists currency_snapshot varchar(10);
alter table habitflow.client_subscriptions add column if not exists feature_snapshot jsonb;
alter table habitflow.client_subscriptions add column if not exists contracted_plan_name varchar(120);
create table if not exists habitflow.plan_restriction_snapshots(id uuid primary key,client_id uuid not null references habitflow.clients(id),previous_plan_code varchar(80) not null,restricted_plan_code varchar(80) not null default 'free',active_habits_snapshot jsonb not null default '[]',active_users_snapshot jsonb not null default '[]',created_at timestamp not null default now(),restored_at timestamp null);
alter table habitflow.payment_transactions add column if not exists client_id uuid null references habitflow.clients(id);
alter table habitflow.payment_transactions add column if not exists client_subscription_id uuid null references habitflow.client_subscriptions(id);
insert into habitflow.system_settings(key,value,description,is_public,created_at,updated_at) values('billing.grace_period_days','3','Período global de tolerância de pagamento.',false,now(),now()) on conflict(key) do nothing;
-- END include database/migrations/035_effective_plan_payment_restrictions.sql

-- BEGIN include database/migrations/036_personal_goals.sql
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
-- END include database/migrations/036_personal_goals.sql

-- BEGIN include database/migrations/037_consistency_milestones.sql
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
-- END include database/migrations/037_consistency_milestones.sql

-- BEGIN include database/migrations/038_reminders.sql
BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.habit_reminders (id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id), habit_id uuid NOT NULL REFERENCES habitflow.habits(id) ON DELETE CASCADE, reminder_time time NOT NULL, timezone varchar(80) NOT NULL DEFAULT 'America/Sao_Paulo', days_of_week integer[], is_active boolean NOT NULL DEFAULT true, last_triggered_at timestamp, next_trigger_at timestamp, created_at timestamp NOT NULL DEFAULT now(), updated_at timestamp NOT NULL DEFAULT now(), CHECK(days_of_week IS NULL OR days_of_week <@ ARRAY[0,1,2,3,4,5,6]));
CREATE INDEX IF NOT EXISTS ix_habit_reminders_due ON habitflow.habit_reminders(next_trigger_at) WHERE is_active;
CREATE INDEX IF NOT EXISTS ix_habit_reminders_scope ON habitflow.habit_reminders(client_id,user_id,habit_id);
CREATE TABLE IF NOT EXISTS habitflow.user_summary_preferences (user_id uuid PRIMARY KEY REFERENCES habitflow.users(id) ON DELETE CASCADE, daily_summary_enabled boolean NOT NULL DEFAULT true, daily_summary_time time NOT NULL DEFAULT '20:00', weekly_summary_enabled boolean NOT NULL DEFAULT true, weekly_summary_day integer NOT NULL DEFAULT 0 CHECK(weekly_summary_day BETWEEN 0 AND 6), weekly_summary_time time NOT NULL DEFAULT '18:00', timezone varchar(80) NOT NULL DEFAULT 'America/Sao_Paulo', updated_at timestamp NOT NULL DEFAULT now());
COMMIT;
-- END include database/migrations/038_reminders.sql

-- BEGIN include database/migrations/039_shared_routines.sql
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
-- END include database/migrations/039_shared_routines.sql

-- BEGIN include database/migrations/040_product_events_privacy.sql
BEGIN;
ALTER TABLE habitflow.habits ADD COLUMN IF NOT EXISTS visibility varchar(40) NOT NULL DEFAULT 'Private';
ALTER TABLE habitflow.habits DROP CONSTRAINT IF EXISTS ck_habits_visibility;
ALTER TABLE habitflow.habits ADD CONSTRAINT ck_habits_visibility CHECK(visibility IN ('Private','SharedWithRoutine','AggregateOnly'));
CREATE TABLE IF NOT EXISTS habitflow.product_events (id uuid PRIMARY KEY, client_id uuid REFERENCES habitflow.clients(id), user_id uuid REFERENCES habitflow.users(id), event_name varchar(120) NOT NULL, entity_type varchar(80), entity_id uuid, plan_code varchar(80), metadata jsonb, occurred_at timestamp NOT NULL DEFAULT now(), session_id varchar(120));
CREATE INDEX IF NOT EXISTS ix_product_events_occurred ON habitflow.product_events(occurred_at DESC,event_name);
CREATE INDEX IF NOT EXISTS ix_product_events_scope ON habitflow.product_events(client_id,user_id,occurred_at DESC);
COMMENT ON COLUMN habitflow.product_events.metadata IS 'Somente metadados operacionais; nunca documentos, credenciais ou conteúdo de hábitos.';
COMMIT;
-- END include database/migrations/040_product_events_privacy.sql

-- BEGIN include database/migrations/041_scheduler_locking_idempotency.sql
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
-- END include database/migrations/041_scheduler_locking_idempotency.sql

-- BEGIN include database/migrations/042_sharing_privacy_consent.sql
BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.sharing_consents (
 id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
 consent_type varchar(80) NOT NULL CHECK(consent_type IN ('AggregateProgress','IndividualProgress','SharedGoals','SharedRoutines')),
 granted boolean NOT NULL DEFAULT false, granted_at timestamp, revoked_at timestamp, updated_at timestamp NOT NULL DEFAULT now(),
 UNIQUE(user_id,consent_type)
);
CREATE INDEX IF NOT EXISTS ix_sharing_consents_scope ON habitflow.sharing_consents(client_id,user_id);
COMMIT;
-- END include database/migrations/042_sharing_privacy_consent.sql

-- BEGIN include database/migrations/043_goal_progress_completion.sql
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
-- END include database/migrations/043_goal_progress_completion.sql

-- BEGIN include database/migrations/044_report_snapshots_analytics.sql
BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.report_snapshots (
 id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid REFERENCES habitflow.users(id),
 report_type varchar(80) NOT NULL, period_start date NOT NULL, period_end date NOT NULL, data jsonb NOT NULL,
 generated_at timestamp NOT NULL DEFAULT now(), CHECK(period_end>=period_start)
);
CREATE INDEX IF NOT EXISTS ix_report_snapshots_scope ON habitflow.report_snapshots(client_id,user_id,report_type,period_start,period_end);
COMMIT;
-- END include database/migrations/044_report_snapshots_analytics.sql

-- BEGIN include database/migrations/045_pwa_product_events_hardening.sql
BEGIN;
CREATE UNIQUE INDEX IF NOT EXISTS ux_product_events_pwa_install_day
 ON habitflow.product_events(user_id,event_name,(occurred_at::date)) WHERE event_name='pwa_installed' AND user_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS ix_product_events_analytics ON habitflow.product_events(event_name,occurred_at DESC,client_id);
COMMIT;
-- END include database/migrations/045_pwa_product_events_hardening.sql

-- BEGIN include database/migrations/046_progress_history_days_limit.sql
begin;
insert into habitflow.feature_catalog(code,name,value_type,category)
values ('history_days_limit','Limite do histórico em dias','Integer','Histórico')
on conflict(code) do update set name=excluded.name,value_type=excluded.value_type,category=excluded.category;

insert into habitflow.plan_features(plan_id,feature_code,int_value)
select id,'history_days_limit',case when code='free' then 90 else -1 end
from habitflow.plans where code in ('free','ritmo','evolucao')
on conflict(plan_id,feature_code) do update set int_value=excluded.int_value,updated_at=now();
commit;
-- END include database/migrations/046_progress_history_days_limit.sql

-- BEGIN include database/migrations/047_schema_migration_governance.sql
alter table habitflow.schema_migrations add column if not exists filename varchar(260);
alter table habitflow.schema_migrations add column if not exists app_version varchar(80);
alter table habitflow.schema_migrations alter column checksum type varchar(64);
-- END include database/migrations/047_schema_migration_governance.sql

-- BEGIN include database/migrations/048_password_recovery_transactional_email.sql
-- Secure, single-use password recovery and asynchronous transactional email.
alter table habitflow.users add column if not exists session_version integer not null default 0;

create table if not exists habitflow.password_reset_tokens (
  id uuid primary key, user_id uuid not null references habitflow.users(id) on delete cascade,
  token_hash varchar(64) not null unique, expires_at timestamptz not null,
  used_at timestamptz null, revoked_at timestamptz null, created_at timestamptz not null,
  requested_ip_hash varchar(64) null, requested_user_agent_hash varchar(64) null,
  request_correlation_id varchar(100) null
);
create index if not exists ix_password_reset_tokens_user on habitflow.password_reset_tokens(user_id);
create index if not exists ix_password_reset_tokens_expires on habitflow.password_reset_tokens(expires_at);
create unique index if not exists ux_password_reset_tokens_active_user on habitflow.password_reset_tokens(user_id)
  where used_at is null and revoked_at is null;

create table if not exists habitflow.password_reset_requests (
  id uuid primary key, email_hash varchar(64) not null, ip_hash varchar(64) not null, created_at timestamptz not null
);
create index if not exists ix_password_reset_requests_email_time on habitflow.password_reset_requests(email_hash,created_at);
create index if not exists ix_password_reset_requests_ip_time on habitflow.password_reset_requests(ip_hash,created_at);

create table if not exists habitflow.transactional_email_outbox (
  id uuid primary key, client_id uuid null, user_id uuid null references habitflow.users(id) on delete set null,
  template_code varchar(80) not null, recipient varchar(254) not null, subject varchar(200) not null,
  payload_json jsonb not null, status varchar(20) not null check(status in ('Pending','Processing','Sent','Failed','DeadLetter')),
  idempotency_key varchar(160) not null unique, attempts integer not null default 0,
  next_attempt_at timestamptz not null, sent_at timestamptz null, last_error varchar(500) null,
  created_at timestamptz not null, updated_at timestamptz not null
);
create index if not exists ix_email_outbox_due on habitflow.transactional_email_outbox(status,next_attempt_at);
-- END include database/migrations/048_password_recovery_transactional_email.sql

-- BEGIN include database/migrations/049_billing_communication_rule_seed_integrity.sql
-- Transaction mode: required. DDL and legacy normalization are atomic.
BEGIN;

ALTER TABLE habitflow.billing_communication_rules
  ALTER COLUMN id SET DEFAULT gen_random_uuid();

-- Rename a legacy code only when its canonical replacement is absent. This keeps
-- the original row id and created_at.
UPDATE habitflow.billing_communication_rules legacy
SET code = CASE legacy.code
    WHEN 'overdue_plus_2' THEN 'due_plus_2'
    WHEN 'overdue_plus_5' THEN 'due_plus_5'
  END,
  trigger_type = 'AfterDueDate',
  updated_at = now()
WHERE legacy.code IN ('overdue_plus_2', 'overdue_plus_5')
  AND NOT EXISTS (
    SELECT 1 FROM habitflow.billing_communication_rules canonical
    WHERE canonical.code = CASE legacy.code
      WHEN 'overdue_plus_2' THEN 'due_plus_2'
      WHEN 'overdue_plus_5' THEN 'due_plus_5'
    END
  );

-- If both forms already exist, retain the row for audit/history but ensure that
-- only the canonical rule can be dispatched.
UPDATE habitflow.billing_communication_rules
SET is_active = false, updated_at = now()
WHERE code IN ('overdue_plus_2', 'overdue_plus_5');

COMMIT;
-- END include database/migrations/049_billing_communication_rule_seed_integrity.sql

-- BEGIN include database/migrations/050_goal_progress_activation_core.sql
-- transaction-mode: transactional
BEGIN;

ALTER TABLE habitflow.goal_progress_events
    ADD COLUMN IF NOT EXISTS event_type varchar(60),
    ADD COLUMN IF NOT EXISTS new_value integer,
    ADD COLUMN IF NOT EXISTS local_date date,
    ADD COLUMN IF NOT EXISTS source_completion_id uuid,
    ADD COLUMN IF NOT EXISTS idempotency_key varchar(240),
    ADD COLUMN IF NOT EXISTS correlation_id varchar(160),
    ADD COLUMN IF NOT EXISTS metadata_json jsonb NOT NULL DEFAULT '{}'::jsonb;

UPDATE habitflow.goal_progress_events
   SET event_type=coalesce(event_type,source_type),
       new_value=coalesce(new_value,current_value),
       local_date=coalesce(local_date,created_at::date),
       idempotency_key=coalesce(idempotency_key,'legacy:' || id::text),
       correlation_id=coalesce(correlation_id,'legacy:' || id::text)
 WHERE event_type IS NULL OR new_value IS NULL OR local_date IS NULL
    OR idempotency_key IS NULL OR correlation_id IS NULL;

ALTER TABLE habitflow.goal_progress_events
    ALTER COLUMN event_type SET NOT NULL,
    ALTER COLUMN new_value SET NOT NULL,
    ALTER COLUMN local_date SET NOT NULL,
    ALTER COLUMN idempotency_key SET NOT NULL,
    ALTER COLUMN correlation_id SET NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS ux_goal_progress_events_idempotency
    ON habitflow.goal_progress_events(idempotency_key);
CREATE INDEX IF NOT EXISTS ix_goal_progress_events_tenant_date
    ON habitflow.goal_progress_events(client_id,user_id,local_date DESC);

INSERT INTO habitflow.milestones(id,code,title,description,threshold)
VALUES ('50100000-0000-0000-0000-000000000001','first_goal_completed','Primeiro objetivo concluído','Você concluiu seu primeiro objetivo.',1)
ON CONFLICT(code) DO UPDATE SET title=excluded.title,description=excluded.description,threshold=excluded.threshold,is_active=true;

COMMIT;
-- END include database/migrations/050_goal_progress_activation_core.sql

-- BEGIN include database/migrations/051_system_settings_contract.sql
-- transaction-mode: runner-managed
alter table habitflow.system_settings add column if not exists description text null;
alter table habitflow.system_settings add column if not exists is_public boolean not null default false;
alter table habitflow.system_settings add column if not exists created_at timestamp null;
update habitflow.system_settings set created_at = coalesce(created_at, updated_at, now()) where created_at is null;
alter table habitflow.system_settings alter column created_at set default now();
alter table habitflow.system_settings alter column created_at set not null;
alter table habitflow.system_settings alter column is_public set default false;
-- END include database/migrations/051_system_settings_contract.sql

-- BEGIN include database/migrations/052_library_v2_onboarding.sql
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
-- END include database/migrations/052_library_v2_onboarding.sql

-- BEGIN include database/migrations/053_persistent_onboarding_drafts.sql
-- transaction-mode: runner-managed
create table if not exists habitflow.user_onboarding_draft_items (
 id uuid primary key,
 client_id uuid not null references habitflow.clients(id),
 user_id uuid not null references habitflow.users(id),
 template_id uuid not null references habitflow.habit_templates(id),
 collection_id uuid null references habitflow.habit_template_collections(id),
 name varchar(120) not null,
 frequency varchar(40) not null,
 days smallint[] not null default '{}',
 target_per_week integer null,
 preferred_time time null,
 color varchar(10) not null,
 category varchar(80) null,
 is_required boolean not null default false,
 sort_order integer not null default 0,
 created_at timestamptz not null default now(),
 constraint ck_onboarding_draft_days check(days <@ array[0,1,2,3,4,5,6]::smallint[]),
 constraint ck_onboarding_draft_target check(target_per_week is null or target_per_week between 1 and 7),
 unique(client_id,user_id,template_id)
);
create index if not exists ix_onboarding_draft_owner on habitflow.user_onboarding_draft_items(client_id,user_id,sort_order);
-- END include database/migrations/053_persistent_onboarding_drafts.sql

-- BEGIN include database/migrations/054_onboarding_engagement_notification_center.sql
-- transaction-mode: runner-managed
alter table habitflow.notifications add column if not exists client_id uuid null references habitflow.clients(id);
alter table habitflow.notifications add column if not exists category varchar(40) null;
alter table habitflow.notifications add column if not exists deduplication_key varchar(160) null;
alter table habitflow.notifications add column if not exists is_archived boolean not null default false;
alter table habitflow.notifications add column if not exists archived_at timestamptz null;
alter table habitflow.notifications add column if not exists expires_at timestamptz null;
update habitflow.notifications n set client_id=u.client_id from habitflow.users u where u.id=n.user_id and n.client_id is null;
create index if not exists ix_notifications_center on habitflow.notifications(user_id,is_archived,is_read,created_at desc);
create unique index if not exists ux_notifications_deduplication on habitflow.notifications(client_id,user_id,deduplication_key) where deduplication_key is not null;

create table if not exists habitflow.reminder_dispatches(
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 habit_reminder_id uuid not null references habitflow.habit_reminders(id), habit_id uuid not null references habitflow.habits(id),
 scheduled_for_utc timestamptz not null, channel varchar(24) not null default 'in_app', status varchar(24) not null,
 attempt_count integer not null default 0, processed_at timestamptz null, error_code varchar(80) null, created_at timestamptz not null default now(),
 unique(habit_reminder_id,scheduled_for_utc,channel)
);
create index if not exists ix_reminder_dispatch_status on habitflow.reminder_dispatches(status,scheduled_for_utc);

alter table habitflow.user_summary_preferences add column if not exists client_id uuid null references habitflow.clients(id);
update habitflow.user_summary_preferences p set client_id=u.client_id from habitflow.users u where u.id=p.user_id and p.client_id is null;
-- END include database/migrations/054_onboarding_engagement_notification_center.sql

-- BEGIN include database/migrations/055_routine_planner_weekly_review.sql
-- transaction-mode: runner-managed
-- Planejamento diário e revisão semanal, sempre isolados no schema habitflow.
create table if not exists habitflow.habit_schedule_exceptions (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id), habit_id uuid not null references habitflow.habits(id),
 local_date date not null, type varchar(16) not null, destination_date date null, reason varchar(240), version integer not null default 1,
 created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 constraint ck_schedule_exception_type check(type in ('Excused','Moved','Added')), constraint ck_schedule_exception_version check(version>0),
 constraint ck_schedule_exception_move check((type='Moved' and destination_date>local_date) or (type<>'Moved' and destination_date is null)), unique(client_id,user_id,habit_id,local_date)
);
create index if not exists ix_schedule_exceptions_range on habitflow.habit_schedule_exceptions(client_id,user_id,local_date);
create table if not exists habitflow.daily_routine_overrides (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id), habit_id uuid not null references habitflow.habits(id), local_date date not null,
 preferred_time time null, sort_order integer not null default 0, version integer not null default 1, created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 constraint ck_daily_override_version check(version>0), unique(client_id,user_id,habit_id,local_date)
);
create index if not exists ix_daily_overrides_day on habitflow.daily_routine_overrides(client_id,user_id,local_date,sort_order);
create table if not exists habitflow.weekly_reviews (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id), period_start date not null, period_end date not null,
 status varchar(16) not null default 'Draft', idempotency_key varchar(80) not null, version integer not null default 1, created_at timestamptz not null default now(), completed_at timestamptz null,
 constraint ck_weekly_review_status check(status in ('Draft','Completed')), constraint ck_weekly_review_period check(period_end=period_start+6), constraint ck_weekly_review_version check(version>0),
 unique(client_id,user_id,period_start), unique(client_id,user_id,idempotency_key)
);
create index if not exists ix_weekly_reviews_user_period on habitflow.weekly_reviews(client_id,user_id,period_start desc);
-- END include database/migrations/055_routine_planner_weekly_review.sql

-- BEGIN include database/migrations/056_secure_admin_honest_plans_legal_privacy.sql
-- v6.10.0. Additive/idempotent governance; never seeds an administrator credential.
BEGIN;
ALTER TABLE habitflow.users ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT false;
ALTER TABLE habitflow.plans ADD COLUMN IF NOT EXISTS is_sellable boolean NOT NULL DEFAULT false;
ALTER TABLE habitflow.plans ADD COLUMN IF NOT EXISTS sales_status varchar(24) NOT NULL DEFAULT 'Hidden';
ALTER TABLE habitflow.feature_catalog ADD COLUMN IF NOT EXISTS implementation_status varchar(24) NOT NULL DEFAULT 'Planned';
ALTER TABLE habitflow.feature_catalog ADD COLUMN IF NOT EXISTS is_marketable boolean NOT NULL DEFAULT false;

UPDATE habitflow.plans SET is_public=true,is_sellable=(code='ritmo'),sales_status='Available' WHERE code IN ('free','ritmo');
UPDATE habitflow.plans SET is_public=false,is_sellable=false,sales_status='Grandfathered' WHERE code='evolucao';
UPDATE habitflow.feature_catalog SET implementation_status='Implemented',is_marketable=true
 WHERE code IN ('active_habits_limit','active_goals_limit','full_habit_library','basic_reports','report_export_csv','report_print','full_history','history_days_limit','custom_categories');
UPDATE habitflow.feature_catalog SET implementation_status='Partial',is_marketable=false
 WHERE code IN ('reminders_per_habit','advanced_reports','shared_routines');
UPDATE habitflow.feature_catalog SET implementation_status='Planned',is_marketable=false
 WHERE code IN ('shared_goals','consolidated_reports','priority_support');
UPDATE habitflow.feature_catalog SET implementation_status='Internal',is_marketable=false
 WHERE code IN ('users_limit','user_invitations','client_admin_dashboard','internal_communications');

CREATE TABLE IF NOT EXISTS habitflow.legal_documents(
 id uuid PRIMARY KEY, document_type varchar(40) NOT NULL UNIQUE, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS habitflow.legal_document_versions(
 id uuid PRIMARY KEY, document_id uuid NOT NULL REFERENCES habitflow.legal_documents(id), version varchar(30) NOT NULL,
 locale varchar(12) NOT NULL DEFAULT 'pt-BR', title varchar(180) NOT NULL, summary text NOT NULL,
 sanitized_content text NOT NULL, content_hash varchar(64) NOT NULL, effective_at timestamptz NOT NULL,
 published_at timestamptz, requires_reacceptance boolean NOT NULL DEFAULT false, status varchar(20) NOT NULL DEFAULT 'Draft',
 created_by_user_id uuid REFERENCES habitflow.users(id), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE(document_id,version,locale), CHECK(status IN ('Draft','Published','Superseded','Archived')));
CREATE TABLE IF NOT EXISTS habitflow.user_legal_acceptances(
 id uuid PRIMARY KEY, client_id uuid REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id),
 document_type varchar(40) NOT NULL, version varchar(30) NOT NULL, content_hash varchar(64) NOT NULL,
 accepted_at timestamptz NOT NULL DEFAULT now(), source varchar(30) NOT NULL, correlation_id varchar(80) NOT NULL,
 ip_hmac varchar(64), user_agent_hmac varchar(64), revoked_at timestamptz, UNIQUE(user_id,document_type,version));
CREATE TABLE IF NOT EXISTS habitflow.user_consents(
 id uuid PRIMARY KEY, client_id uuid REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id),
 purpose varchar(50) NOT NULL, granted boolean NOT NULL DEFAULT false, recorded_at timestamptz NOT NULL DEFAULT now(),
 revoked_at timestamptz, correlation_id varchar(80) NOT NULL, UNIQUE(user_id,purpose));
CREATE TABLE IF NOT EXISTS habitflow.plan_public_benefits(
 id uuid PRIMARY KEY, plan_code varchar(40) NOT NULL, feature_code varchar(80) NOT NULL REFERENCES habitflow.feature_catalog(code),
 title varchar(120) NOT NULL, description text NOT NULL, icon_code varchar(80) NOT NULL, sort_order integer NOT NULL DEFAULT 0,
 comparison_group varchar(80) NOT NULL, is_highlighted boolean NOT NULL DEFAULT false, UNIQUE(plan_code,feature_code));
CREATE INDEX IF NOT EXISTS ix_legal_versions_current ON habitflow.legal_document_versions(document_id,locale,effective_at DESC) WHERE status='Published';
COMMIT;
-- END include database/migrations/056_secure_admin_honest_plans_legal_privacy.sql

-- BEGIN include database/migrations/057_legal_document_immutability.sql
-- v6.10.2: enforce the legal publication invariant at the database boundary.
BEGIN;

CREATE OR REPLACE FUNCTION habitflow.prevent_published_legal_version_mutation()
RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF OLD.status = 'Published' AND (
    NEW.version IS DISTINCT FROM OLD.version OR NEW.locale IS DISTINCT FROM OLD.locale OR
    NEW.title IS DISTINCT FROM OLD.title OR NEW.summary IS DISTINCT FROM OLD.summary OR
    NEW.sanitized_content IS DISTINCT FROM OLD.sanitized_content OR NEW.content_hash IS DISTINCT FROM OLD.content_hash OR
    NEW.effective_at IS DISTINCT FROM OLD.effective_at OR NEW.requires_reacceptance IS DISTINCT FROM OLD.requires_reacceptance
  ) THEN
    RAISE EXCEPTION 'published legal document versions are immutable' USING ERRCODE = 'check_violation';
  END IF;
  RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS trg_legal_version_immutable ON habitflow.legal_document_versions;
CREATE TRIGGER trg_legal_version_immutable
BEFORE UPDATE ON habitflow.legal_document_versions
FOR EACH ROW EXECUTE FUNCTION habitflow.prevent_published_legal_version_mutation();

COMMIT;
-- END include database/migrations/057_legal_document_immutability.sql

-- BEGIN include database/migrations/058_user_sessions.sql
-- v6.10.2 - Server-authorized account sessions and revocation.
BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.user_sessions (
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    client_id uuid REFERENCES habitflow.clients(id),
    user_agent varchar(500) NOT NULL,
    ip_address varchar(64) NOT NULL,
    created_at timestamptz NOT NULL,
    last_activity_at timestamptz NOT NULL,
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz,
    revocation_reason varchar(80)
);
CREATE INDEX IF NOT EXISTS ix_user_sessions_owner_active ON habitflow.user_sessions(user_id,client_id,last_activity_at DESC) WHERE revoked_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_user_sessions_expiration ON habitflow.user_sessions(expires_at) WHERE revoked_at IS NULL;
COMMIT;
-- END include database/migrations/058_user_sessions.sql

-- BEGIN include database/migrations/059_superadmin_mfa.sql
begin;

create table if not exists habitflow.user_mfa_settings (
    user_id uuid primary key references habitflow.users(id) on delete cascade,
    client_id uuid null references habitflow.clients(id) on delete cascade,
    protected_secret text not null,
    is_enabled boolean not null default false,
    last_accepted_time_step bigint null,
    created_at timestamptz not null default now(),
    enabled_at timestamptz null
);

create table if not exists habitflow.user_mfa_recovery_codes (
    id uuid primary key,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    client_id uuid null references habitflow.clients(id) on delete cascade,
    code_hash char(64) not null,
    created_at timestamptz not null default now(),
    used_at timestamptz null,
    unique (user_id, code_hash)
);

create table if not exists habitflow.user_mfa_challenges (
    id uuid primary key,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    client_id uuid null references habitflow.clients(id) on delete cascade,
    failed_attempts integer not null default 0 check (failed_attempts between 0 and 5),
    expires_at timestamptz not null,
    verified_at timestamptz null
);

create table if not exists habitflow.user_security_events (
    id uuid primary key,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    client_id uuid null references habitflow.clients(id) on delete cascade,
    event_type varchar(80) not null,
    occurred_at timestamptz not null default now()
);

create index if not exists ix_mfa_recovery_owner on habitflow.user_mfa_recovery_codes(user_id, client_id) where used_at is null;
create index if not exists ix_mfa_challenge_owner on habitflow.user_mfa_challenges(user_id, client_id, expires_at desc);
create index if not exists ix_security_event_owner on habitflow.user_security_events(user_id, client_id, occurred_at desc);

commit;
-- END include database/migrations/059_superadmin_mfa.sql

-- BEGIN include database/migrations/060_public_privacy_notice.sql
-- v6.10.5: baseline public notice. Runtime fallback remains available when migrations are pending.
BEGIN;
INSERT INTO habitflow.legal_documents(id, document_type, created_at)
VALUES ('61050000-0000-4000-8000-000000000001', 'PrivacyNotice', now())
ON CONFLICT (document_type) DO NOTHING;

WITH document AS (SELECT id FROM habitflow.legal_documents WHERE document_type='PrivacyNotice'), content AS (
 SELECT '<h2 id="quem-somos">Quem somos</h2><p>O HabitFlow é oferecido por MNSOLUÇÕES TECNOLÓGICAS &amp; CONSULTORIA LTDA, nome comercial MNSOFT, CNPJ 18.160.057/0001-13.</p><h2 id="dados-tratados">Dados tratados</h2><p>Tratamos dados cadastrais; login e segurança; hábitos, objetivos e progresso; plano, pagamento e assinatura; suporte; e dados técnicos de uso.</p><h2 id="finalidades">Finalidades e bases legais</h2><p>Usamos dados para prestar e proteger o serviço, executar a assinatura, atender solicitações, prevenir fraude, cumprir obrigações legais e melhorar o produto. Conforme o caso, usamos execução do contrato, obrigação legal, legítimo interesse avaliado e consentimento para escolhas opcionais.</p><h2>Fornecedores</h2><p>Compartilhamos somente o necessário com fornecedores de infraestrutura, autenticação, comunicação, pagamento e suporte, ou quando a lei exigir. Não vendemos dados pessoais.</p><h2>Segurança e retenção</h2><p>Aplicamos controles de acesso, proteção de credenciais e registros de segurança. Mantemos dados enquanto necessários ao serviço, às obrigações legais e à defesa de direitos; depois, excluímos ou anonimizamos.</p><h2 id="direitos">Seus direitos</h2><p>Você pode solicitar confirmação, acesso, correção, portabilidade, informações, anonimização ou exclusão aplicável e revisão de consentimentos. A identidade será confirmada para sua proteção.</p><h2 id="como-solicitar">Como solicitar</h2><p>Use a Central de Privacidade da conta ou o canal de suporte configurado no produto. Cancelar a assinatura não exclui os dados automaticamente.</p><h2>Cookies</h2><p>Cookies necessários mantêm sessão, preferências e segurança. Consulte o Aviso de Cookies para detalhes.</p><h2 id="contato">Contato</h2><p>Use o contato de privacidade configurado ou, se indisponível, o canal oficial de suporte exibido no produto. Nenhum endereço ou encarregado é presumido.</p><p><strong>Aviso:</strong> este conteúdo inicial deve passar por revisão jurídica antes do uso em produção.</p>'::text value)
INSERT INTO habitflow.legal_document_versions(id,document_id,version,locale,title,summary,sanitized_content,content_hash,effective_at,published_at,requires_reacceptance,status,created_by_user_id,created_at,updated_at)
SELECT '61050000-0000-4000-8000-000000000002', document.id, '1.0', 'pt-BR', 'Política de Privacidade',
 'Como a MNSOFT trata dados no HabitFlow e como você pode exercer seus direitos.', content.value,
 md5(content.value)||md5(content.value), timestamptz '2026-08-06 00:00:00+00', now(), false, 'Published', null, now(), now()
FROM document, content WHERE NOT EXISTS (SELECT 1 FROM habitflow.legal_document_versions v WHERE v.document_id=document.id AND v.locale='pt-BR' AND v.status='Published')
ON CONFLICT (document_id,version,locale) DO NOTHING;
COMMIT;
-- END include database/migrations/060_public_privacy_notice.sql

-- BEGIN include database/migrations/061_habit_lifecycle.sql
-- Habit lifecycle is reversible: pausing and archiving never remove completions.
begin;
alter table habitflow.habits add column if not exists is_paused boolean not null default false;
alter table habitflow.habits add column if not exists paused_at timestamptz null;
create index if not exists ix_habits_tenant_user_lifecycle
    on habitflow.habits(client_id, user_id, is_archived, is_paused, updated_at desc);
commit;
-- END include database/migrations/061_habit_lifecycle.sql

-- BEGIN include database/migrations/062_product_tips.sql
-- transaction-mode: runner-managed
create table if not exists habitflow.product_tips(
 id uuid primary key,
 code varchar(80) not null unique,
 route_pattern varchar(160) not null,
 target_selector varchar(200) not null,
 title varchar(120) not null,
 content varchar(400) not null,
 display_order integer not null default 0,
 is_active boolean not null default true,
 created_at timestamptz not null default now()
);
create table if not exists habitflow.user_product_tips(
 user_id uuid not null references habitflow.users(id) on delete cascade,
 product_tip_id uuid not null references habitflow.product_tips(id) on delete cascade,
 seen_at timestamptz null,
 dismissed_at timestamptz null,
 updated_at timestamptz not null default now(),
 primary key(user_id,product_tip_id)
);
create index if not exists ix_user_product_tips_pending on habitflow.user_product_tips(user_id,dismissed_at);

insert into habitflow.product_tips(id,code,route_pattern,target_selector,title,content,display_order) values
('62000000-0000-0000-0000-000000000001','dashboard','/dashboard%','#conteudo','Seu painel diário','Veja consistência, próximos passos e alertas do seu plano usando somente seus dados reais.',10),
('62000000-0000-0000-0000-000000000002','header','/%','[data-app-header]','Navegação rápida','Use a busca, o menu Novo e suas notificações sem sair do contexto atual.',20),
('62000000-0000-0000-0000-000000000003','my-day','/my-day%','#conteudo','Organize o seu dia','Conclua hábitos planejados e acompanhe o que ainda precisa da sua atenção.',30),
('62000000-0000-0000-0000-000000000004','progress','/progress%','#conteudo','Entenda seu progresso','Explore o calendário para reconhecer padrões de consistência ao longo do tempo.',40),
('62000000-0000-0000-0000-000000000005','reports','/reports%','#conteudo','Leia seus relatórios','Compare períodos e transforme seus resultados em próximos passos possíveis.',50),
('62000000-0000-0000-0000-000000000006','library','/habit-library%','#conteudo','Descubra hábitos','Revise cada sugestão antes de ativá-la e adapte-a à sua rotina.',60),
('62000000-0000-0000-0000-000000000007','plan','/account/plan%','#conteudo','Acompanhe seu plano','Confira limites e uso antes de decidir por qualquer alteração.',70),
('62000000-0000-0000-0000-000000000008','security','/account/security%','#conteudo','Proteja sua conta','Revise sessões, senha e autenticação para manter seus dados seguros.',80),
('62000000-0000-0000-0000-000000000009','privacy','/account/privacy%','#conteudo','Controle sua privacidade','Gerencie preferências e solicitações relacionadas aos seus dados.',90),
('62000000-0000-0000-0000-000000000010','notifications','/notifications%','#conteudo','Sua central de notificações','Filtre, leia e arquive avisos operacionais vinculados à sua conta.',100)
on conflict(code) do update set route_pattern=excluded.route_pattern,target_selector=excluded.target_selector,title=excluded.title,content=excluded.content,display_order=excluded.display_order;
-- END include database/migrations/062_product_tips.sql

-- BEGIN include database/migrations/063_account_privacy_center.sql
begin;
alter table habitflow.lgpd_requests drop constraint if exists ck_habitflow_lgpd_requests_type;
alter table habitflow.lgpd_requests add constraint ck_habitflow_lgpd_requests_type check (type in ('Export', 'Delete', 'Anonymize'));
create table if not exists habitflow.user_privacy_consents (user_id uuid not null references habitflow.users(id) on delete cascade,consent_key varchar(60) not null,granted boolean not null,updated_at timestamp not null default now(),primary key(user_id,consent_key),constraint ck_user_privacy_consents_key check(consent_key in ('analytics','communications')));
create table if not exists habitflow.privacy_request_events (id bigint generated always as identity primary key,request_id uuid not null references habitflow.lgpd_requests(id) on delete cascade,event_type varchar(40) not null,status varchar(50) not null,occurred_at timestamp not null default now());
create index if not exists ix_privacy_request_events_request on habitflow.privacy_request_events(request_id,occurred_at desc);
create or replace function habitflow.audit_privacy_request() returns trigger language plpgsql as $$ begin insert into habitflow.privacy_request_events(request_id,event_type,status) values(new.id,case when tg_op='INSERT' then 'requested' else 'status_changed' end,new.status);return new;end $$;
drop trigger if exists trg_audit_privacy_request on habitflow.lgpd_requests;
create trigger trg_audit_privacy_request after insert or update of status on habitflow.lgpd_requests for each row execute function habitflow.audit_privacy_request();
commit;
-- END include database/migrations/063_account_privacy_center.sql

-- BEGIN include database/migrations/064_v6121_commercial_plan_integrity.sql
-- v6.12.1: a public plan must not offer values for non-marketable capabilities.
BEGIN;
UPDATE habitflow.plan_features pf
SET bool_value = CASE WHEN f.value_type = 'Boolean' THEN false ELSE pf.bool_value END,
    int_value = CASE WHEN f.value_type = 'Integer' THEN null ELSE pf.int_value END,
    string_value = CASE WHEN f.value_type = 'String' THEN null ELSE pf.string_value END,
    updated_at = now()
FROM habitflow.feature_catalog f, habitflow.plans p
WHERE f.code = pf.feature_code
  AND p.id = pf.plan_id
  AND p.code IN ('free', 'ritmo')
  AND (f.implementation_status <> 'Implemented' OR NOT f.is_marketable);

UPDATE habitflow.plans
SET is_public = false, is_sellable = false, sales_status = 'Grandfathered'
WHERE code = 'evolucao';
COMMIT;
-- END include database/migrations/064_v6121_commercial_plan_integrity.sql

-- BEGIN include database/migrations/065_v6123_crud_contract_backfill.sql
-- v6.12.3: repair the persisted Habit/domain contract without rewriting historical migrations.
update habitflow.habits
set start_date = created_at::date
where start_date is null;

alter table habitflow.habits
    alter column start_date set default current_date,
    alter column start_date set not null;

create index if not exists ix_goal_habits_tenant_goal
    on habitflow.goal_habits(client_id, goal_id, habit_id);
-- END include database/migrations/065_v6123_crud_contract_backfill.sql

-- BEGIN include database/migrations/066_lgpd_privacy_schema_repair.sql
begin;

create table if not exists habitflow.user_privacy_consents (
    user_id uuid not null references habitflow.users(id) on delete cascade,
    consent_key varchar(60) not null,
    granted boolean not null,
    updated_at timestamp not null default now(),
    primary key (user_id, consent_key),
    constraint ck_user_privacy_consents_key
        check (consent_key in ('analytics', 'communications'))
);

create table if not exists habitflow.privacy_request_events (
    id bigint generated always as identity primary key,
    request_id uuid not null references habitflow.lgpd_requests(id) on delete cascade,
    event_type varchar(40) not null,
    status varchar(50) not null,
    occurred_at timestamp not null default now()
);

create index if not exists ix_privacy_request_events_request
    on habitflow.privacy_request_events(request_id, occurred_at desc);

create or replace function habitflow.audit_privacy_request()
returns trigger
language plpgsql
as $$
begin
    insert into habitflow.privacy_request_events(request_id, event_type, status)
    values (
        new.id,
        case when tg_op = 'INSERT' then 'requested' else 'status_changed' end,
        new.status
    );
    return new;
end
$$;

drop trigger if exists trg_audit_privacy_request on habitflow.lgpd_requests;

create trigger trg_audit_privacy_request
after insert or update of status on habitflow.lgpd_requests
for each row execute function habitflow.audit_privacy_request();

commit;
-- END include database/migrations/066_lgpd_privacy_schema_repair.sql

-- BEGIN include database/migrations/067_reminder_dispatch_runtime.sql
-- transaction-mode: runner-managed
alter table habitflow.habit_reminders add column if not exists locked_by varchar(160) null;
alter table habitflow.habit_reminders add column if not exists locked_until timestamptz null;
alter table habitflow.reminder_dispatches add column if not exists next_attempt_at timestamptz null;
alter table habitflow.reminder_dispatches add column if not exists locked_by varchar(160) null;
alter table habitflow.reminder_dispatches add column if not exists locked_until timestamptz null;
alter table habitflow.reminder_dispatches add column if not exists last_error_at timestamptz null;
alter table habitflow.reminder_dispatches add column if not exists correlation_id uuid null;
update habitflow.reminder_dispatches set correlation_id=gen_random_uuid() where correlation_id is null;
alter table habitflow.reminder_dispatches alter column correlation_id set not null;
create index if not exists ix_reminder_dispatch_retry on habitflow.reminder_dispatches(next_attempt_at,locked_until) where status in ('Pending','Retry','Processing');
create index if not exists ix_habit_reminder_lease on habitflow.habit_reminders(next_trigger_at,locked_until) where is_active;
-- END include database/migrations/067_reminder_dispatch_runtime.sql

-- BEGIN include database/migrations/068_reminder_runtime_integrity.sql
-- habitflow:transaction=runner
-- Reminder instants were historically stored as timestamp without time zone.  The
-- application has always written UTC, so preserve the wall-clock value while
-- making that contract explicit to PostgreSQL.
alter table habitflow.habit_reminders
  alter column next_trigger_at type timestamptz using next_trigger_at at time zone 'UTC',
  alter column last_triggered_at type timestamptz using last_triggered_at at time zone 'UTC';

create index if not exists ix_habit_reminders_dispatch_due
  on habitflow.habit_reminders(next_trigger_at, id)
  where is_active and next_trigger_at is not null;
create index if not exists ix_reminder_dispatches_lease_recovery
  on habitflow.reminder_dispatches(locked_until)
  where locked_until is not null and status in ('Pending','Processing','Retry');
-- END include database/migrations/068_reminder_runtime_integrity.sql

-- BEGIN include database/migrations/069_v6165_intelligent_onboarding_challenges.sql
-- habitflow:transaction=runner
-- HabitFlow v6.16.5: tenant-safe challenge foundation.
create table if not exists habitflow.user_challenges (
 id uuid primary key,
 client_id uuid not null references habitflow.clients(id),
 user_id uuid not null references habitflow.users(id),
 habit_id uuid not null references habitflow.habits(id),
 name varchar(160) not null,
 description varchar(320) not null,
 duration_days integer not null constraint ck_user_challenges_duration check(duration_days in (7,30,90)),
 start_date date not null,
 end_date date not null,
 status varchar(20) not null default 'Active' constraint ck_user_challenges_status check(status in ('Active','Completed','Abandoned','Expired')),
 created_at timestamptz not null default now(), updated_at timestamptz not null default now(), completed_at timestamptz,
 constraint ck_user_challenges_dates check(end_date=start_date+(duration_days-1))
);
create index if not exists ix_user_challenges_owner on habitflow.user_challenges(client_id,user_id,status,created_at desc);
create unique index if not exists ux_user_challenges_active_habit on habitflow.user_challenges(client_id,user_id,habit_id) where status='Active';

-- The feature catalogue is honest: the 7-day flow is available to Free, while
-- longer challenges are implemented but enforced as Premium by the backend.
insert into habitflow.feature_catalog(code,name,value_type,category,implementation_status,is_marketable)
values
 ('challenge_7_days','Desafio de 7 dias','Boolean','Desafios','Implemented',true),
 ('challenge_30_days','Desafio de 30 dias','Boolean','Desafios','Implemented',true),
 ('challenge_90_days','Desafio de 90 dias','Boolean','Desafios','Implemented',true)
on conflict(code) do update set name=excluded.name,value_type=excluded.value_type,category=excluded.category,implementation_status='Implemented',is_marketable=true;
insert into habitflow.plan_features(plan_id,feature_code,bool_value)
select p.id,f.code,(f.code='challenge_7_days' or p.code<>'free') from habitflow.plans p cross join habitflow.feature_catalog f
where f.code in ('challenge_7_days','challenge_30_days','challenge_90_days')
on conflict(plan_id,feature_code) do update set bool_value=excluded.bool_value,updated_at=now();
-- END include database/migrations/069_v6165_intelligent_onboarding_challenges.sql

-- BEGIN include database/migrations/070_v6166_premium_reports.sql
-- v6.16.6: tenant-safe, idempotent report snapshots.
begin;
alter table habitflow.user_reports add column if not exists client_id uuid null;
alter table habitflow.user_reports add column if not exists algorithm_version integer not null default 1;
update habitflow.user_reports r set client_id=u.client_id from habitflow.users u where r.user_id=u.id and r.client_id is null;
alter table habitflow.user_reports alter column client_id set not null;
create index if not exists ix_user_reports_tenant_owner_period on habitflow.user_reports(client_id,user_id,period_start desc);
create unique index if not exists ux_user_reports_snapshot_version on habitflow.user_reports(client_id,user_id,report_type,period_start,algorithm_version);
commit;
-- END include database/migrations/070_v6166_premium_reports.sql

-- BEGIN include database/migrations/071_v6167_secure_multitenant_billing.sql
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
-- END include database/migrations/071_v6167_secure_multitenant_billing.sql

-- BEGIN include database/migrations/072_v6168_pwa_push_offline.sql
-- HabitFlow v6.16.8: tenant-safe Web Push persistence and idempotent offline events.
begin;
create table if not exists habitflow.push_subscriptions (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 endpoint text not null, p256dh text not null, auth text not null, device_name varchar(80) not null,
 is_active boolean not null default true, created_at timestamptz not null default now(), last_seen_at timestamptz,
 unique(client_id,user_id,endpoint));
create index if not exists ix_push_subscriptions_owner_active on habitflow.push_subscriptions(client_id,user_id,is_active);

create table if not exists habitflow.notification_preferences (
 client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 push_enabled boolean not null default false, internal_enabled boolean not null default true,
 quiet_start time, quiet_end time, maximum_per_day integer not null default 5 check(maximum_per_day between 1 and 20),
 paused_until timestamptz, updated_at timestamptz not null default now(), primary key(client_id,user_id));

create table if not exists habitflow.push_delivery_attempts (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 subscription_id uuid not null references habitflow.push_subscriptions(id) on delete cascade,
 status varchar(20) not null check(status in ('Delivered','Failed')), error_code varchar(80), attempted_at timestamptz not null default now());
create index if not exists ix_push_attempts_owner_date on habitflow.push_delivery_attempts(client_id,user_id,attempted_at desc);

create table if not exists habitflow.offline_sync_events (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 action varchar(30) not null check(action in ('complete','undo','snooze')), entity_id uuid not null,
 status varchar(20) not null default 'Processed', created_at timestamptz not null default now(), expires_at timestamptz not null,
 unique(client_id,user_id,id));
create index if not exists ix_offline_sync_expiry on habitflow.offline_sync_events(expires_at);
commit;
-- END include database/migrations/072_v6168_pwa_push_offline.sql

-- BEGIN include database/migrations/073_v6169_secure_assistance_support.sql
-- HabitFlow v6.16.9: secure assistant, support contact and tenant-isolated tickets.
begin;
create table if not exists habitflow.assistant_conversations (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 created_at timestamptz not null default now(), updated_at timestamptz not null default now());
create index if not exists ix_assistant_conversations_owner on habitflow.assistant_conversations(client_id,user_id,updated_at desc);
create table if not exists habitflow.assistant_messages (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 conversation_id uuid not null references habitflow.assistant_conversations(id) on delete cascade,
 role varchar(20) not null check(role in ('user','assistant')), message text not null, sanitized_message text not null,
 safety_status varchar(30) not null, provider varchar(40) not null, created_at timestamptz not null default now(), correlation_id varchar(100) not null);
create index if not exists ix_assistant_messages_owner on habitflow.assistant_messages(client_id,user_id,conversation_id,created_at);
create table if not exists habitflow.assistant_feedback (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 message_id uuid not null references habitflow.assistant_messages(id) on delete cascade, helpful boolean not null, comment varchar(500), created_at timestamptz not null default now());
create index if not exists ix_assistant_feedback_owner on habitflow.assistant_feedback(client_id,user_id,created_at desc);

create table if not exists habitflow.support_settings (
 id uuid primary key, company_name varchar(120) not null, company_document varchar(30) not null,
 support_email varchar(254) not null, whatsapp_phone varchar(20), default_message varchar(500) not null,
 business_hours varchar(160) not null, is_active boolean not null default true, button_text varchar(80) not null, updated_at timestamptz not null default now());
insert into habitflow.support_settings(id,company_name,company_document,support_email,whatsapp_phone,default_message,business_hours,is_active,button_text)
values('61690000-0000-0000-0000-000000000001','MNSOFT','18.160.057/0001-13','comercial@mnsoft.com.br',null,'Olá! Preciso de ajuda com o HabitFlow.','Segunda a sexta, 9h às 18h',true,'Falar com a MNSOFT') on conflict(id) do nothing;

create table if not exists habitflow.support_tickets_v2 (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 protocol varchar(40) not null unique, category varchar(30) not null check(category in ('Usage','Technical','Subscription','Report','Notifications','Suggestion','Other')),
 status varchar(20) not null check(status in ('Open','InAnalysis','Responded','Closed')), subject varchar(160) not null,
 description text not null, safe_context varchar(1000) not null, created_at timestamptz not null default now(), updated_at timestamptz not null default now(), closed_at timestamptz);
create index if not exists ix_support_tickets_v2_owner on habitflow.support_tickets_v2(client_id,user_id,status,updated_at desc);
create table if not exists habitflow.support_ticket_messages_v2 (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), ticket_id uuid not null references habitflow.support_tickets_v2(id) on delete cascade,
 user_id uuid not null references habitflow.users(id), is_staff boolean not null default false, message text not null, created_at timestamptz not null default now());
create index if not exists ix_support_ticket_messages_v2_tenant on habitflow.support_ticket_messages_v2(client_id,ticket_id,created_at);
create table if not exists habitflow.support_ticket_events (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), ticket_id uuid not null references habitflow.support_tickets_v2(id) on delete cascade,
 actor_user_id uuid references habitflow.users(id), event_type varchar(40) not null, metadata jsonb not null default '{}', created_at timestamptz not null default now());
create index if not exists ix_support_ticket_events_tenant on habitflow.support_ticket_events(client_id,ticket_id,created_at);
commit;
-- END include database/migrations/073_v6169_secure_assistance_support.sql

-- BEGIN include database/migrations/074_v6170_saas_admin_lgpd.sql
-- HabitFlow v6.17.0: tenant-scoped SaaS administration, RBAC, audit, flags and LGPD.
begin;
set local search_path to habitflow, public;

create table if not exists habitflow.tenant_settings (
 client_id uuid primary key references habitflow.clients(id), slug varchar(80) not null,
 timezone varchar(80) not null default 'America/Sao_Paulo', language varchar(10) not null default 'pt-BR',
 logo_url varchar(500), theme varchar(40), support_email varchar(254), support_whatsapp varchar(24),
 retention_days integer not null default 730 check(retention_days between 30 and 3650), status varchar(20) not null default 'Active' check(status in ('Active','Suspended','Archived')),
 updated_by_user_id uuid references habitflow.users(id), updated_at timestamptz not null default now());
create unique index if not exists ux_tenant_settings_slug_lower on habitflow.tenant_settings(lower(slug));

alter table habitflow.roles add column if not exists created_at timestamptz not null default now();
insert into habitflow.roles(id,code,name,scope,description,is_system,is_active) values
 ('61700000-0000-0000-0000-000000000001','owner','Owner','Client','Controle total do tenant',true,true),
 ('61700000-0000-0000-0000-000000000002','admin','Admin','Client','Operação administrativa',true,true),
 ('61700000-0000-0000-0000-000000000003','support','Support','Client','Atendimento',true,true),
 ('61700000-0000-0000-0000-000000000004','billing_admin','BillingAdmin','Client','Cobrança',true,true),
 ('61700000-0000-0000-0000-000000000005','read_only','ReadOnly','Client','Consulta administrativa',true,true)
on conflict(code) do update set name=excluded.name,description=excluded.description;
insert into habitflow.permissions(code,name,category) select code,replace(code,'_',' '),split_part(code,'.',1) from unnest(array[
 'admin.dashboard.read','users.read','users.invite','users.update_role','users.disable','billing.read','billing.manage',
 'support.read','support.reply','audit.read','feature_flags.manage','privacy.manage','system_health.read']) code on conflict(code) do nothing;
insert into habitflow.role_permissions(role_id,permission_code)
select r.id,p.code from habitflow.roles r cross join habitflow.permissions p where
 r.code='owner' or
 (r.code='admin' and p.code <> 'billing.manage') or
 (r.code='support' and p.code in ('admin.dashboard.read','users.read','support.read','support.reply')) or
 (r.code='billing_admin' and p.code in ('admin.dashboard.read','billing.read','billing.manage')) or
 (r.code='read_only' and p.code in ('admin.dashboard.read','users.read','billing.read','support.read','audit.read','system_health.read'))
on conflict do nothing;

create table if not exists habitflow.user_invitations (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), email_normalized varchar(254) not null,
 token_hash varchar(128) not null unique, role_id uuid not null references habitflow.roles(id), invited_by_user_id uuid not null references habitflow.users(id),
 expires_at timestamptz not null, accepted_at timestamptz, accepted_by_user_id uuid references habitflow.users(id), revoked_at timestamptz,
 created_at timestamptz not null default now(), constraint ck_user_invitations_lifecycle check(accepted_at is null or revoked_at is null));
create index if not exists ix_user_invitations_tenant_email on habitflow.user_invitations(client_id,email_normalized,created_at desc);

create table if not exists habitflow.feature_flags (
 id uuid primary key, code varchar(100) not null, environment varchar(40) not null, client_id uuid references habitflow.clients(id), plan_code varchar(40),
 enabled boolean not null default false, starts_at timestamptz, ends_at timestamptz, updated_by_user_id uuid references habitflow.users(id),
 created_at timestamptz not null default now(), updated_at timestamptz not null default now(), check(ends_at is null or starts_at is null or ends_at>starts_at));
create unique index if not exists ux_feature_flags_scope on habitflow.feature_flags(code,environment,coalesce(client_id,'00000000-0000-0000-0000-000000000000'::uuid),coalesce(plan_code,''));

create table if not exists habitflow.audit_events (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), actor_user_id uuid references habitflow.users(id), target_user_id uuid references habitflow.users(id),
 action varchar(100) not null, resource_type varchar(80) not null, resource_id uuid, occurred_at timestamptz not null default now(), correlation_id varchar(100) not null,
 ip_hash varchar(128), user_agent_summary varchar(200), summary varchar(500) not null, before_data jsonb, after_data jsonb);
create index if not exists ix_audit_events_tenant_time on habitflow.audit_events(client_id,occurred_at desc);

create table if not exists habitflow.privacy_requests (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id), request_type varchar(30) not null check(request_type in ('Export','Deletion','Anonymization')),
 status varchar(30) not null default 'Requested' check(status in ('Requested','InProgress','Completed','Rejected','LegalHold')), legal_hold_reason varchar(300), requested_at timestamptz not null default now(), completed_at timestamptz);
create index if not exists ix_privacy_requests_owner on habitflow.privacy_requests(client_id,user_id,requested_at desc);
create table if not exists habitflow.consent_records (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id), purpose varchar(80) not null,
 document_version varchar(40) not null, granted boolean not null, recorded_at timestamptz not null default now(), source varchar(40) not null);
create index if not exists ix_consent_records_owner on habitflow.consent_records(client_id,user_id,recorded_at desc);
commit;
-- END include database/migrations/074_v6170_saas_admin_lgpd.sql

-- BEGIN include database/migrations/075_v6171_release_candidate_integrity.sql
-- HabitFlow v6.17.1: honest feature states and indexes for tenant-scoped hot paths.
begin;
set local search_path to habitflow, public;

alter table habitflow.notifications add column if not exists is_archived boolean not null default false;

-- Disabled is an explicit product state. Internal remains reserved for operational
-- capabilities which are real but must never be advertised as customer benefits.
update habitflow.feature_catalog
   set implementation_status = 'Disabled', is_marketable = false
 where not is_active;

do $catalog_contract$
begin
  if exists (select 1 from habitflow.feature_catalog
              where implementation_status not in ('Implemented','Partial','Planned','Disabled','Internal','Deprecated')) then
    raise exception 'feature_catalog contains an unsupported implementation status';
  end if;
  if exists (select 1 from habitflow.feature_catalog
              where is_marketable and implementation_status <> 'Implemented') then
    raise exception 'only implemented features may be marketable';
  end if;
end
$catalog_contract$;

create index if not exists ix_habit_completions_tenant_user_day
  on habitflow.habit_completions(client_id,user_id,completed_date desc);
create index if not exists ix_notifications_tenant_user_center
  on habitflow.notifications(client_id,user_id,is_archived,is_read,created_at desc);
create index if not exists ix_habits_tenant_user_active
  on habitflow.habits(client_id,user_id,created_at desc) where not is_archived;

commit;
-- END include database/migrations/075_v6171_release_candidate_integrity.sql

-- BEGIN include database/migrations/076_v6173_healthy_gamification.sql
-- HabitFlow v6.17.3: healthy, personal gamification (tenant-safe and additive).
begin;
set local search_path to habitflow, public;

create table if not exists weekly_goals (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 name varchar(120) not null, week_start date not null, week_end date not null,
 target_completions integer not null check(target_completions between 1 and 100), current_completions integer not null default 0,
 status varchar(20) not null default 'Active' check(status in ('Active','Paused','Completed','Closed')),
 created_at timestamptz not null default now(), completed_at timestamptz,
 check(week_end=week_start+6), unique(client_id,user_id,week_start,name)
);
create table if not exists weekly_goal_habits (
 client_id uuid not null, user_id uuid not null, weekly_goal_id uuid not null references weekly_goals(id) on delete cascade,
 habit_id uuid not null references habits(id), created_at timestamptz not null default now(),
 primary key(client_id,user_id,weekly_goal_id,habit_id)
);
create table if not exists achievement_definitions (
 code varchar(80) primary key, name varchar(120) not null, description varchar(260) not null, icon varchar(40) not null,
 criterion varchar(120) not null, category varchar(30) not null check(category in ('começo','consistência','retorno','desafio','foco','premium')),
 rarity varchar(20) not null check(rarity in ('comum','especial','rara')), is_active boolean not null default true
);
create table if not exists user_achievements (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 achievement_code varchar(80) not null references achievement_definitions(code), status varchar(20) not null default 'Unlocked',
 unlocked_at timestamptz not null default now(), unique(client_id,user_id,achievement_code)
);
create table if not exists user_missions (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 code varchar(80) not null, title varchar(140) not null, description varchar(260) not null, target integer not null check(target>0),
 progress integer not null default 0 check(progress>=0), status varchar(20) not null default 'Active' check(status in ('Active','Completed','Dismissed')),
 local_date date not null, completed_at timestamptz, created_at timestamptz not null default now(), unique(client_id,user_id,code,local_date)
);
create table if not exists streak_freezes (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id), habit_id uuid not null references habits(id),
 frozen_date date not null, reason varchar(160), created_at timestamptz not null default now(), unique(client_id,user_id,habit_id,frozen_date)
);
create table if not exists gamification_events (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 event_type varchar(80) not null, entity_type varchar(40), entity_id uuid, idempotency_key varchar(160) not null,
 occurred_at timestamptz not null default now(), metadata jsonb not null default '{}'::jsonb, unique(client_id,user_id,idempotency_key)
);
create index if not exists ix_weekly_goals_owner_week on weekly_goals(client_id,user_id,week_start desc);
create index if not exists ix_user_achievements_owner on user_achievements(client_id,user_id,unlocked_at desc);
create index if not exists ix_user_missions_owner_day on user_missions(client_id,user_id,local_date desc);

insert into achievement_definitions(code,name,description,icon,criterion,category,rarity) values
 ('first_habit','Primeiro passo','Você criou seu primeiro hábito.','sparkles','habits >= 1','começo','comum'),
 ('first_completion','Hoje conta','Você concluiu seu primeiro dia.','check-circle','completions >= 1','começo','comum'),
 ('consistency_3','Ritmo de 3 dias','Boa sequência esta semana.','flame','streak >= 3','consistência','comum'),
 ('consistency_7','Uma semana presente','Sete dias de passos consistentes.','calendar-check','streak >= 7','consistência','especial'),
 ('total_30','30 passos','Trinta conclusões construíram seu caminho.','footprints','completions >= 30','foco','especial'),
 ('challenge_started','Desafio aceito','Você iniciou seu primeiro desafio.','flag','challenges_started >= 1','desafio','comum'),
 ('challenge_completed','Desafio concluído','Você chegou ao fim do seu primeiro desafio.','trophy','challenges_completed >= 1','desafio','especial'),
 ('weekly_goal_completed','Semana no ritmo','Você alcançou sua primeira meta semanal.','target','weekly_goals_completed >= 1','foco','especial'),
 ('back_on_track','Ritmo retomado','Você retomou o ritmo após uma pausa.','refresh-cw','returned_after_pause','retorno','especial'),
 ('habit_30_days','Cuidado contínuo','Um hábito acompanhou você por 30 dias.','award','habit_age_days >= 30','consistência','rara')
on conflict(code) do update set name=excluded.name,description=excluded.description,icon=excluded.icon,criterion=excluded.criterion,category=excluded.category,rarity=excluded.rarity,is_active=true;

insert into feature_catalog(code,name,value_type,category,implementation_status,is_marketable) values
 ('weekly_goals','Metas semanais','Boolean','Progresso','Implemented',true),
 ('achievements','Conquistas','Boolean','Progresso','Implemented',true),
 ('advanced_achievements','Conquistas avançadas','Boolean','Progresso','Implemented',true),
 ('streak_freeze','Proteção de sequência','Boolean','Progresso','Implemented',true),
 ('missions','Missões pessoais','Boolean','Progresso','Implemented',true),
 ('progress_dashboard','Painel de progresso','Boolean','Progresso','Implemented',true)
on conflict(code) do update set name=excluded.name,value_type=excluded.value_type,category=excluded.category,implementation_status='Implemented',is_marketable=true,is_active=true;
insert into plan_features(plan_id,feature_code,bool_value)
select p.id,f.code,case when f.code in ('streak_freeze','advanced_achievements') then p.code<>'free' else true end
from plans p cross join feature_catalog f where f.code in ('weekly_goals','achievements','advanced_achievements','streak_freeze','missions','progress_dashboard')
on conflict(plan_id,feature_code) do update set bool_value=excluded.bool_value,updated_at=now();
commit;
-- END include database/migrations/076_v6173_healthy_gamification.sql

-- BEGIN include database/migrations/077_v6177_mobile_pwa_notification_preferences.sql
-- HabitFlow v6.17.7: preferências completas e trilha de entrega multicanal.
begin;
alter table habitflow.notification_preferences add column if not exists habit_reminders boolean not null default true;
alter table habitflow.notification_preferences add column if not exists daily_summary boolean not null default false;
alter table habitflow.notification_preferences add column if not exists weekly_summary boolean not null default true;
alter table habitflow.notification_preferences add column if not exists timezone varchar(80) not null default 'America/Sao_Paulo';
alter table habitflow.notification_preferences add column if not exists language varchar(10) not null default 'pt-BR';
alter table habitflow.notification_preferences drop constraint if exists ck_notification_preferences_language;
alter table habitflow.notification_preferences add constraint ck_notification_preferences_language check (language in ('pt-BR','en-US'));
alter table habitflow.notification_preferences drop constraint if exists ck_notification_preferences_quiet_period;
alter table habitflow.notification_preferences add constraint ck_notification_preferences_quiet_period check ((quiet_start is null and quiet_end is null) or (quiet_start is not null and quiet_end is not null and quiet_start <> quiet_end));
-- Revogação preserva a auditoria e os endpoints continuam isolados pelo par client/user.
alter table habitflow.push_subscriptions add column if not exists revoked_at timestamptz;
create index if not exists ix_push_subscriptions_tenant_user_active on habitflow.push_subscriptions(client_id,user_id) where is_active and revoked_at is null;
-- A entrega existente representa BrowserPush. A chave opcional permite idempotência por ocorrência.
alter table habitflow.push_delivery_attempts add column if not exists channel varchar(20) not null default 'BrowserPush';
alter table habitflow.push_delivery_attempts add column if not exists scheduled_for timestamptz;
alter table habitflow.push_delivery_attempts add column if not exists reminder_id uuid;
alter table habitflow.push_delivery_attempts drop constraint if exists ck_push_delivery_channel;
alter table habitflow.push_delivery_attempts add constraint ck_push_delivery_channel check (channel in ('InApp','BrowserPush'));
create unique index if not exists ux_push_attempt_delivery on habitflow.push_delivery_attempts(client_id,user_id,subscription_id,reminder_id,channel,scheduled_for) where reminder_id is not null and scheduled_for is not null;
commit;
-- END include database/migrations/077_v6177_mobile_pwa_notification_preferences.sql

-- BEGIN include database/migrations/078_v6179_real_billing_commercial.sql
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
-- END include database/migrations/078_v6179_real_billing_commercial.sql

-- BEGIN include database/migrations/079_v6180_corporate_programs.sql
-- HabitFlow v6.18.0 - corporate collaboration, privacy-first and tenant isolated.
begin;
create table if not exists habitflow.organization_members (
 client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 role varchar(30) not null check(role in ('Owner','Admin','TeamManager','Member','ReportReader')),
 is_active boolean not null default true, created_at timestamptz not null default now(), primary key(client_id,user_id));
create table if not exists habitflow.teams (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), name varchar(120) not null,
 description varchar(500), is_archived boolean not null default false, created_at timestamptz not null default now(), updated_at timestamptz not null default now(), unique(client_id,name));
create table if not exists habitflow.team_members (
 client_id uuid not null, team_id uuid not null, user_id uuid not null references habitflow.users(id), is_manager boolean not null default false,
 joined_at timestamptz not null default now(), primary key(client_id,team_id,user_id), foreign key(team_id) references habitflow.teams(id) on delete restrict);
create table if not exists habitflow.team_invitations (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), team_id uuid references habitflow.teams(id), email varchar(320) not null,
 role varchar(30) not null check(role in ('Admin','TeamManager','Member','ReportReader')), token_hash char(64) not null unique,
 status varchar(20) not null check(status in ('Pending','Accepted','Declined','Cancelled','Expired')), sent_at timestamptz not null,
 expires_at timestamptz not null, responded_at timestamptz, invited_by uuid not null references habitflow.users(id), check(expires_at>sent_at));
create table if not exists habitflow.corporate_programs (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), name varchar(160) not null, description varchar(1000) not null,
 objective varchar(500) not null, starts_on date not null, ends_on date not null, audience varchar(300) not null,
 status varchar(20) not null check(status in ('Draft','Active','Paused','Ended','Archived')), owner_user_id uuid not null references habitflow.users(id),
 allow_leaving boolean not null default true, created_at timestamptz not null default now(), updated_at timestamptz not null default now(), check(ends_on>=starts_on));
create table if not exists habitflow.corporate_program_teams (client_id uuid not null, program_id uuid not null references habitflow.corporate_programs(id), team_id uuid not null references habitflow.teams(id), primary key(client_id,program_id,team_id));
create table if not exists habitflow.corporate_program_habits (client_id uuid not null, program_id uuid not null references habitflow.corporate_programs(id), habit_template_id uuid not null references habitflow.habit_templates(id), is_optional boolean not null default true, primary key(client_id,program_id,habit_template_id), check(is_optional));
create table if not exists habitflow.corporate_program_members (client_id uuid not null, program_id uuid not null references habitflow.corporate_programs(id), user_id uuid not null references habitflow.users(id), joined_at timestamptz not null default now(), left_at timestamptz, consented_at timestamptz not null, primary key(client_id,program_id,user_id));
create table if not exists habitflow.team_challenges (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), team_id uuid not null references habitflow.teams(id), program_id uuid references habitflow.corporate_programs(id),
 name varchar(160) not null, goal varchar(500) not null, starts_on date not null, ends_on date not null, target integer not null check(target>0), is_collective boolean not null,
 ranking_enabled boolean not null default false, status varchar(20) not null check(status in ('Draft','Active','Finished','Cancelled','Archived')), created_at timestamptz not null default now(), check(ends_on>=starts_on));
create table if not exists habitflow.team_challenge_progress (client_id uuid not null, challenge_id uuid not null references habitflow.team_challenges(id), user_id uuid not null references habitflow.users(id), progress integer not null default 0 check(progress>=0), opted_in boolean not null default false, updated_at timestamptz not null default now(), primary key(client_id,challenge_id,user_id));
create table if not exists habitflow.privacy_preferences (client_id uuid not null, user_id uuid not null references habitflow.users(id), habits_private boolean not null default true, share_program_progress boolean not null default false, updated_at timestamptz not null default now(), primary key(client_id,user_id));
create index if not exists ix_teams_tenant_status on habitflow.teams(client_id,is_archived);
create index if not exists ix_team_members_tenant_team on habitflow.team_members(client_id,team_id);
create index if not exists ix_invitations_tenant_status_expiry on habitflow.team_invitations(client_id,status,expires_at);
create index if not exists ix_programs_tenant_status_period on habitflow.corporate_programs(client_id,status,starts_on,ends_on);
create index if not exists ix_challenges_tenant_team_status on habitflow.team_challenges(client_id,team_id,status);
commit;
-- END include database/migrations/079_v6180_corporate_programs.sql

-- BEGIN include database/migrations/080_v6182_safe_contextual_assistant.sql
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
-- END include database/migrations/080_v6182_safe_contextual_assistant.sql

-- BEGIN include database/migrations/081_v6188_premium_gamification.sql
-- HabitFlow v6.18.8: pontos verificáveis e ranking explicitamente opt-in.
set search_path to habitflow, public;
create table if not exists gamification_points_ledger(
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 source_type varchar(30) not null check(source_type in('completion','routine','consistency','reversal')),
 source_id uuid not null, points integer not null check(points between -100 and 100), local_date date not null,
 occurred_at timestamptz not null, idempotency_key varchar(160) not null,
 unique(client_id,user_id,idempotency_key)
);
create index if not exists ix_gamification_points_owner_period on gamification_points_ledger(client_id,user_id,local_date desc);
create table if not exists gamification_leaderboard_preferences(
 client_id uuid not null references clients(id), user_id uuid not null references users(id), is_opted_in boolean not null default false,
 scope varchar(20) not null default 'Private' check(scope in('Private','Team','General')), public_name varchar(40) not null,
 team_id uuid null references teams(id), updated_at timestamptz not null default now(), primary key(client_id,user_id),
 check(is_opted_in or scope='Private')
);
create index if not exists ix_gamification_leaderboard_visible on gamification_leaderboard_preferences(client_id,scope,team_id) where is_opted_in;
insert into achievement_definitions(code,name,description,icon,criterion,category,rarity) values
 ('first_habit','Primeiro passo','Você criou seu primeiro hábito ativo.','sparkles','first_active_habit','começo','comum'),
 ('consistency_30','Presença de 30 dias','Trinta dias de presença, respeitando pausas.','calendar','streak_30','consistência','especial'),
 ('routine_completed','Rotina completa','Você concluiu uma rotina real.','check-circle','routine_completed','premium','especial'),
 ('consistent_week','Semana consistente','Uma semana saudável e consistente.','sun','consistent_week','consistência','especial'),
 ('return_after_pause','Bom retorno','Você voltou depois de uma pausa, sem punição.','heart','return_after_pause','retorno','comum'),
 ('template_used','Começo guiado','Você iniciou um hábito usando um template.','layout','template_used','começo','comum')
on conflict(code) do update set name=excluded.name,description=excluded.description,criterion=excluded.criterion,is_active=true;
-- END include database/migrations/081_v6188_premium_gamification.sql

-- BEGIN include database/migrations/082_v6190_security_lgpd_hardening.sql
-- HabitFlow v6.19.0: LGPD governance, immutable consent history and tenant-safe portability.
begin;
set local search_path to habitflow, public;

create table if not exists security_audit_events (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid references users(id),
 event_type varchar(100) not null, severity varchar(20) not null check(severity in ('Info','Warning','Critical')),
 correlation_id varchar(100), sanitized_details jsonb not null default '{}'::jsonb,
 occurred_at timestamptz not null default now());
create index if not exists ix_security_audit_tenant_type_time on security_audit_events(client_id,event_type,occurred_at desc);
create index if not exists ix_security_audit_tenant_severity_time on security_audit_events(client_id,severity,occurred_at desc);

create table if not exists user_consent_history (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 consent_key varchar(60) not null check(consent_key in ('terms','privacy','analytics','notifications','assistant_context')),
 document_version varchar(40) not null, granted boolean not null, occurred_at timestamptz not null default now());
create index if not exists ix_consent_history_owner_purpose_time on user_consent_history(client_id,user_id,consent_key,occurred_at desc);

create table if not exists data_exports (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 format varchar(10) not null check(format in ('JSON','CSV')), status varchar(20) not null check(status in ('Requested','Processing','Completed','Failed','Expired')),
 storage_key varchar(300), expires_at timestamptz, created_at timestamptz not null default now(), completed_at timestamptz,
 check(storage_key is null or storage_key not like '%..%'));
create index if not exists ix_data_exports_owner_status_time on data_exports(client_id,user_id,status,created_at desc);

create table if not exists account_deletion_requests (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 status varchar(20) not null check(status in ('Requested','Confirmed','Processing','Completed','Canceled','Failed')),
 confirmation_token_hash varchar(128), requested_at timestamptz not null default now(), confirmed_at timestamptz,
 processing_started_at timestamptz, completed_at timestamptz, canceled_at timestamptz,
 failure_code varchar(80));
create unique index if not exists ux_account_deletion_active on account_deletion_requests(client_id,user_id) where status in ('Requested','Confirmed','Processing');
create index if not exists ix_account_deletion_tenant_status_time on account_deletion_requests(client_id,status,requested_at desc);

-- SECURITY DEFINER is intentionally not used: the caller retains the application's DB privileges.
-- Every branch is anchored to both tenant and user. Secrets, auth/session and billing tables are excluded.
create or replace function export_user_data_json(p_client_id uuid, p_user_id uuid)
returns jsonb language sql stable as $$
 select case when exists(select 1 from users u where u.id=p_user_id and u.client_id=p_client_id) then jsonb_build_object(
  'schemaVersion','6.19.0','exportedAtUtc',now(),
  'profile',(select to_jsonb(x) from (select u.id,u.name,u.email,u.created_at,u.accepted_terms_at,u.accepted_privacy_at from users u where u.id=p_user_id and u.client_id=p_client_id) x),
  'habits',coalesce((select jsonb_agg(to_jsonb(x)) from (select h.id,h.name,h.category,h.is_archived,h.created_at,h.updated_at from habits h where h.user_id=p_user_id and h.client_id=p_client_id order by h.created_at) x),'[]'::jsonb),
  'goals',coalesce((select jsonb_agg(to_jsonb(x)) from (select g.id,g.name,g.week_start,g.week_end,g.target_completions,g.current_completions,g.status,g.created_at from weekly_goals g where g.client_id=p_client_id and g.user_id=p_user_id order by g.created_at) x),'[]'::jsonb),
  'routines',coalesce((select jsonb_agg(to_jsonb(x)) from (select r.id,r.habit_id,r.local_date,r.preferred_time,r.sort_order,r.created_at,r.updated_at from daily_routine_overrides r where r.client_id=p_client_id and r.user_id=p_user_id order by r.local_date,r.sort_order) x),'[]'::jsonb),
  'completions',coalesce((select jsonb_agg(to_jsonb(x)) from (select c.id,c.habit_id,c.completed_date,c.created_at from habit_completions c join habits h on h.id=c.habit_id where c.user_id=p_user_id and h.client_id=p_client_id order by c.completed_date) x),'[]'::jsonb),
  'preferences',coalesce((select jsonb_agg(to_jsonb(x)) from (select p.habits_private,p.share_program_progress,p.updated_at from privacy_preferences p where p.client_id=p_client_id and p.user_id=p_user_id) x),'[]'::jsonb),
  'notifications',coalesce((select jsonb_agg(to_jsonb(x)) from (select n.id,n.type,n.title,n.is_read,n.created_at,n.read_at from notifications n join users u on u.id=n.user_id where n.user_id=p_user_id and u.client_id=p_client_id order by n.created_at) x),'[]'::jsonb),
  'achievements',coalesce((select jsonb_agg(to_jsonb(x)) from (select a.achievement_code,a.status,a.unlocked_at from user_achievements a where a.client_id=p_client_id and a.user_id=p_user_id order by a.unlocked_at) x),'[]'::jsonb),
  'consents',coalesce((select jsonb_agg(to_jsonb(x)) from (select c.consent_key,c.granted,c.updated_at from user_privacy_consents c join users u on u.id=c.user_id where c.user_id=p_user_id and u.client_id=p_client_id order by c.consent_key) x),'[]'::jsonb)
 ) else '{}'::jsonb end;
$$;
commit;
-- END include database/migrations/082_v6190_security_lgpd_hardening.sql

-- BEGIN include database/migrations/083_v6191_public_integrations.sql
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
-- END include database/migrations/083_v6191_public_integrations.sql

-- BEGIN include database/migrations/084_v6192_superadmin_tenant_governance.sql
-- HabitFlow v6.19.2 - additive SaaS tenant governance. Safe to re-run.
begin;
create schema if not exists habitflow;

alter table if exists habitflow.users drop constraint if exists ck_habitflow_users_role;
alter table if exists habitflow.users add constraint ck_habitflow_users_role check(role in
 ('User','Admin','SuperAdmin','ReadOnly','Manager','TenantAdmin','TenantOwner','BillingAdmin'));
alter table if exists habitflow.user_invites drop constraint if exists ck_habitflow_user_invites_role;
alter table if exists habitflow.user_invites add constraint ck_habitflow_user_invites_role check(role in
 ('User','Admin','ReadOnly','Manager','TenantAdmin','TenantOwner','BillingAdmin'));

create table if not exists habitflow.tenant_modules (
 tenant_id uuid not null references habitflow.clients(id), module_code varchar(40) not null,
 enabled boolean not null default true, blocked_reason varchar(500), updated_by uuid references habitflow.users(id),
 created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 primary key (tenant_id,module_code), constraint ck_tenant_module_code check(module_code in
 ('habits','goals','routines','calendar','notifications','analytics','gamification','assistant','teams','integrations','billing','support')),
 constraint ck_tenant_module_reason check(enabled or nullif(btrim(blocked_reason),'') is not null));

create table if not exists habitflow.tenant_manual_charges (
 id uuid primary key, tenant_id uuid not null references habitflow.clients(id), amount numeric(12,2) not null,
 due_date date not null, description varchar(240) not null, reason varchar(500) not null,
 status varchar(20) not null default 'Pending', approved_at timestamptz, created_by uuid not null references habitflow.users(id),
 approved_by uuid references habitflow.users(id), created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 constraint ck_manual_charge_amount check(amount > 0), constraint ck_manual_charge_reason check(nullif(btrim(reason),'') is not null),
 constraint ck_manual_charge_status check(status in ('Pending','Approved','Canceled','Overdue')),
 constraint ck_manual_charge_approval check((status='Approved')=(approved_at is not null and approved_by is not null)));

create table if not exists habitflow.tenant_access_audit (
 id uuid primary key, tenant_id uuid references habitflow.clients(id), user_id uuid references habitflow.users(id),
 actor_user_id uuid not null references habitflow.users(id), event_code varchar(80) not null, reason varchar(500),
 correlation_id varchar(100), metadata_json jsonb not null default '{}'::jsonb, created_at timestamptz not null default now(),
 constraint ck_tenant_audit_event check(event_code in ('tenant.created','tenant.updated','tenant.blocked','tenant.unblocked',
 'tenant.module_enabled','tenant.module_disabled','tenant.user_created','tenant.user_blocked','tenant.role_changed',
 'billing.manual_charge_created','billing.manual_payment_approved','superadmin.tenant_accessed','login.document_attempted')));

create table if not exists habitflow.user_documents (
 id uuid primary key, user_id uuid not null references habitflow.users(id), tenant_id uuid not null references habitflow.clients(id),
 document_type varchar(4) not null, document_normalized varchar(14) not null, enabled_for_login boolean not null default false,
 created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 constraint ck_user_document_type check((document_type='CPF' and length(document_normalized)=11) or (document_type='CNPJ' and length(document_normalized)=14)),
 constraint ck_user_document_digits check(document_normalized ~ '^[0-9]+$'), unique(user_id,tenant_id,document_type));

create index if not exists ix_tenant_modules_status on habitflow.tenant_modules(tenant_id,enabled,module_code);
create index if not exists ix_manual_charges_tenant_status on habitflow.tenant_manual_charges(tenant_id,status,due_date desc);
create index if not exists ix_tenant_access_audit_tenant_event on habitflow.tenant_access_audit(tenant_id,event_code,created_at desc);
create index if not exists ix_tenant_access_audit_actor on habitflow.tenant_access_audit(actor_user_id,created_at desc);
create unique index if not exists ux_user_documents_login on habitflow.user_documents(document_normalized,tenant_id) where enabled_for_login;
commit;
-- END include database/migrations/084_v6192_superadmin_tenant_governance.sql

-- BEGIN include database/migrations/085_v6194_operations_center.sql
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
-- END include database/migrations/085_v6194_operations_center.sql

-- BEGIN include database/migrations/086_v6195_superadmin_bootstrap.sql
-- HabitFlow v6.19.5: estrutura idempotente; a senha é criada somente pelo hasher BCrypt da aplicação.
begin;
insert into habitflow.clients(id,name,legal_name,document,email,plan,status,is_active,created_at,updated_at,person_type,document_type,document_raw,document_normalized,trade_name)
values('61950000-0000-4000-8000-000000000001','MNSOFT','MNSOFT','18160057000113','comercial@mnsoft.com.br','Enterprise','Active',true,now(),now(),'LegalPerson','CNPJ','18.160.057/0001-13','18160057000113','MNSOFT')
on conflict(id) do update set name='MNSOFT',legal_name='MNSOFT',is_active=true,updated_at=now();

insert into habitflow.permissions(code,name,description,category) values
('Platform.Health.View','Saúde do sistema','Visualização da saúde global','Platform'),
('Platform.Tenants.Block','Bloqueio de clientes','Bloqueio e desbloqueio auditado','Platform')
on conflict(code) do nothing;
insert into habitflow.role_permissions(role_id,permission_code)
select r.id,p.code from habitflow.roles r cross join habitflow.permissions p
where r.code='super_admin' and p.code like 'Platform.%' on conflict do nothing;
insert into habitflow.schema_migrations(id,name,applied_at) values('086','v6195_superadmin_bootstrap',now()) on conflict(id) do nothing;
commit;
-- END include database/migrations/086_v6195_superadmin_bootstrap.sql

-- BEGIN include database/migrations/087_v6196_ai_multi_provider_assistant.sql
-- HabitFlow v6.19.6: multi-provider AI assistant + public feature flags.
BEGIN;
SET LOCAL search_path TO habitflow, public;

ALTER TABLE habitflow.feature_catalog
  ADD COLUMN IF NOT EXISTS is_public boolean NOT NULL DEFAULT true;

-- Internal capabilities exist but are not advertised as public benefits.
UPDATE habitflow.feature_catalog
SET is_public = false
WHERE implementation_status = 'Internal' AND is_public;

INSERT INTO habitflow.feature_catalog(code,name,description,value_type,category,is_active,implementation_status,is_marketable,is_public)
VALUES
 ('ai_assistant','Assistente IA','Chat orientado ao HabitFlow com respostas seguras e isolamento multi-tenant.','Boolean','IA',true,'Implemented',true,true)
ON CONFLICT(code) DO UPDATE SET
 name=EXCLUDED.name,
 description=EXCLUDED.description,
 value_type=EXCLUDED.value_type,
 category=EXCLUDED.category,
 is_active=true,
 implementation_status='Implemented',
 is_marketable=true,
 is_public=true;

INSERT INTO habitflow.plan_features(plan_id,feature_code,bool_value)
SELECT p.id,'ai_assistant',(p.code<>'free')
FROM habitflow.plans p
WHERE p.code IN ('free','ritmo','evolucao')
ON CONFLICT(plan_id,feature_code) DO UPDATE SET
 bool_value=EXCLUDED.bool_value,
 updated_at=now();

COMMIT;
-- END include database/migrations/087_v6196_ai_multi_provider_assistant.sql

-- BEGIN include database/migrations/088_v6193_support_center.sql
-- HabitFlow v6.19.3: atendimento SaaS, SLA e auditoria. Idempotente.
begin;
alter table habitflow.support_tickets_v2 add column if not exists priority varchar(12) not null default 'Medium';
alter table habitflow.support_tickets_v2 add column if not exists assigned_user_id uuid references habitflow.users(id);
alter table habitflow.support_tickets_v2 add column if not exists sla_due_at timestamptz;
update habitflow.support_tickets_v2 set sla_due_at=created_at+interval '48 hours' where sla_due_at is null;
alter table habitflow.support_tickets_v2 alter column sla_due_at set not null;
alter table habitflow.support_tickets_v2 drop constraint if exists support_tickets_v2_category_check;
alter table habitflow.support_tickets_v2 add constraint support_tickets_v2_category_check check(category in ('Question','Error','Billing','Access','Configuration','Suggestion','Commercial')) not valid;
alter table habitflow.support_tickets_v2 drop constraint if exists support_tickets_v2_status_check;
alter table habitflow.support_tickets_v2 add constraint support_tickets_v2_status_check check(status in ('Open','InAnalysis','WaitingCustomer','WaitingMnsoft','Resolved','Closed','Cancelled')) not valid;
alter table habitflow.support_tickets_v2 drop constraint if exists support_tickets_v2_priority_check;
alter table habitflow.support_tickets_v2 add constraint support_tickets_v2_priority_check check(priority in ('Low','Medium','High','Critical'));
create index if not exists ix_support_tickets_v2_queue on habitflow.support_tickets_v2(client_id,status,priority,created_at desc);
create index if not exists ix_support_tickets_v2_sla on habitflow.support_tickets_v2(status,sla_due_at) where status not in ('Closed','Cancelled');
alter table habitflow.support_ticket_messages_v2 add column if not exists is_internal boolean not null default false;
alter table habitflow.support_ticket_messages_v2 add constraint support_ticket_message_not_blank check(length(btrim(message))>0) not valid;
create table if not exists habitflow.support_ticket_status_history(id uuid primary key,client_id uuid not null references habitflow.clients(id),ticket_id uuid not null references habitflow.support_tickets_v2(id) on delete cascade,actor_user_id uuid references habitflow.users(id),from_status varchar(20) not null,to_status varchar(20) not null,reason varchar(1000),created_at timestamptz not null default now());
create index if not exists ix_support_status_history_tenant on habitflow.support_ticket_status_history(client_id,ticket_id,created_at);
create table if not exists habitflow.support_sla_rules(priority varchar(12) primary key,business_hours integer not null check(business_hours between 1 and 720),updated_at timestamptz not null default now());
insert into habitflow.support_sla_rules(priority,business_hours) values('Low',72),('Medium',48),('High',24),('Critical',8) on conflict(priority) do nothing;
commit;
-- END include database/migrations/088_v6193_support_center.sql

-- BEGIN include database/migrations/089_v6196_saas_team_enterprise_catalog.sql
-- HabitFlow v6.19.6: SaaS vendável — catálogo Team/Enterprise, estados comerciais completos, tenant MNSOFT.
-- Somente aditivo/idempotente; nunca remete valores já existentes.
BEGIN;
SET LOCAL search_path TO habitflow, public;

-- 1) Estados de assinatura: amplia o conjunto aceito (mantém todos os valores antigos).
ALTER TABLE habitflow.subscriptions DROP CONSTRAINT IF EXISTS ck_habitflow_subscriptions_status;
ALTER TABLE habitflow.subscriptions ADD CONSTRAINT ck_habitflow_subscriptions_status
 CHECK(status in ('Pending','PaymentPending','Active','Trial','Trialing','PastDue','Canceled','Expired','ManualReview','Suspended','Failed','Inactive'));

-- 2) Estados comerciais do cliente: adiciona PaymentPending/ManualReview.
ALTER TABLE habitflow.clients DROP CONSTRAINT IF EXISTS ck_habitflow_clients_subscription_status;
ALTER TABLE habitflow.clients ADD CONSTRAINT ck_habitflow_clients_subscription_status
 CHECK(subscription_status in ('Free','Trial','Active','PaymentPending','PastDue','Canceled','Suspended','ManualReview'));
CREATE INDEX IF NOT EXISTS ix_clients_subscription_status ON habitflow.clients(subscription_status);

-- 3) Novas features (controladas pelo backend; não divulgadas como benefício público sem implementação).
INSERT INTO habitflow.feature_catalog(code,name,description,value_type,category,is_active,implementation_status,is_marketable,is_public)
VALUES
 ('teams','Gestão de times','Pessoas, papéis e convites da conta.','Boolean','Conta',true,'Implemented',false,false),
 ('corporate_features','Recursos corporativos','SSO, integrações e contrato corporativo sob negociação.','Boolean','Corporativo',true,'Planned',false,false)
ON CONFLICT(code) DO UPDATE SET
 name=EXCLUDED.name,
 description=EXCLUDED.description,
 value_type=EXCLUDED.value_type,
 category=EXCLUDED.category,
 is_active=true,
 implementation_status=EXCLUDED.implementation_status,
 is_marketable=EXCLUDED.is_marketable,
 is_public=EXCLUDED.is_public;

-- 4) Planos Team e Enterprise (códigos estáveis). Enterprise é venda por contato comercial.
INSERT INTO habitflow.plans(id,code,name,public_name,headline,description,audience_text,badge_text,is_active,is_public,is_sellable,sales_status,is_featured,sort_order,created_at,updated_at)
VALUES
 ('10000000-0000-0000-0000-000000000004','team','Team','Team','Para equipes que organizam rotinas em conjunto.','Hábitos, objetivos, desafios e assistente IA para o time inteiro, com gestão de pessoas.','Equipes pequenas e médias.','Equipe',true,true,true,'Available',false,40,now(),now()),
 ('10000000-0000-0000-0000-000000000005','enterprise','Enterprise','Enterprise','Atendimento corporativo dedicado.','Plataforma completa para organizações, com recursos corporativos e suporte prioritário.','Organizações e contas corporativas.','Sob contrato',true,true,true,'Contact',false,50,now(),now())
ON CONFLICT(code) DO UPDATE SET
 name=EXCLUDED.name,
 public_name=EXCLUDED.public_name,
 headline=EXCLUDED.headline,
 description=EXCLUDED.description,
 audience_text=EXCLUDED.audience_text,
 badge_text=EXCLUDED.badge_text,
 is_active=true,
 is_public=true,
 is_sellable=true,
 sales_status=EXCLUDED.sales_status,
 sort_order=EXCLUDED.sort_order,
 updated_at=now();

-- 5) Preços reais do Team (anual com economia real: 12 x R$ 79,90 = R$ 958,80 -> R$ 799,00, ~17% off).
INSERT INTO habitflow.plan_prices(id,plan_id,billing_cycle,amount,currency,valid_from)
SELECT v.id,p.id,v.cycle,v.amount,'BRL',timestamp '2026-01-01' FROM (VALUES
 ('20000000-0000-0000-0000-000000000007'::uuid,'team','Monthly',79.90),
 ('20000000-0000-0000-0000-000000000008'::uuid,'team','Yearly',799.00)) v(id,code,cycle,amount)
JOIN habitflow.plans p ON p.code=v.code
ON CONFLICT DO NOTHING;

-- 6) Recursos por plano (apenas o que o backend realmente aplica; sem promessa sem regra).
-- Somente features com implementation_status 'Implemented' entram como promessa publica;
-- Partial/Planned (advanced_reports, shared_routines, shared_goals, reminders_per_habit,
-- consolidated_reports, priority_support) entram aqui quando forem implementadas.
INSERT INTO habitflow.plan_features(plan_id,feature_code,bool_value,int_value)
SELECT p.id,f.code,
 CASE WHEN f.value_type='Boolean' THEN f.code IN
  ('full_habit_library','custom_categories','basic_reports','report_export_csv','report_print','full_history',
   'weekly_goals','achievements','advanced_achievements','streak_freeze','missions','progress_dashboard',
   'challenge_7_days','challenge_30_days','challenge_90_days','ai_assistant','teams','user_invitations','client_admin_dashboard')
 END,
 CASE WHEN f.value_type='Integer' THEN CASE f.code
   WHEN 'users_limit' THEN CASE WHEN p.code='team' THEN 10 ELSE -1 END
   WHEN 'active_habits_limit' THEN -1
   WHEN 'active_goals_limit' THEN -1
   WHEN 'history_days_limit' THEN -1 END END
FROM habitflow.plans p CROSS JOIN habitflow.feature_catalog f
WHERE p.code IN ('team','enterprise')
ON CONFLICT(plan_id,feature_code) DO UPDATE SET bool_value=EXCLUDED.bool_value,int_value=EXCLUDED.int_value,updated_at=now();

-- 7) Tenant MNSOFT (plataforma) passa a operar no plano Enterprise.
UPDATE habitflow.clients c
SET contracted_plan_code='enterprise', effective_plan_code='enterprise', access_restriction_reason=NULL, updated_at=now()
WHERE c.document_normalized='18160057000113' AND coalesce(c.contracted_plan_code,'free') IN ('free');

-- 8) Sinalização por tenant/módulo (alimenta os claims tenant_status/tenant_module do cookie de sessão).
CREATE TABLE IF NOT EXISTS habitflow.tenant_feature_flags(
 client_id uuid not null references habitflow.clients(id) on delete cascade,
 module varchar(40) not null,
 enabled boolean not null default true,
 updated_by uuid null references habitflow.users(id) on delete set null,
 updated_at timestamptz not null default now(),
 primary key(client_id,module));

-- 9) Histórico de mudanças de status comercial do tenant (auditoria imutável).
CREATE TABLE IF NOT EXISTS habitflow.tenant_commercial_status(
 id uuid primary key default gen_random_uuid(),
 client_id uuid not null references habitflow.clients(id) on delete cascade,
 previous_status varchar(32) not null,
 new_status varchar(32) not null,
 reason varchar(240) null,
 changed_by uuid null references habitflow.users(id) on delete set null,
 changed_at timestamptz not null default now());
CREATE INDEX IF NOT EXISTS ix_tenant_commercial_status_client_date ON habitflow.tenant_commercial_status(client_id, changed_at desc);

COMMIT;
-- END include database/migrations/089_v6196_saas_team_enterprise_catalog.sql

-- BEGIN include database/migrations/090_atomic_account_invites_capacity.sql
-- 090: Keep pending invite reservations unique per tenant and email.
-- Resolve expired rows first; stop visibly if pre-existing live duplicates need review.
BEGIN;

UPDATE habitflow.user_invites
SET status = 'Expired', updated_at = now()
WHERE status = 'Pending' AND expires_at <= now();

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM habitflow.user_invites
        WHERE status = 'Pending'
        GROUP BY client_id, lower(trim(email))
        HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION 'Migration 090 blocked: duplicate pending invites exist for the same tenant and email; reconcile them before retrying.';
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_habitflow_user_invites_pending_client_email
    ON habitflow.user_invites(client_id, lower(trim(email)))
    WHERE status = 'Pending';

CREATE INDEX IF NOT EXISTS ix_habitflow_user_invites_client_status_expiry
    ON habitflow.user_invites(client_id, status, expires_at);

COMMIT;
-- END include database/migrations/090_atomic_account_invites_capacity.sql

-- BEGIN include database/migrations/091_account_people_join_date.sql
-- 091: Preserve the actual tenant-join date separately from the user's signup date.
BEGIN;

ALTER TABLE habitflow.users
    ADD COLUMN IF NOT EXISTS client_joined_at timestamp NULL;

UPDATE habitflow.users
SET client_joined_at = created_at
WHERE client_id IS NOT NULL AND client_joined_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_habitflow_users_client_joined_at
    ON habitflow.users(client_id, client_joined_at DESC);

COMMIT;
-- END include database/migrations/091_account_people_join_date.sql

-- BEGIN include database/migrations/092_market_implemented_user_invites.sql
-- 092: Make the existing invitation entitlement enforceable and expose it to backend feature checks.
BEGIN;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM (VALUES ('user_invitations'), ('users_limit')) req(code)
        WHERE NOT EXISTS (SELECT 1 FROM habitflow.feature_catalog f WHERE f.code = req.code)
    ) THEN
        RAISE EXCEPTION 'Migration 092 blocked: invitation or seat-limit feature seed is missing; restore the feature seeds before retrying.';
    END IF;
END $$;

UPDATE habitflow.feature_catalog
SET implementation_status = 'Implemented',
    is_marketable = true,
    is_public = false
WHERE code IN ('user_invitations', 'users_limit');

INSERT INTO habitflow.plan_features(plan_id, feature_code, int_value)
SELECT p.id, 'users_limit', 1
FROM habitflow.plans p
WHERE p.code IN ('free', 'ritmo')
ON CONFLICT(plan_id, feature_code) DO UPDATE
SET int_value = EXCLUDED.int_value,
    updated_at = now();

COMMIT;
-- END include database/migrations/092_market_implemented_user_invites.sql

-- BEGIN include database/migrations/093_restore_schema_contract_constraints.sql
-- 093: Restore named schema contracts missing from the ordered migration set.
BEGIN;

DO $$
DECLARE
    existing_constraint text;
BEGIN
    IF EXISTS (
        SELECT 1
        FROM habitflow.users
        WHERE role NOT IN ('User', 'Admin', 'SuperAdmin', 'ReadOnly', 'Manager', 'TenantAdmin', 'TenantOwner', 'BillingAdmin')
           OR account_status NOT IN ('Active', 'Blocked', 'Suspended', 'DeletedPending')
           OR risk_status NOT IN ('Normal', 'Watchlist', 'Suspicious')
           OR plan NOT IN ('Free', 'Premium')
           OR plan_status NOT IN ('Active', 'Trial', 'Canceled', 'Inactive', 'PastDue')
    ) THEN
        RAISE EXCEPTION 'Migration 093 blocked: existing users contain values outside the supported role, account, risk, plan, or plan-status contracts.';
    END IF;

    IF EXISTS (
        SELECT 1 FROM habitflow.support_tickets
        WHERE status NOT IN ('Open', 'InProgress', 'Resolved', 'Closed')
    ) THEN
        RAISE EXCEPTION 'Migration 093 blocked: existing support tickets contain an unsupported status.';
    END IF;

    IF EXISTS (
        SELECT 1 FROM habitflow.lgpd_requests
        WHERE type NOT IN ('Export', 'Delete', 'Anonymize')
           OR status NOT IN ('Requested', 'InReview', 'Processing', 'Completed', 'Rejected', 'Canceled')
    ) THEN
        RAISE EXCEPTION 'Migration 093 blocked: existing LGPD requests contain an unsupported type or status.';
    END IF;

    IF EXISTS (
        SELECT 1 FROM habitflow.billing_events
        WHERE plan IS NOT NULL AND plan NOT IN ('Free', 'Premium')
    ) THEN
        RAISE EXCEPTION 'Migration 093 blocked: existing billing events contain an unsupported plan.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM habitflow.habit_completions
        GROUP BY habit_id, completed_date
        HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION 'Migration 093 blocked: duplicate habit completions must be resolved before enforcing uniqueness.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.users'::regclass AND conname = 'ck_habitflow_users_role'
    ) THEN
        ALTER TABLE habitflow.users ADD CONSTRAINT ck_habitflow_users_role
            CHECK (role IN ('User', 'Admin', 'SuperAdmin', 'ReadOnly', 'Manager', 'TenantAdmin', 'TenantOwner', 'BillingAdmin'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.users'::regclass AND conname = 'ck_habitflow_users_account_status'
    ) THEN
        ALTER TABLE habitflow.users ADD CONSTRAINT ck_habitflow_users_account_status
            CHECK (account_status IN ('Active', 'Blocked', 'Suspended', 'DeletedPending'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.users'::regclass AND conname = 'ck_habitflow_users_risk_status'
    ) THEN
        ALTER TABLE habitflow.users ADD CONSTRAINT ck_habitflow_users_risk_status
            CHECK (risk_status IN ('Normal', 'Watchlist', 'Suspicious'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.users'::regclass AND conname = 'ck_habitflow_users_plan'
    ) THEN
        ALTER TABLE habitflow.users ADD CONSTRAINT ck_habitflow_users_plan
            CHECK (plan IN ('Free', 'Premium'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.users'::regclass AND conname = 'ck_habitflow_users_plan_status'
    ) THEN
        ALTER TABLE habitflow.users ADD CONSTRAINT ck_habitflow_users_plan_status
            CHECK (plan_status IN ('Active', 'Trial', 'Canceled', 'Inactive', 'PastDue'));
    END IF;

    IF to_regclass('habitflow.habit_completions') IS NULL THEN
        RAISE EXCEPTION 'Migration 093 blocked: habitflow.habit_completions is missing.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'habitflow.habit_completions'::regclass
          AND conname = 'uq_habitflow_habit_completions_habit_date'
    ) THEN
        SELECT conname
        INTO existing_constraint
        FROM pg_constraint
        WHERE conrelid = 'habitflow.habit_completions'::regclass
          AND contype = 'u'
          AND pg_get_constraintdef(oid) = 'UNIQUE (habit_id, completed_date)'
        ORDER BY conname
        LIMIT 1;

        IF existing_constraint IS NOT NULL THEN
            EXECUTE format(
                'ALTER TABLE habitflow.habit_completions RENAME CONSTRAINT %I TO %I',
                existing_constraint,
                'uq_habitflow_habit_completions_habit_date'
            );
        ELSE
            ALTER TABLE habitflow.habit_completions
                ADD CONSTRAINT uq_habitflow_habit_completions_habit_date UNIQUE (habit_id, completed_date);
        END IF;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.support_tickets'::regclass AND conname = 'ck_habitflow_support_tickets_status'
    ) THEN
        ALTER TABLE habitflow.support_tickets ADD CONSTRAINT ck_habitflow_support_tickets_status
            CHECK (status IN ('Open', 'InProgress', 'Resolved', 'Closed'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.lgpd_requests'::regclass AND conname = 'ck_habitflow_lgpd_requests_type'
    ) THEN
        ALTER TABLE habitflow.lgpd_requests ADD CONSTRAINT ck_habitflow_lgpd_requests_type
            CHECK (type IN ('Export', 'Delete', 'Anonymize'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.lgpd_requests'::regclass AND conname = 'ck_habitflow_lgpd_requests_status'
    ) THEN
        ALTER TABLE habitflow.lgpd_requests ADD CONSTRAINT ck_habitflow_lgpd_requests_status
            CHECK (status IN ('Requested', 'InReview', 'Processing', 'Completed', 'Rejected', 'Canceled'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.billing_events'::regclass AND conname = 'ck_habitflow_billing_events_plan'
    ) THEN
        ALTER TABLE habitflow.billing_events ADD CONSTRAINT ck_habitflow_billing_events_plan
            CHECK (plan IS NULL OR plan IN ('Free', 'Premium'));
    END IF;
END $$;

COMMIT;
-- END include database/migrations/093_restore_schema_contract_constraints.sql

-- BEGIN include database/migrations/094_v6197_ai_admin_usage.sql
-- HabitFlow v6.19.7: painel de IA. Sem prompt, sem API key.
BEGIN;
SET LOCAL search_path TO habitflow, public;

CREATE TABLE IF NOT EXISTS habitflow.ai_settings (
 id boolean PRIMARY KEY DEFAULT true CHECK (id),
 enabled boolean NOT NULL DEFAULT false,
 default_provider varchar(40) NOT NULL DEFAULT '',
 groq_enabled boolean NOT NULL DEFAULT false,
 gemini_enabled boolean NOT NULL DEFAULT false,
 deepseek_enabled boolean NOT NULL DEFAULT false,
 groq_models text NOT NULL DEFAULT '',
 gemini_models text NOT NULL DEFAULT '',
 deepseek_models text NOT NULL DEFAULT '',
 global_daily_limit integer NOT NULL DEFAULT 200,
 updated_by uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
 updated_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_ai_settings_global_limit CHECK (global_daily_limit BETWEEN 0 AND 100000),
 CONSTRAINT ck_ai_settings_provider CHECK (default_provider IN ('', 'Groq', 'Gemini', 'DeepSeek'))
);
INSERT INTO habitflow.ai_settings(id) VALUES (true) ON CONFLICT (id) DO NOTHING;

CREATE TABLE IF NOT EXISTS habitflow.ai_plan_limits (
 plan_code varchar(40) PRIMARY KEY,
 daily_limit integer NOT NULL,
 updated_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_ai_plan_limits_daily CHECK (daily_limit BETWEEN 0 AND 100000)
);
INSERT INTO habitflow.ai_plan_limits(plan_code, daily_limit) VALUES
 ('free', 5), ('ritmo', 40), ('team', 40), ('enterprise', 80)
ON CONFLICT (plan_code) DO NOTHING;

CREATE TABLE IF NOT EXISTS habitflow.ai_tenant_limits (
 client_id uuid PRIMARY KEY REFERENCES habitflow.clients(id) ON DELETE CASCADE,
 daily_limit integer NOT NULL,
 updated_at timestamptz NOT NULL DEFAULT now(),
 CONSTRAINT ck_ai_tenant_limits_daily CHECK (daily_limit BETWEEN 0 AND 100000)
);

CREATE TABLE IF NOT EXISTS habitflow.ai_usage_events (
 id uuid PRIMARY KEY,
 client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
 user_id uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
 provider varchar(40) NOT NULL,
 model varchar(80) NOT NULL DEFAULT '',
 status varchar(40) NOT NULL,
 event_code varchar(80) NOT NULL,
 correlation_id varchar(100) NOT NULL,
 duration_ms integer NOT NULL DEFAULT 0,
 created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_ai_usage_events_tenant_date ON habitflow.ai_usage_events(client_id, created_at DESC, status);
CREATE INDEX IF NOT EXISTS ix_ai_usage_events_user_date ON habitflow.ai_usage_events(client_id, user_id, created_at DESC);
CREATE INDEX IF NOT EXISTS ix_ai_usage_events_provider_date ON habitflow.ai_usage_events(provider, created_at DESC, status);
CREATE INDEX IF NOT EXISTS ix_ai_usage_events_status_date ON habitflow.ai_usage_events(status, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.ai_provider_events (
 id uuid PRIMARY KEY,
 client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
 user_id uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
 provider varchar(40) NOT NULL,
 model varchar(80) NOT NULL DEFAULT '',
 status varchar(40) NOT NULL,
 event_code varchar(80) NOT NULL,
 correlation_id varchar(100) NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_ai_provider_events_tenant_date ON habitflow.ai_provider_events(client_id, created_at DESC, status);
CREATE INDEX IF NOT EXISTS ix_ai_provider_events_provider_date ON habitflow.ai_provider_events(provider, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.ai_safety_events (
 id uuid PRIMARY KEY,
 client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
 user_id uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
 status varchar(40) NOT NULL,
 event_code varchar(80) NOT NULL,
 correlation_id varchar(100) NOT NULL,
 created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_ai_safety_events_tenant_date ON habitflow.ai_safety_events(client_id, created_at DESC, status);

COMMIT;
-- END include database/migrations/094_v6197_ai_admin_usage.sql

-- BEGIN include database/migrations/095_v6198_product_activation.sql
-- HabitFlow v6.19.8: implantação, templates por tenant e eventos de sucesso.
-- Não guarda prompt, segredo nem mensagem de conversa.
BEGIN;
SET LOCAL search_path TO habitflow, public;

ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS audience varchar(160);
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS goal_text varchar(300);
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS created_by uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL;
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS client_id uuid NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE;

UPDATE habitflow.habit_templates
SET audience = COALESCE(audience, 'Uso pessoal'),
    goal_text = COALESCE(goal_text, benefit_text)
WHERE client_id IS NULL AND (audience IS NULL OR goal_text IS NULL);

INSERT INTO habitflow.habit_templates(
    id, objective_id, name, description, category, suggested_frequency, suggested_color, difficulty,
    estimated_time_minutes, benefit_text, sort_order, is_active, minimum_plan_code, audience, goal_text, published_at)
SELECT v.id, v.objective_id, v.name, v.description, v.category, 'Daily', '#16A34A', 'Easy',
    v.minutes, v.goal_text, v.sort_order, true, v.minimum_plan, v.audience, v.goal_text, now()
FROM (VALUES
    ('01980000-0000-0000-0000-000000000001'::uuid, '10000000-0000-0000-0000-000000000005'::uuid, 'Começar o dia com uma prioridade', 'Escolha uma tarefa curta antes de abrir outras demandas.', 'Rotina matinal', 10, 20, 'free', 'Quem quer abrir o dia com uma ação só.', 'Definir a primeira prioridade do dia.'),
    ('01980000-0000-0000-0000-000000000002'::uuid, '10000000-0000-0000-0000-000000000006'::uuid, 'Encerrar o dia sem telas', 'Reserve os últimos minutos da noite sem tela.', 'Rotina noturna', 10, 21, 'free', 'Quem quer um fechamento curto do dia.', 'Reduzir tela antes de dormir.'),
    ('01980000-0000-0000-0000-000000000003'::uuid, '10000000-0000-0000-0000-000000000003'::uuid, 'Bloco de foco de 25 minutos', 'Um bloco curto, sem trocar de tarefa.', 'Foco', 25, 22, 'free', 'Quem precisa de um intervalo de concentração.', 'Concluir um bloco de foco.'),
    ('01980000-0000-0000-0000-000000000004'::uuid, '10000000-0000-0000-0000-000000000003'::uuid, 'Alinhar a equipe em 10 minutos', 'Uma pauta curta com a equipe, sem expor hábito privado.', 'Corporativo', 10, 23, 'team', 'Times com plano Team ou Enterprise.', 'Alinhar a prioridade do time.'),
    ('01980000-0000-0000-0000-000000000005'::uuid, '10000000-0000-0000-0000-000000000005'::uuid, 'Receber um colega novo', 'Checklist leve de boas-vindas do time.', 'Onboarding de equipe', 15, 24, 'team', 'Quem implanta um novo colega.', 'Concluir a recepção inicial.')
) AS v(id, objective_id, name, description, category, minutes, sort_order, minimum_plan, audience, goal_text)
WHERE NOT EXISTS (
    SELECT 1 FROM habitflow.habit_templates t WHERE t.objective_id = v.objective_id AND t.name = v.name
);

ALTER TABLE habitflow.habit_templates DROP CONSTRAINT IF EXISTS uq_habitflow_habit_templates_objective_name;
CREATE UNIQUE INDEX IF NOT EXISTS ux_habit_templates_scope_name
    ON habitflow.habit_templates (objective_id, name, COALESCE(client_id, '00000000-0000-0000-0000-000000000000'::uuid));
CREATE INDEX IF NOT EXISTS ix_habit_templates_client ON habitflow.habit_templates(client_id, is_active);

CREATE TABLE IF NOT EXISTS habitflow.tenant_onboarding_step_overrides (
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    step_code varchar(40) NOT NULL,
    status varchar(20) NOT NULL,
    reason varchar(500) NULL,
    updated_by uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (client_id, step_code),
    CONSTRAINT ck_onboarding_step_status CHECK (status IN ('Pendente', 'EmAndamento', 'Concluido', 'Bloqueado', 'Ignorado')),
    CONSTRAINT ck_onboarding_step_reason CHECK (status <> 'Ignorado' OR (reason IS NOT NULL AND char_length(btrim(reason)) >= 5))
);

CREATE TABLE IF NOT EXISTS habitflow.customer_success_events (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    event_code varchar(80) NOT NULL,
    score integer NULL,
    status varchar(40) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_customer_success_score CHECK (score IS NULL OR score BETWEEN 0 AND 100)
);
CREATE INDEX IF NOT EXISTS ix_customer_success_events_client ON habitflow.customer_success_events(client_id, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.notification_events (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    event_code varchar(80) NOT NULL,
    channel varchar(20) NOT NULL,
    status varchar(20) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_notification_events_channel CHECK (channel IN ('in-app', 'email', 'whatsapp', 'manual')),
    CONSTRAINT ck_notification_events_status CHECK (status IN ('created', 'suppressed', 'unavailable'))
);
CREATE INDEX IF NOT EXISTS ix_notification_events_client ON habitflow.notification_events(client_id, created_at DESC);

COMMIT;
-- END include database/migrations/095_v6198_product_activation.sql

-- BEGIN include database/migrations/096_v6200_commercial_homologation_trial.sql
-- 096: Homologação SaaS de Produção - Catálogo Comercial, Trial de 15 Dias e Desativação do Free Público
BEGIN;

UPDATE habitflow.plans
SET is_public = false,
    updated_at = now()
WHERE code = 'free';

UPDATE habitflow.plans
SET is_public = true,
    is_active = true,
    updated_at = now()
WHERE code IN ('premium_monthly', 'premium_yearly', 'ritmo', 'team', 'enterprise');

CREATE INDEX IF NOT EXISTS ix_plans_public_active
ON habitflow.plans(is_active, is_public, sort_order);

ALTER TABLE habitflow.clients
ADD COLUMN IF NOT EXISTS last_commercial_adjustment_at timestamp without time zone,
ADD COLUMN IF NOT EXISTS last_commercial_adjustment_reason text;

COMMIT;
-- END include database/migrations/096_v6200_commercial_homologation_trial.sql

-- BEGIN include database/migrations/097_v6210_operational_saas_cs_incidents.sql
-- HabitFlow v6.21.0 - Operação SaaS, Customer Success, Incidentes e Suporte Avançado
BEGIN;

ALTER TABLE habitflow.support_tickets_v2 
    ADD COLUMN IF NOT EXISTS resolution_reason VARCHAR(1000) NULL,
    ADD COLUMN IF NOT EXISTS satisfaction_rating INTEGER NULL CHECK (satisfaction_rating BETWEEN 1 AND 5),
    ADD COLUMN IF NOT EXISTS satisfaction_feedback VARCHAR(1000) NULL,
    ADD COLUMN IF NOT EXISTS reopened_at TIMESTAMPTZ NULL,
    ADD COLUMN IF NOT EXISTS resolved_at TIMESTAMPTZ NULL;


CREATE TABLE IF NOT EXISTS habitflow.operational_incidents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    title VARCHAR(200) NOT NULL,
    description TEXT NOT NULL,
    severity VARCHAR(20) NOT NULL CHECK (severity IN ('Info', 'Minor', 'Major', 'Critical')),
    status VARCHAR(20) NOT NULL CHECK (status IN ('Investigating', 'Identified', 'Monitoring', 'Resolved', 'Canceled')),
    impact TEXT NOT NULL,
    affected_tenants_count INTEGER NOT NULL DEFAULT 0,
    starts_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    estimated_resolution_at TIMESTAMPTZ NULL,
    resolved_at TIMESTAMPTZ NULL,
    canceled_at TIMESTAMPTZ NULL,
    responsible_user_id VARCHAR(100) NULL,
    responsible_name VARCHAR(150) NULL,
    communication_sent BOOLEAN NOT NULL DEFAULT FALSE,
    communication_notes TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_operational_incidents_status_severity 
    ON habitflow.operational_incidents (status, severity);
CREATE INDEX IF NOT EXISTS idx_operational_incidents_created_at 
    ON habitflow.operational_incidents (created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.incident_tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    incident_id UUID NOT NULL REFERENCES habitflow.operational_incidents(id) ON DELETE CASCADE,
    client_id UUID NOT NULL,
    impact_summary VARCHAR(500) NULL,
    notified BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_incident_tenants_incident_client 
    ON habitflow.incident_tenants (incident_id, client_id);
CREATE INDEX IF NOT EXISTS idx_incident_tenants_client 
    ON habitflow.incident_tenants (client_id);

CREATE TABLE IF NOT EXISTS habitflow.tenant_health_snapshots (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL,
    score INTEGER NOT NULL CHECK (score BETWEEN 0 AND 100),
    health_status VARCHAR(30) NOT NULL CHECK (health_status IN ('Healthy', 'Attention', 'AtRisk', 'Blocked', 'TrialEnding', 'PaymentIssue')),
    risk_factors TEXT[] NULL,
    metrics_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    captured_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_tenant_health_snapshots_client_captured 
    ON habitflow.tenant_health_snapshots (client_id, captured_at DESC);
CREATE INDEX IF NOT EXISTS idx_tenant_health_snapshots_status 
    ON habitflow.tenant_health_snapshots (health_status, captured_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.customer_success_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL,
    author_user_id VARCHAR(100) NOT NULL,
    author_name VARCHAR(150) NOT NULL,
    note_type VARCHAR(30) NOT NULL DEFAULT 'General' CHECK (note_type IN ('General', 'RiskFlag', 'CallLog', 'PlanReview', 'ActionPlan')),
    content TEXT NOT NULL,
    is_risk_flag BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_cs_notes_client_created 
    ON habitflow.customer_success_notes (client_id, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.operational_audit_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    event_name VARCHAR(100) NOT NULL,
    correlation_id VARCHAR(100) NOT NULL,
    client_id UUID NULL,
    executor_user_id VARCHAR(100) NULL,
    executor_email VARCHAR(200) NULL,
    severity VARCHAR(20) NOT NULL DEFAULT 'Info' CHECK (severity IN ('Info', 'Warning', 'Error', 'Critical')),
    status VARCHAR(30) NOT NULL DEFAULT 'Success',
    payload_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_operational_audit_event_name_occurred 
    ON habitflow.operational_audit_events (event_name, occurred_at DESC);
CREATE INDEX IF NOT EXISTS idx_operational_audit_client_occurred 
    ON habitflow.operational_audit_events (client_id, occurred_at DESC);
CREATE INDEX IF NOT EXISTS idx_operational_audit_correlation 
    ON habitflow.operational_audit_events (correlation_id);

COMMIT;
-- END include database/migrations/097_v6210_operational_saas_cs_incidents.sql


-- BEGIN include database/migrations/098_v6230_smart_automation_journeys.sql
-- HabitFlow v6.23.0: Automação Inteligente, Jornadas Guiadas, Marketplace de Templates e IA Aplicada
BEGIN;

SET LOCAL search_path TO habitflow, public;

CREATE TABLE IF NOT EXISTS habitflow.habit_journeys (
    id uuid PRIMARY KEY,
    client_id uuid NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    name varchar(150) NOT NULL,
    description text NOT NULL,
    category varchar(60) NOT NULL,
    goal varchar(150) NOT NULL,
    suggested_duration_days integer NOT NULL DEFAULT 21,
    frequency varchar(50) NOT NULL DEFAULT 'Diário',
    difficulty varchar(30) NOT NULL DEFAULT 'Iniciante',
    tags varchar(255) NULL,
    target_audience varchar(100) NULL,
    minimum_plan varchar(50) NOT NULL DEFAULT 'Free',
    is_active boolean NOT NULL DEFAULT true,
    is_official boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_journey_difficulty CHECK (difficulty IN ('Iniciante', 'Intermediário', 'Avançado'))
);

CREATE INDEX IF NOT EXISTS ix_habit_journeys_client ON habitflow.habit_journeys(client_id, is_active);
CREATE INDEX IF NOT EXISTS ix_habit_journeys_category ON habitflow.habit_journeys(category, difficulty);

ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS language varchar(10) NOT NULL DEFAULT 'pt-BR';
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS origin varchar(40) NOT NULL DEFAULT 'Official';
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS marketplace_status varchar(30) NOT NULL DEFAULT 'Published';
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS review_notes text NULL;
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS last_reviewed_at timestamptz NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_habit_templates_language_v6230'
          AND conrelid = 'habitflow.habit_templates'::regclass
    ) THEN
        ALTER TABLE habitflow.habit_templates
            ADD CONSTRAINT ck_habit_templates_language_v6230 CHECK (language IN ('pt-BR','en-US','es-ES'));
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_habit_templates_marketplace_status_v6230'
          AND conrelid = 'habitflow.habit_templates'::regclass
    ) THEN
        ALTER TABLE habitflow.habit_templates
            ADD CONSTRAINT ck_habit_templates_marketplace_status_v6230 CHECK (marketplace_status IN ('Draft','PendingReview','Published','Rejected','Archived'));
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_habit_templates_marketplace_filters
    ON habitflow.habit_templates(language, marketplace_status, category, difficulty);

CREATE TABLE IF NOT EXISTS habitflow.habit_journey_steps (
    id uuid PRIMARY KEY,
    journey_id uuid NOT NULL REFERENCES habitflow.habit_journeys(id) ON DELETE CASCADE,
    step_order integer NOT NULL,
    title varchar(150) NOT NULL,
    description text NOT NULL,
    suggested_habit_name varchar(150) NOT NULL,
    suggested_frequency varchar(50) NOT NULL DEFAULT 'Diário',
    suggested_reminder_time varchar(10) NULL DEFAULT '08:00',
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_habit_journey_steps_journey ON habitflow.habit_journey_steps(journey_id, step_order);

CREATE TABLE IF NOT EXISTS habitflow.habit_journey_members (
    id uuid PRIMARY KEY,
    journey_id uuid NOT NULL REFERENCES habitflow.habit_journeys(id) ON DELETE CASCADE,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    status varchar(30) NOT NULL DEFAULT 'Active',
    progress_percentage numeric(5,2) NOT NULL DEFAULT 0.00,
    notes text NULL,
    joined_at timestamptz NOT NULL DEFAULT now(),
    completed_at timestamptz NULL,
    CONSTRAINT ck_journey_member_status CHECK (status IN ('Active', 'Completed', 'Left')),
    CONSTRAINT uk_journey_member UNIQUE (journey_id, user_id)
);

CREATE INDEX IF NOT EXISTS ix_habit_journey_members_user ON habitflow.habit_journey_members(user_id, status);

CREATE TABLE IF NOT EXISTS habitflow.habit_automations (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    habit_id uuid NULL REFERENCES habitflow.habits(id) ON DELETE CASCADE,
    type varchar(50) NOT NULL,
    frequency varchar(30) NOT NULL DEFAULT 'Daily',
    status varchar(30) NOT NULL DEFAULT 'Active',
    quiet_hours_start varchar(5) NOT NULL DEFAULT '22:00',
    quiet_hours_end varchar(5) NOT NULL DEFAULT '07:00',
    settings_json text NULL,
    last_triggered_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_automation_type CHECK (type IN ('SmartReminder', 'WeeklyReview', 'ConsistencyAlert', 'RecoveryPlan', 'RescheduleSuggestion', 'MotivationalBoost', 'MonthlySummary')),
    CONSTRAINT ck_automation_status CHECK (status IN ('Active', 'Paused', 'Completed', 'Canceled', 'Error', 'AwaitingConfirmation'))
);

CREATE INDEX IF NOT EXISTS ix_habit_automations_user ON habitflow.habit_automations(client_id, user_id, status);

CREATE TABLE IF NOT EXISTS habitflow.habit_weekly_reviews (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    period_start date NOT NULL,
    period_end date NOT NULL,
    habits_completed integer NOT NULL DEFAULT 0,
    habits_abandoned integer NOT NULL DEFAULT 0,
    consistency_rate numeric(5,2) NOT NULL DEFAULT 0.00,
    best_streak integer NOT NULL DEFAULT 0,
    goals_achieved integer NOT NULL DEFAULT 0,
    summary_text text NOT NULL,
    recommendations_json text NULL,
    ai_generated boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_habit_weekly_reviews_user ON habitflow.habit_weekly_reviews(client_id, user_id, period_start DESC);

CREATE TABLE IF NOT EXISTS habitflow.habit_monthly_reviews (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    period_start date NOT NULL,
    period_end date NOT NULL,
    habits_completed integer NOT NULL DEFAULT 0,
    consistency_rate numeric(5,2) NOT NULL DEFAULT 0.00,
    summary_text text NOT NULL,
    recommendations_json text NULL,
    ai_generated boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uk_habit_monthly_reviews_period UNIQUE (client_id, user_id, period_start)
);

CREATE INDEX IF NOT EXISTS ix_habit_monthly_reviews_user ON habitflow.habit_monthly_reviews(client_id, user_id, period_start DESC);

CREATE TABLE IF NOT EXISTS habitflow.ai_recommendations (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    category varchar(50) NOT NULL,
    recommendation_type varchar(50) NOT NULL,
    title varchar(150) NOT NULL,
    content text NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'Pending',
    ai_model varchar(100) NULL,
    provider varchar(50) NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    resolved_at timestamptz NULL,
    CONSTRAINT ck_ai_recommendation_status CHECK (status IN ('Pending', 'Accepted', 'Rejected'))
);

CREATE INDEX IF NOT EXISTS ix_ai_recommendations_user ON habitflow.ai_recommendations(client_id, user_id, status);

CREATE TABLE IF NOT EXISTS habitflow.ai_usage_events (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    provider varchar(50) NOT NULL,
    model varchar(100) NOT NULL,
    event_code varchar(80) NOT NULL,
    status varchar(30) NOT NULL,
    correlation_id varchar(80) NOT NULL,
    duration_ms integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ai_usage_events_tenant_month
    ON habitflow.ai_usage_events(client_id, user_id, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.ai_prompt_audit (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    provider varchar(50) NOT NULL,
    model varchar(100) NOT NULL,
    purpose varchar(80) NOT NULL,
    safety_status varchar(40) NOT NULL,
    prompt_hash varchar(128) NOT NULL,
    response_hash varchar(128) NULL,
    correlation_id varchar(80) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ai_prompt_audit_client ON habitflow.ai_prompt_audit(client_id, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.billing_trial_events (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    event_code varchar(80) NOT NULL,
    plan_code varchar(50) NOT NULL,
    trial_started_at timestamptz NULL,
    trial_ends_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_billing_trial_events_client ON habitflow.billing_trial_events(client_id, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.security_audit_events (
    id uuid PRIMARY KEY,
    client_id uuid NULL REFERENCES habitflow.clients(id) ON DELETE SET NULL,
    user_id uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    event_code varchar(80) NOT NULL,
    severity varchar(20) NOT NULL DEFAULT 'Info',
    result varchar(30) NOT NULL,
    correlation_id varchar(80) NOT NULL,
    metadata_json text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_security_audit_severity CHECK (severity IN ('Info','Warning','Critical'))
);

CREATE INDEX IF NOT EXISTS ix_security_audit_events_lookup ON habitflow.security_audit_events(client_id, user_id, created_at DESC);

-- Seed Jornadas Guiadas Oficiais
INSERT INTO habitflow.habit_journeys (id, client_id, name, description, category, goal, suggested_duration_days, frequency, difficulty, tags, target_audience, minimum_plan, is_active, is_official)
VALUES
('62300000-0000-4000-8000-000000000001', NULL, 'Dormir Melhor', 'Estabeleça uma rotina noturna consistente para melhorar a qualidade do sono e disposição matinal.', 'Saúde e Bem-estar', 'Melhorar a qualidade do sono e ter mais energia pela manhã', 21, 'Diário', 'Iniciante', 'sono,saúde,descanso,bem-estar', 'Pessoas com insônia ou cansaço matinal', 'Free', true, true),
('62300000-0000-4000-8000-000000000002', NULL, 'Produtividade e Foco', 'Técnicas diárias de blocos de tempo e eliminação de distrações para render mais no trabalho e estudos.', 'Produtividade', 'Aumentar foco e entrega sem burnout', 30, 'Diário', 'Intermediário', 'foco,tempo,produtividade,organização', 'Profissionais e estudantes', 'Free', true, true),
('62300000-0000-4000-8000-000000000003', NULL, 'Hábito da Leitura', 'Leia diariamente de forma sustentável começando com metas acessíveis até criar o prazer pelo livro.', 'Educação', 'Ler pelo menos 1 livro por mês com consistência', 21, 'Diário', 'Iniciante', 'leitura,livros,estudos,conhecimento', 'Qualquer pessoa que quer ler mais', 'Free', true, true),
('62300000-0000-4000-8000-000000000004', NULL, 'Atividade Física Constante', 'Saia do sedentarismo com passos graduais e comemore cada sessão concluída.', 'Saúde e Movimento', 'Praticar exercícios pelo menos 4 vezes por semana', 30, '4x por semana', 'Iniciante', 'exercícios,corpo,movimento,energia', 'Iniciantes em atividades físicas', 'Free', true, true),
('62300000-0000-4000-8000-000000000005', NULL, 'Saúde Mental e Atenção Plena', 'Pequenas pausas respiratórias e check-ins emocionais para reduzir a ansiedade do dia a dia.', 'Bem-estar', 'Reduzir estresse e cultivar clareza mental', 21, 'Diário', 'Iniciante', 'mindfulness,respiração,paz,ansiedade', 'Pessoas sob rotina acelerada', 'Free', true, true),
('62300000-0000-4000-8000-000000000006', NULL, 'Rotina Matinal Poderosa', 'Comece seus dias com intenção: hidratação, planejamento e foco nas primeiras horas.', 'Organização', 'Dominar a primeira hora do dia sem celular', 15, 'Diário', 'Iniciante', 'manhã,energia,intencionalidade,planejamento', 'Pessoas que acordam correndo', 'Free', true, true),
('62300000-0000-4000-8000-000000000007', NULL, 'Foco nos Estudos', 'Desenvolva disciplina de estudos com revisões periódicas e sessões sem celular.', 'Educação', 'Consistência em cursos, faculdade ou certificações', 30, 'Diário', 'Intermediário', 'estudo,provas,disciplina,concurso', 'Estudantes e concurseiros', 'Free', true, true),
('62300000-0000-4000-8000-000000000008', NULL, 'Organização Financeira', 'Registre despesas diariamente, revise metas semanais e evite gastos impulsivos.', 'Finanças', 'Controlar orçamento e formar reserva de emergência', 30, 'Diário', 'Intermediário', 'finanças,dinheiro,planejamento,economia', 'Adultos organizando despesas', 'Free', true, true),
('62300000-0000-4000-8000-000000000009', NULL, 'Hábitos Corporativos de Alto Impacto', 'Alinhamentos ágeis, comunicação assíncrona clara e encerramento pontual de expediente.', 'Corporativo', 'Aumentar a colaboração e harmonia em times', 30, 'Dias úteis', 'Intermediário', 'trabalho,equipe,alinhamento,saúde corporativa', 'Times e gestores corporativos', 'Team', true, true),
('62300000-0000-4000-8000-000000000010', NULL, 'Onboarding de Colaboradores', 'Checklist guiado de primeiros passos na empresa, rotina de boas-vindas e metas de 15 dias.', 'Corporativo', 'Acelerar integração de novos membros no workspace', 15, 'Dias úteis', 'Iniciante', 'onboarding,rh,integração,cultura', 'Novos colaboradores e gestores', 'Team', true, true)
ON CONFLICT (id) DO NOTHING;

-- Seed Passos das Jornadas
INSERT INTO habitflow.habit_journey_steps (id, journey_id, step_order, title, description, suggested_habit_name, suggested_frequency, suggested_reminder_time)
VALUES
('62300001-0000-4000-8000-000000000001', '62300000-0000-4000-8000-000000000001', 1, 'Desconectar Telas 30min Antes', 'Desligue smartphone e TV 30 minutos antes de deitar.', 'Desconectar telas à noite', 'Diário', '22:30'),
('62300001-0000-4000-8000-000000000002', '62300000-0000-4000-8000-000000000001', 2, 'Horário Fixo de Deitar', 'Mantenha o mesmo horário de dormir para sincronizar o ciclo circadiano.', 'Dormir no horário planejado', 'Diário', '23:00'),
('62300001-0000-4000-8000-000000000003', '62300000-0000-4000-8000-000000000002', 1, 'Planejar as 3 Prioridades do Dia', 'Defina apenas 3 grandes tarefas no início da jornada.', 'Definir 3 prioridades diárias', 'Dias úteis', '08:30'),
('62300001-0000-4000-8000-000000000004', '62300000-0000-4000-8000-000000000002', 2, 'Bloco de Foco sem Notificações', 'Trabalhe 45 minutos com celular em modo foco.', 'Bloco de foco profundo', 'Dias úteis', '10:00'),
('62300001-0000-4000-8000-000000000005', '62300000-0000-4000-8000-000000000003', 1, 'Leitura de 15 Minutos', 'Leia pelo menos 10 páginas ou 15 minutos de um livro.', 'Ler 15 minutos', 'Diário', '20:00'),
('62300001-0000-4000-8000-000000000006', '62300000-0000-4000-8000-000000000004', 1, 'Caminhada ou Treino 30min', 'Pratique 30 minutos de caminhada, musculação ou esporte.', 'Atividade física 30min', '4x por semana', '07:00'),
('62300001-0000-4000-8000-000000000007', '62300000-0000-4000-8000-000000000005', 1, 'Pausa de Respiração Consciente', 'Reserve 5 minutos para respiração lenta e desacelerar a mente.', 'Respiração consciente 5min', 'Diário', '14:00'),
('62300001-0000-4000-8000-000000000008', '62300000-0000-4000-8000-000000000006', 1, 'Beber 500ml de Água ao Acordar', 'Hidrate o corpo imediatamente ao levantar.', 'Água matinal 500ml', 'Diário', '07:00'),
('62300001-0000-4000-8000-000000000009', '62300000-0000-4000-8000-000000000007', 1, 'Sessão Pomodoro de Estudo', 'Estude 25 a 50 minutos com dedicação exclusiva.', 'Sessão focada de estudos', 'Diário', '19:00'),
('62300001-0000-4000-8000-000000000010', '62300000-0000-4000-8000-000000000008', 1, 'Anotar Gastos do Dia', 'Registre todas as despesas diárias para conscientização.', 'Registro de gastos diários', 'Diário', '21:00')
ON CONFLICT (id) DO NOTHING;

COMMIT;

-- END include database/migrations/098_v6230_smart_automation_journeys.sql

-- BEGIN include database/migrations/099_v6240_customer_operations_growth.sql
-- Migration 099: HabitFlow v6.24.0 - Operacao SaaS Inteligente, Retencao, Saude do Cliente e Growth
-- Schema habitflow

CREATE SCHEMA IF NOT EXISTS habitflow;

-- Saude operacional consolidada por tenant, com sinais auditaveis e acoes recomendadas.
CREATE TABLE IF NOT EXISTS habitflow.customer_health_scores (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    score INTEGER NOT NULL CHECK (score BETWEEN 0 AND 100),
    status VARCHAR(30) NOT NULL CHECK (status IN ('Healthy', 'Attention', 'AtRisk', 'Critical', 'Inactive')),
    churn_risk BOOLEAN NOT NULL DEFAULT FALSE,
    signals TEXT[] NOT NULL DEFAULT ARRAY[]::TEXT[],
    unavailable_metrics TEXT[] NOT NULL DEFAULT ARRAY[]::TEXT[],
    recommended_actions TEXT[] NOT NULL DEFAULT ARRAY[]::TEXT[],
    metrics_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    calculated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT uq_customer_health_scores_client UNIQUE (client_id)
);

CREATE INDEX IF NOT EXISTS idx_customer_health_scores_status
    ON habitflow.customer_health_scores (status, score DESC, updated_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.customer_health_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    event_code VARCHAR(100) NOT NULL,
    score INTEGER NOT NULL CHECK (score BETWEEN 0 AND 100),
    status VARCHAR(30) NOT NULL CHECK (status IN ('Healthy', 'Attention', 'AtRisk', 'Critical', 'Inactive')),
    correlation_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_customer_health_events_client_created
    ON habitflow.customer_health_events (client_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_customer_health_events_correlation
    ON habitflow.customer_health_events (correlation_id);

-- Funil canonico de ativacao por usuario e por tenant.
CREATE TABLE IF NOT EXISTS habitflow.activation_funnel_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    scope VARCHAR(20) NOT NULL CHECK (scope IN ('User', 'Tenant')),
    step_code VARCHAR(80) NOT NULL,
    status VARCHAR(30) NOT NULL CHECK (status IN ('Completed', 'Skipped', 'Blocked')),
    correlation_id VARCHAR(100) NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_activation_funnel_events_step
    ON habitflow.activation_funnel_events (
        client_id,
        scope,
        step_code,
        COALESCE(user_id, '00000000-0000-0000-0000-000000000000'::uuid)
    );
CREATE INDEX IF NOT EXISTS idx_activation_funnel_events_client_scope
    ON habitflow.activation_funnel_events (client_id, scope, occurred_at DESC);
CREATE INDEX IF NOT EXISTS idx_activation_funnel_events_user
    ON habitflow.activation_funnel_events (user_id, occurred_at DESC);

-- Riscos de retencao tratados como eventos idempotentes e acompanhaveis.
CREATE TABLE IF NOT EXISTS habitflow.retention_risk_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    risk_code VARCHAR(80) NOT NULL,
    severity VARCHAR(20) NOT NULL CHECK (severity IN ('Low', 'Medium', 'High', 'Critical')),
    recommendation TEXT NOT NULL,
    status VARCHAR(30) NOT NULL DEFAULT 'Open' CHECK (status IN ('Open', 'Acknowledged', 'Resolved', 'Dismissed')),
    correlation_id VARCHAR(100) NOT NULL,
    detected_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolved_at TIMESTAMPTZ NULL,
    CONSTRAINT uq_retention_risk_client_code_correlation UNIQUE (client_id, risk_code, correlation_id)
);

CREATE INDEX IF NOT EXISTS idx_retention_risk_events_client_status
    ON habitflow.retention_risk_events (client_id, status, severity, detected_at DESC);
CREATE INDEX IF NOT EXISTS idx_retention_risk_events_user
    ON habitflow.retention_risk_events (user_id, detected_at DESC);

-- Notas operacionais de CS, suporte e plano de acao.
CREATE TABLE IF NOT EXISTS habitflow.operation_notes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    author_user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE RESTRICT,
    note_type VARCHAR(30) NOT NULL CHECK (note_type IN ('General', 'RiskFlag', 'CallLog', 'PlanReview', 'ActionPlan', 'CommercialFollowUp')),
    content TEXT NOT NULL CHECK (char_length(content) BETWEEN 5 AND 2000),
    correlation_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_operation_notes_client_created
    ON habitflow.operation_notes (client_id, created_at DESC);

-- Insights de IA operacional sempre higienizados e pendentes de revisao humana.
CREATE TABLE IF NOT EXISTS habitflow.ai_operation_insights (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    insight_type VARCHAR(80) NOT NULL,
    provider VARCHAR(50) NOT NULL,
    model VARCHAR(100) NOT NULL,
    sanitized_summary TEXT NOT NULL CHECK (char_length(sanitized_summary) <= 4000),
    requires_human_review BOOLEAN NOT NULL DEFAULT TRUE,
    correlation_id VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_ai_operation_insights_client_created
    ON habitflow.ai_operation_insights (client_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_ai_operation_insights_correlation
    ON habitflow.ai_operation_insights (correlation_id);

-- Compatibilidade do snapshot v6.21 com novos estados de saude v6.24.
ALTER TABLE habitflow.tenant_health_snapshots
    DROP CONSTRAINT IF EXISTS tenant_health_snapshots_health_status_check;
ALTER TABLE habitflow.tenant_health_snapshots
    ADD CONSTRAINT tenant_health_snapshots_health_status_check
    CHECK (health_status IN ('Healthy', 'Attention', 'AtRisk', 'Blocked', 'TrialEnding', 'PaymentIssue', 'Critical', 'Inactive'));

-- END include database/migrations/099_v6240_customer_operations_growth.sql

-- START include database/migrations/100_v6250_pwa_offline_integrations_api_webhooks_portability.sql
-- Migration 100: HabitFlow v6.25.0 - PWA Mobile, Offline-First, Notificações Reais, Integrações, API Pública, Webhooks e Portabilidade SaaS
CREATE TABLE IF NOT EXISTS habitflow.offline_sync_queue (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    action_type VARCHAR(40) NOT NULL,
    entity_id UUID NULL,
    payload JSONB NOT NULL DEFAULT '{}'::jsonb,
    status VARCHAR(20) NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending', 'Synced', 'Failed', 'Conflict', 'Discarded')),
    error_message TEXT NULL,
    conflict_details JSONB NULL,
    client_created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    synced_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_offline_sync_queue_client_user
    ON habitflow.offline_sync_queue (client_id, user_id, status, created_at DESC);

ALTER TABLE habitflow.integration_webhooks
    ADD COLUMN IF NOT EXISTS consecutive_failures INTEGER NOT NULL DEFAULT 0;

ALTER TABLE habitflow.integration_webhooks
    ADD COLUMN IF NOT EXISTS last_failed_at TIMESTAMPTZ NULL;

ALTER TABLE habitflow.integration_webhooks
    ADD COLUMN IF NOT EXISTS is_paused BOOLEAN NOT NULL DEFAULT FALSE;

ALTER TABLE habitflow.webhook_delivery_attempts
    ADD COLUMN IF NOT EXISTS response_body TEXT NULL;

ALTER TABLE habitflow.webhook_delivery_attempts
    ADD COLUMN IF NOT EXISTS error_message TEXT NULL;

ALTER TABLE habitflow.webhook_delivery_attempts
    ADD COLUMN IF NOT EXISTS duration_ms INTEGER NULL;

CREATE TABLE IF NOT EXISTS habitflow.data_export_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    scope VARCHAR(20) NOT NULL CHECK (scope IN ('User', 'Tenant')),
    format VARCHAR(10) NOT NULL CHECK (format IN ('csv', 'json')),
    status VARCHAR(20) NOT NULL DEFAULT 'Completed' CHECK (status IN ('Completed', 'Failed')),
    file_name VARCHAR(150) NOT NULL,
    record_count INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_data_export_requests_client_user
    ON habitflow.data_export_requests (client_id, user_id, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.data_import_batches (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    entity_type VARCHAR(30) NOT NULL CHECK (entity_type IN ('habits', 'templates')),
    format VARCHAR(10) NOT NULL CHECK (format IN ('csv', 'json')),
    status VARCHAR(20) NOT NULL DEFAULT 'Completed' CHECK (status IN ('Simulated', 'Completed', 'Failed')),
    total_rows INTEGER NOT NULL DEFAULT 0,
    imported_rows INTEGER NOT NULL DEFAULT 0,
    failed_rows INTEGER NOT NULL DEFAULT 0,
    errors_json JSONB NOT NULL DEFAULT '[]'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_data_import_batches_client_user
    ON habitflow.data_import_batches (client_id, user_id, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.ai_mobile_recommendations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    recommendation_type VARCHAR(50) NOT NULL,
    provider VARCHAR(30) NOT NULL,
    model VARCHAR(50) NOT NULL,
    content TEXT NOT NULL,
    metadata JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_ai_mobile_rec_client_user
    ON habitflow.ai_mobile_recommendations (client_id, user_id, recommendation_type, created_at DESC);

INSERT INTO habitflow.plan_features (code, name, value_type, default_bool_value, default_int_value, default_string_value)
VALUES
    ('webhooks', 'Webhooks de Saída', 'boolean', false, null, null),
    ('public_api', 'API Pública Rest', 'boolean', false, null, null),
    ('data_portability', 'Portabilidade de Dados (Export/Import)', 'boolean', true, null, null),
    ('push_notifications', 'Notificações Web Push', 'boolean', false, null, null),
    ('advanced_offline', 'Modo Offline Avançado', 'boolean', false, null, null)
ON CONFLICT (code) DO NOTHING;

INSERT INTO habitflow.plan_feature_entitlements (plan_code, feature_code, bool_value, int_value)
VALUES
    ('ritmo', 'webhooks', true, 2),
    ('ritmo', 'public_api', true, 3),
    ('ritmo', 'data_portability', true, null),
    ('ritmo', 'push_notifications', true, null),
    ('ritmo', 'advanced_offline', true, null),
    ('evolucao', 'webhooks', true, 10),
    ('evolucao', 'public_api', true, 10),
    ('evolucao', 'data_portability', true, null),
    ('evolucao', 'push_notifications', true, null),
    ('evolucao', 'advanced_offline', true, null),
    ('team', 'webhooks', true, 20),
    ('team', 'public_api', true, 20),
    ('team', 'data_portability', true, null),
    ('team', 'push_notifications', true, null),
    ('team', 'advanced_offline', true, null),
    ('enterprise', 'webhooks', true, -1),
    ('enterprise', 'public_api', true, -1),
    ('enterprise', 'data_portability', true, null),
    ('enterprise', 'push_notifications', true, null),
    ('enterprise', 'advanced_offline', true, null),
    ('free', 'data_portability', true, null),
    ('free', 'webhooks', false, 0),
    ('free', 'public_api', false, 0),
    ('free', 'push_notifications', false, null),
    ('free', 'advanced_offline', false, null)
ON CONFLICT (plan_code, feature_code) DO NOTHING;
-- END include database/migrations/100_v6250_pwa_offline_integrations_api_webhooks_portability.sql

-- START include database/migrations/101_v6260_production_observability_lgpd_backup_incidents_governance.sql
CREATE TABLE IF NOT EXISTS habitflow.system_health_checks (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    component_name VARCHAR(60) NOT NULL UNIQUE,
    status VARCHAR(30) NOT NULL CHECK (status IN ('Healthy', 'Degraded', 'Unhealthy', 'Disabled', 'NotConfigured')),
    duration_ms INTEGER NOT NULL DEFAULT 0,
    details TEXT NULL,
    error_message TEXT NULL,
    operational_recommendation TEXT NULL,
    last_checked_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS habitflow.system_health_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    component_name VARCHAR(60) NOT NULL,
    status VARCHAR(30) NOT NULL,
    duration_ms INTEGER NOT NULL DEFAULT 0,
    error_message TEXT NULL,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_system_health_history_comp_date
    ON habitflow.system_health_history (component_name, recorded_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.lgpd_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    request_type VARCHAR(40) NOT NULL CHECK (request_type IN ('Export', 'Deletion', 'Anonymization', 'Rectification')),
    status VARCHAR(40) NOT NULL DEFAULT 'Aberta' CHECK (status IN ('Aberta', 'Em análise', 'Aguardando confirmação', 'Processada', 'Recusada', 'Cancelada')),
    reason TEXT NULL,
    admin_notes TEXT NULL,
    processed_by_user_id UUID NULL,
    processed_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_lgpd_requests_client_user
    ON habitflow.lgpd_requests (client_id, user_id, status, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.consent_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    client_id UUID NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    consent_type VARCHAR(60) NOT NULL CHECK (consent_type IN ('TermsOfService', 'PrivacyPolicy', 'Marketing', 'Analytics', 'DataProcessing')),
    granted BOOLEAN NOT NULL DEFAULT TRUE,
    policy_version VARCHAR(30) NOT NULL DEFAULT '1.0',
    ip_address VARCHAR(45) NULL,
    user_agent TEXT NULL,
    granted_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    revoked_at TIMESTAMPTZ NULL
);

CREATE INDEX IF NOT EXISTS ix_consent_records_user
    ON habitflow.consent_records (user_id, consent_type, granted_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.backup_records (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    backup_type VARCHAR(30) NOT NULL CHECK (backup_type IN ('Full', 'SchemaOnly', 'DataOnly', 'Automated')),
    status VARCHAR(30) NOT NULL CHECK (status IN ('Completed', 'Failed', 'Verified', 'Restored')),
    file_name VARCHAR(200) NOT NULL,
    size_bytes BIGINT NOT NULL DEFAULT 0,
    duration_ms INTEGER NOT NULL DEFAULT 0,
    integrity_status VARCHAR(30) NOT NULL DEFAULT 'Pending' CHECK (integrity_status IN ('Pending', 'Valid', 'Corrupted', 'Skipped')),
    sha256_hash CHAR(64) NULL,
    notes TEXT NULL,
    verified_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_backup_records_created
    ON habitflow.backup_records (created_at DESC);

ALTER TABLE habitflow.operational_incidents
    ADD COLUMN IF NOT EXISTS sev_code VARCHAR(10) NOT NULL DEFAULT 'SEV3';

ALTER TABLE habitflow.operational_incidents
    ADD COLUMN IF NOT EXISTS affected_module VARCHAR(80) NULL DEFAULT 'Geral';

ALTER TABLE habitflow.operational_incidents
    ADD COLUMN IF NOT EXISTS root_cause TEXT NULL;

ALTER TABLE habitflow.operational_incidents
    ADD COLUMN IF NOT EXISTS actions_taken TEXT NULL;

ALTER TABLE habitflow.operational_incidents
    ADD COLUMN IF NOT EXISTS next_steps TEXT NULL;

CREATE TABLE IF NOT EXISTS habitflow.release_checklist_items (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    release_version VARCHAR(20) NOT NULL,
    category VARCHAR(50) NOT NULL,
    title VARCHAR(150) NOT NULL,
    description TEXT NULL,
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,
    completed_by VARCHAR(120) NULL,
    completed_at TIMESTAMPTZ NULL,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS ix_release_checklist_version
    ON habitflow.release_checklist_items (release_version, sort_order ASC);

INSERT INTO habitflow.release_checklist_items (release_version, category, title, description, is_completed, sort_order)
VALUES
    ('v6.26.0', 'Observabilidade', 'Health Checks de produção operacionais', 'Banco, migrations, IA, SMTP e webhooks monitorados', true, 1),
    ('v6.26.0', 'LGPD', 'Fluxo de consentimentos e solicitações LGPD auditado', 'Exportação, anonimização e exclusão com retenção legal', true, 2),
    ('v6.26.0', 'Backup', 'Plano de backup/restore e RPO/RTO documentados', 'RPO <= 1h e RTO <= 30m com verificação de integridade', true, 3),
    ('v6.26.0', 'Incidentes', 'Gestão de incidentes SEV1-SEV4 com comunicação', 'Painel operacional integrado e transparente para clientes', true, 4),
    ('v6.26.0', 'Segurança', 'Isolamento multi-tenant e sanitização de secrets', 'Zero vazamento de credenciais em logs e headers seguros', true, 5),
    ('v6.26.0', 'Qualidade', 'Suíte de testes automatizados e builds verdes', 'Testes unitários e de integração 100% aprovados', true, 6)
ON CONFLICT DO NOTHING;
-- END include database/migrations/101_v6260_production_observability_lgpd_backup_incidents_governance.sql
