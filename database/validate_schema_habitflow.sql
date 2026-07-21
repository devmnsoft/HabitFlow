\set ON_ERROR_STOP on
-- HabitFlow v4.5 - validação do schema oficial habitflow.

with required_tables(table_name) as (values
 ('users'),('habits'),('habit_completions'),('support_tickets'),('support_messages'),
 ('system_audit_logs'),('admin_audit_logs'),('system_settings'),('lgpd_requests'),
 ('billing_events'),('notifications'),('habit_objectives'),('habit_templates'),('user_reports'),('plans'),('subscriptions'),('payment_transactions'),('payment_webhook_events'),('payment_audit_logs')
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
 if exists(with r(table_name) as (values ('users'),('habits'),('habit_completions'),('support_tickets'),('support_messages'),('system_audit_logs'),('admin_audit_logs'),('system_settings'),('lgpd_requests'),('billing_events'),('notifications'),('habit_objectives'),('habit_templates'),('user_reports'),('plans'),('subscriptions'),('payment_transactions'),('payment_webhook_events'),('payment_audit_logs')) select 1 from r left join information_schema.tables t on t.table_schema='habitflow' and t.table_name=r.table_name where t.table_name is null) then errors := array_append(errors,'há tabelas obrigatórias ausentes em habitflow'); end if;
 if (select count(*) from habitflow.system_settings where key in ('companyName','companyLegalName','companyCnpj','commercialEmail','supportEmail')) < 5 then errors := array_append(errors,'settings mínimos MNSOFT ausentes'); end if;
 if array_length(errors,1) is not null then raise exception 'Validação HabitFlow falhou: %', array_to_string(errors,'; '); end if;
end $$;
