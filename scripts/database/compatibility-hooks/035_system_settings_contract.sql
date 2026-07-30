\set ON_ERROR_STOP on

-- Target: migration 035. The runner supplies identity/checksum metadata and invokes
-- this hook immediately before its target, never as an unconditional bootstrap step.
begin;
select pg_advisory_xact_lock(76467002);
select set_config('habitflow.hook_name', :'hook_name', true);
select set_config('habitflow.hook_target', :'hook_target', true);
select set_config('habitflow.hook_checksum', :'hook_checksum', true);
select set_config('habitflow.hook_app_version', :'hook_app_version', true);

do $hook$
declare recorded_checksum varchar(64);
begin
  select checksum into recorded_checksum
    from habitflow.schema_compatibility_fixes
   where name = current_setting('habitflow.hook_name')
     and target_version = current_setting('habitflow.hook_target');
  if recorded_checksum is not null and recorded_checksum <> current_setting('habitflow.hook_checksum') then
    raise exception 'Checksum divergence for compatibility hook %', current_setting('habitflow.hook_name');
  end if;

  if recorded_checksum is null
     and to_regclass('habitflow.system_settings') is not null
     and not exists (select 1 from habitflow.schema_migrations where id = current_setting('habitflow.hook_target')) then
    alter table habitflow.system_settings add column if not exists description text null;
    alter table habitflow.system_settings add column if not exists is_public boolean not null default false;
    alter table habitflow.system_settings add column if not exists created_at timestamp not null default now();
    insert into habitflow.schema_compatibility_fixes(name,target_version,checksum,app_version)
    values (current_setting('habitflow.hook_name'), current_setting('habitflow.hook_target'),
            current_setting('habitflow.hook_checksum'), current_setting('habitflow.hook_app_version'));
  end if;
end $hook$;
commit;
