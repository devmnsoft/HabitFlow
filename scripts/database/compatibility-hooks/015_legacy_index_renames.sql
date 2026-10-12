\set ON_ERROR_STOP on

-- Target: migration 015. Migration 015 renames legacy index names (ix_*) to the
-- canonical habitflow-prefixed names. Databases built from the aggregate script
-- (database/script_completo.sql) already carry the canonical names, so both the
-- legacy and the canonical index exist and the plain rename aborts with
-- "relation ... already exists" (SQLSTATE 42P07). Keep migration 015 immutable
-- and bridge the existing database before it runs: drop the redundant legacy
-- index only when the canonical twin already exists on the same table.
begin;
select pg_advisory_xact_lock(76467003);
select set_config('habitflow.hook_name', :'hook_name', true);
select set_config('habitflow.hook_target', :'hook_target', true);
select set_config('habitflow.hook_checksum', :'hook_checksum', true);
select set_config('habitflow.hook_app_version', :'hook_app_version', true);

do $hook$
declare
  pair text[];
  schema_name constant text := 'habitflow';
  recorded_checksum varchar(64);
  legacy regclass;
  dropped integer := 0;
begin
  select checksum into recorded_checksum
    from habitflow.schema_compatibility_fixes
   where name = current_setting('habitflow.hook_name')
     and target_version = current_setting('habitflow.hook_target');
  if recorded_checksum is not null and recorded_checksum <> current_setting('habitflow.hook_checksum') then
    raise exception 'Checksum divergence for compatibility hook %', current_setting('habitflow.hook_name');
  end if;

  if recorded_checksum is null and not exists (select 1 from habitflow.schema_migrations where id = current_setting('habitflow.hook_target')) then
    foreach pair slice 1 in array array[
      array['habitflow.users','ix_users_email','ix_habitflow_users_email'],
      array['habitflow.users','ix_users_role','ix_habitflow_users_role'],
      array['habitflow.users','ix_users_account_status','ix_habitflow_users_account_status'],
      array['habitflow.users','ix_users_plan','ix_habitflow_users_plan'],
      array['habitflow.habits','ix_habits_user_id','ix_habitflow_habits_user_id'],
      array['habitflow.habit_completions','ix_habit_completions_user_id','ix_habitflow_habit_completions_user_id'],
      array['habitflow.support_tickets','ix_support_tickets_user_id','ix_habitflow_support_tickets_user_id'],
      array['habitflow.lgpd_requests','ix_lgpd_requests_user_id','ix_habitflow_lgpd_requests_user_id'],
      array['habitflow.system_audit_logs','ix_system_audit_logs_created_at','ix_habitflow_system_audit_logs_created_at'],
      array['habitflow.admin_audit_logs','ix_admin_audit_logs_created_at','ix_habitflow_admin_audit_logs_created_at']
    ] loop
      legacy := to_regclass(schema_name || '.' || pair[2]);
      if legacy is not null
         and to_regclass(schema_name || '.' || pair[3]) is not null
         and (select indrelid from pg_index where indexrelid = legacy) = to_regclass(pair[1]) then
        execute format('drop index %I.%I', schema_name, pair[2]);
        dropped := dropped + 1;
      end if;
    end loop;

    if dropped > 0 then
      insert into habitflow.schema_compatibility_fixes(name,target_version,checksum,app_version)
      values (current_setting('habitflow.hook_name'), current_setting('habitflow.hook_target'),
              current_setting('habitflow.hook_checksum'), current_setting('habitflow.hook_app_version'));
    end if;
  end if;
end $hook$;
commit;
