\set ON_ERROR_STOP on

-- Target: migration 098. Migration 082 introduced security_audit_events with
-- occurred_at/event_type; migration 098 indexes created_at. Keep the canonical
-- migration immutable and bridge the existing table contract before 098 runs.
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
     and to_regclass('habitflow.security_audit_events') is not null
     and not exists (select 1 from habitflow.schema_migrations where id = current_setting('habitflow.hook_target')) then
    alter table habitflow.security_audit_events add column if not exists event_code varchar(80);
    alter table habitflow.security_audit_events add column if not exists result varchar(30) not null default 'Recorded';
    alter table habitflow.security_audit_events add column if not exists metadata_json text null;
    alter table habitflow.security_audit_events add column if not exists created_at timestamptz;
    update habitflow.security_audit_events
       set event_code = coalesce(event_code, event_type, 'security.event'),
           created_at = coalesce(created_at, occurred_at, now())
     where event_code is null or created_at is null;
    alter table habitflow.security_audit_events alter column event_code set not null;
    alter table habitflow.security_audit_events alter column created_at set default now();
    alter table habitflow.security_audit_events alter column created_at set not null;

    insert into habitflow.schema_compatibility_fixes(name,target_version,checksum,app_version)
    values (current_setting('habitflow.hook_name'), current_setting('habitflow.hook_target'),
            current_setting('habitflow.hook_checksum'), current_setting('habitflow.hook_app_version'));
  end if;
end $hook$;
commit;
