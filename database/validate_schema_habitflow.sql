\set ON_ERROR_STOP on
-- HabitFlow v4.5 - validação do schema oficial habitflow.

with required_tables(table_name) as (values
 ('users'),('habits'),('habit_completions'),('support_tickets'),('support_messages'),
 ('system_audit_logs'),('admin_audit_logs'),('system_settings'),('lgpd_requests'),
 ('billing_events'),('billing_customers'),('billing_subscriptions'),('billing_checkout_sessions'),('billing_invoices'),('billing_payments'),('billing_webhook_events'),('billing_audit_events'),('notifications'),('habit_objectives'),('habit_templates'),('user_reports'),('plans'),('subscriptions'),('payment_transactions'),('payment_webhook_events'),('payment_audit_logs')
), required_indexes(index_name) as (values
 ('ix_habitflow_users_email'),('ix_habitflow_users_role'),('ix_habitflow_users_account_status'),
 ('ix_habitflow_users_plan'),('ix_habitflow_habits_user_id'),('ix_habitflow_habit_completions_user_id'),
 ('ix_habitflow_support_tickets_user_id'),('ix_habitflow_lgpd_requests_user_id'),
 ('ix_habitflow_system_audit_logs_created_at'),('ix_habitflow_admin_audit_logs_created_at'),('ix_habitflow_plans_code'),('ix_habitflow_subscriptions_user_id'),('ix_habitflow_subscriptions_status'),('ix_habitflow_subscriptions_provider_payment_id'),('ix_habitflow_payment_transactions_user_id'),('ix_habitflow_payment_transactions_provider_payment_id'),('ix_habitflow_payment_webhook_events_event_id'),('ix_habitflow_payment_webhook_events_received_at'),('ix_habitflow_habit_objectives_slug'),('ix_habitflow_habit_templates_objective_id'),('ix_habitflow_habit_templates_category'),('ix_habitflow_habit_templates_is_active')
), required_constraints(constraint_name) as (values
 ('ck_habitflow_users_role'),('ck_habitflow_users_account_status'),('ck_habitflow_users_risk_status'),
 ('ck_habitflow_users_plan'),('ck_habitflow_users_plan_status'),('uq_habitflow_habit_completions_habit_date'),
 ('ck_habitflow_support_tickets_status'),('ck_habitflow_lgpd_requests_type'),('ck_habitflow_lgpd_requests_status'),
 ('ck_habitflow_billing_events_plan'),('ck_habitflow_subscriptions_status'),('ck_habitflow_subscriptions_billing_cycle'),('ck_habitflow_subscriptions_provider'),('ck_habitflow_payment_transactions_provider'),('ck_habitflow_payment_transactions_status'),('ck_habitflow_habit_templates_frequency'),('ck_habitflow_habit_templates_difficulty'),('uq_habitflow_habit_templates_objective_name')
), public_conflicts as (
 select table_schema, table_name from information_schema.tables where table_schema = 'public' and table_name in
 ('users','habits','habit_completions','support_tickets','support_messages','system_audit_logs','admin_audit_logs','system_settings','lgpd_requests','billing_events','notifications','user_reports','plans','subscriptions','payment_transactions','payment_webhook_events','payment_audit_logs','habit_objectives','habit_templates')
), missing_tables as (
 select rt.table_name from required_tables rt left join information_schema.tables t on t.table_schema='habitflow' and t.table_name=rt.table_name where t.table_name is null
), missing_indexes as (
 select ri.index_name from required_indexes ri left join pg_indexes i on i.schemaname='habitflow' and i.indexname=ri.index_name where i.indexname is null
), missing_constraints as (
 select rc.constraint_name from required_constraints rc left join information_schema.table_constraints c on c.table_schema='habitflow' and c.constraint_name=rc.constraint_name where c.constraint_name is null
), mnsoft_settings as (
 select count(*)::int total from habitflow.system_settings where key in ('companyName','companyLegalName','companyCnpj','commercialEmail','supportEmail')
)
select current_database() database, exists(select 1 from information_schema.schemata where schema_name='habitflow') schema_exists,
 (select count(*) from public_conflicts) public_conflicts,
 (select count(*) from missing_tables) missing_tables,
 (select count(*) from missing_indexes) missing_indexes,
 (select count(*) from missing_constraints) missing_constraints,
 (select total from mnsoft_settings) mnsoft_settings_found,
 now() checked_at;

do $$
declare errors text[] := array[]::text[];
begin
 if not exists(select 1 from information_schema.schemata where schema_name='habitflow') then errors := array_append(errors,'schema habitflow não existe'); end if;
 if exists(select 1 from information_schema.tables where table_schema='public' and table_name in ('users','habits','habit_completions','support_tickets','support_messages','system_audit_logs','admin_audit_logs','system_settings','lgpd_requests','billing_events','notifications','user_reports','plans','subscriptions','payment_transactions','payment_webhook_events','payment_audit_logs','habit_objectives','habit_templates')) then errors := array_append(errors,'há tabelas HabitFlow indevidas no schema public'); end if;
 if exists(with r(table_name) as (values ('users'),('habits'),('habit_completions'),('support_tickets'),('support_messages'),('system_audit_logs'),('admin_audit_logs'),('system_settings'),('lgpd_requests'),('billing_events'),('billing_customers'),('billing_subscriptions'),('billing_checkout_sessions'),('billing_invoices'),('billing_payments'),('billing_webhook_events'),('billing_audit_events'),('notifications'),('habit_objectives'),('habit_templates'),('user_reports'),('plans'),('subscriptions'),('payment_transactions'),('payment_webhook_events'),('payment_audit_logs')) select 1 from r left join information_schema.tables t on t.table_schema='habitflow' and t.table_name=r.table_name where t.table_name is null) then errors := array_append(errors,'há tabelas obrigatórias ausentes em habitflow'); end if;
 if (select count(*) from habitflow.system_settings where key in ('companyName','companyLegalName','companyCnpj','commercialEmail','supportEmail')) < 5 then errors := array_append(errors,'settings mínimos MNSOFT ausentes'); end if;
 if array_length(errors,1) is not null then raise exception 'Validação HabitFlow falhou: %', array_to_string(errors,'; '); end if;
end $$;

-- v5 accessibility preferences validation
select 'habitflow.user_ui_preferences' as required_table, to_regclass('habitflow.user_ui_preferences') is not null as exists;
select column_name from information_schema.columns where table_schema='habitflow' and table_name='user_ui_preferences' and column_name in ('id','user_id','contrast_mode','font_scale','reduce_motion','show_achievement_popups','show_tip_popups','enable_toasts','reduce_popups','created_at','updated_at') order by column_name;
select 'habitflow.clients' as required_table, to_regclass('habitflow.clients') is not null as exists;
select 'habitflow.users.client_id' as required_column, exists(select 1 from information_schema.columns where table_schema='habitflow' and table_name='users' and column_name='client_id') as exists;

-- v5.8 SuperAdmin CPF/CNPJ billing entitlement validation
select 'clients_document_normalized' as check_name, count(*) from information_schema.columns where table_schema='habitflow' and table_name='clients' and column_name='document_normalized';
select 'superadmin_audit_logs' as check_name, count(*) from information_schema.tables where table_schema='habitflow' and table_name='superadmin_audit_logs';
select 'client_invoices' as check_name, count(*) from information_schema.tables where table_schema='habitflow' and table_name='client_invoices';

-- v5.9 tenant isolation validation
select 'v5.9 users client_id' as check_name, count(*) from information_schema.columns where table_schema='habitflow' and table_name='users' and column_name='client_id';
select 'v5.9 habits client_id' as check_name, count(*) from information_schema.columns where table_schema='habitflow' and table_name='habits' and column_name='client_id';
select 'v5.9 user_invites table' as check_name, count(*) from information_schema.tables where table_schema='habitflow' and table_name='user_invites';
select 'v5.9 no public user_invites' as check_name, count(*) from information_schema.tables where table_schema='public' and table_name='user_invites';
select 'client_onboarding' as required_table where exists (select 1 from information_schema.tables where table_schema='habitflow' and table_name='client_onboarding');
select 'billing_communication_rules' as required_table where exists (select 1 from information_schema.tables where table_schema='habitflow' and table_name='billing_communication_rules');
select 'client_communications' as required_table where exists (select 1 from information_schema.tables where table_schema='habitflow' and table_name='client_communications');
select 'job_execution_logs' as required_table where exists (select 1 from information_schema.tables where table_schema='habitflow' and table_name='job_execution_logs');

-- v6.1 validations
select 'ck_habitflow_users_role accepts SuperAdmin' as check_name, count(*) as found
from information_schema.check_constraints where constraint_schema='habitflow' and constraint_name='ck_habitflow_users_role';
select table_schema, table_name from information_schema.tables where table_schema='public' and table_name in ('users','clients','client_invoices','client_subscriptions','client_communications');
select table_name from information_schema.tables where table_schema='habitflow' and table_name in ('schema_migrations','client_onboarding','client_communications','job_execution_logs','client_invoices','client_subscriptions','client_entitlement_events','superadmin_audit_logs');


-- v6.1.1 client registration CPF/CNPJ validation
select 'v6.1.1 clients.document_normalized unique' as check_name, count(*) as found from pg_indexes where schemaname='habitflow' and indexname='ux_habitflow_clients_document_normalized_not_null';
select 'v6.1.1 person/document coherence' as check_name, count(*) as found from information_schema.check_constraints where constraint_schema='habitflow' and constraint_name='ck_habitflow_clients_person_document_match';
select 'v6.1.1 CPF/CNPJ columns' as check_name, count(*) as found from information_schema.columns where table_schema='habitflow' and table_name='clients' and column_name in ('person_type','document_type','document_raw','document_normalized','legal_name','trade_name','billing_responsible_name','billing_email','billing_phone');

select '031_registration_claims_onboarding_quality expected' as check_name, count(*) as found from habitflow.schema_migrations where id='031';
select 'users.client_id exists' as check_name, count(*) as found from information_schema.columns where table_schema='habitflow' and table_name='users' and column_name='client_id';
select 'clients.document_normalized exists' as check_name, count(*) as found from information_schema.columns where table_schema='habitflow' and table_name='clients' and column_name='document_normalized';
select 'no HabitFlow tables in public' as check_name, count(*) as public_tables from information_schema.tables where table_schema='public' and table_name in ('clients','users','habits','schema_migrations');
select to_regclass('habitflow.plan_prices') as plan_prices,
       to_regclass('habitflow.feature_catalog') as feature_catalog,
       to_regclass('habitflow.plan_features') as plan_features,
       to_regclass('habitflow.roles') as roles,
       to_regclass('habitflow.permissions') as permissions;

-- v6.3 personal journey: every object must resolve inside habitflow.
select table_name, to_regclass('habitflow.'||table_name) is not null as exists from (values ('user_goals'),('goal_habits'),('milestones'),('user_milestones'),('habit_reminders'),('user_summary_preferences'),('shared_routines'),('shared_routine_habits'),('shared_routine_members'),('shared_goals'),('shared_goal_members'),('shared_goal_progress'),('product_events')) v(table_name);
select 'v6.3 no public conflicts' check_name,count(*) public_conflicts from information_schema.tables where table_schema='public' and table_name in ('user_goals','goal_habits','milestones','user_milestones','habit_reminders','user_summary_preferences','shared_routines','shared_goals','product_events');
select 'v6.3 habits visibility' check_name,count(*) found from information_schema.columns where table_schema='habitflow' and table_name='habits' and column_name='visibility';
select 'v6.16.5 challenges' check_name,to_regclass('habitflow.user_challenges') is not null as exists;

-- v6.4 operational tables must exist in habitflow.
DO $$
DECLARE object_name text;
BEGIN
 FOREACH object_name IN ARRAY ARRAY['job_locks','notification_deliveries','sharing_consents','goal_progress_events','report_snapshots'] LOOP
  IF to_regclass('habitflow.' || object_name) IS NULL THEN RAISE EXCEPTION 'Tabela obrigatória ausente: habitflow.%', object_name; END IF;
 END LOOP;
 IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname='public' AND tablename IN ('job_locks','notification_deliveries','sharing_consents','goal_progress_events','report_snapshots')) THEN
  RAISE EXCEPTION 'Objetos v6.4 não podem existir em public';
 END IF;
END $$;

-- v6.16.8 PWA/push contract
select to_regclass('habitflow.push_subscriptions') as push_subscriptions,
       to_regclass('habitflow.notification_preferences') as notification_preferences,
       to_regclass('habitflow.push_delivery_attempts') as push_delivery_attempts,
       to_regclass('habitflow.offline_sync_events') as offline_sync_events;

-- v6.16.9 assistance/support contracts
select to_regclass('habitflow.assistant_conversations') is not null as assistant_conversations_ok,
       to_regclass('habitflow.assistant_messages') is not null as assistant_messages_ok,
       to_regclass('habitflow.support_settings') is not null as support_settings_ok,
       to_regclass('habitflow.support_tickets_v2') is not null as support_tickets_v2_ok;

-- v6.17.0 SaaS administration contracts
DO $$ DECLARE object_name text; BEGIN
 FOREACH object_name IN ARRAY ARRAY['tenant_settings','user_invitations','roles','permissions','role_permissions','user_role_assignments','feature_flags','audit_events','privacy_requests','consent_records'] LOOP
  IF to_regclass('habitflow.' || object_name) IS NULL THEN RAISE EXCEPTION 'Tabela administrativa ausente: habitflow.%', object_name; END IF;
 END LOOP;
 IF EXISTS (SELECT 1 FROM pg_tables WHERE schemaname='public' AND tablename IN ('tenant_settings','user_invitations','feature_flags','audit_events','privacy_requests','consent_records')) THEN RAISE EXCEPTION 'Objetos administrativos não podem existir em public'; END IF;
END $$;
-- v6.17.3 healthy gamification parity
do $gamification_schema$
declare table_name text;
begin
  foreach table_name in array array['weekly_goals','weekly_goal_habits','achievement_definitions','user_achievements','user_missions','streak_freezes','gamification_events'] loop
    if to_regclass('habitflow.' || table_name) is null then
      raise exception 'Missing gamification table: %', table_name;
    end if;
  end loop;
  if exists(select 1 from habitflow.user_achievements group by client_id,user_id,achievement_code having count(*)>1) then
    raise exception 'Duplicate user achievement detected';
  end if;
end
$gamification_schema$;
