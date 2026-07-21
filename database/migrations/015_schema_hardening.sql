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
