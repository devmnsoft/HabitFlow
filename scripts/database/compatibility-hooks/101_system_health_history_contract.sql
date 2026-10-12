begin;

select pg_advisory_xact_lock(76467101);

select set_config('habitflow.compatibility_hook', :'hook_name', true);
select set_config('habitflow.compatibility_target', :'hook_target', true);
select set_config('habitflow.compatibility_checksum', :'hook_checksum', true);
select set_config('habitflow.compatibility_app_version', :'hook_app_version', true);

do $$
begin
  if to_regclass('habitflow.system_health_history') is not null then
    alter table habitflow.system_health_history add column if not exists component_name varchar(60);
    alter table habitflow.system_health_history add column if not exists duration_ms integer not null default 0;
    alter table habitflow.system_health_history add column if not exists error_message text null;
    alter table habitflow.system_health_history add column if not exists recorded_at timestamptz;

    update habitflow.system_health_history
       set component_name = coalesce(component_name, check_name, 'system'),
           recorded_at = coalesce(recorded_at, checked_at, now()),
           error_message = coalesce(error_message, message)
     where component_name is null
        or recorded_at is null
        or error_message is null;

    alter table habitflow.system_health_history alter column component_name set not null;
    alter table habitflow.system_health_history alter column recorded_at set default now();
    alter table habitflow.system_health_history alter column recorded_at set not null;
  end if;

  if to_regclass('habitflow.consent_records') is not null then
    alter table habitflow.consent_records add column if not exists consent_type varchar(60);
    alter table habitflow.consent_records add column if not exists policy_version varchar(30);
    alter table habitflow.consent_records add column if not exists ip_address varchar(45) null;
    alter table habitflow.consent_records add column if not exists user_agent text null;
    alter table habitflow.consent_records add column if not exists granted_at timestamptz;
    alter table habitflow.consent_records add column if not exists revoked_at timestamptz null;

    update habitflow.consent_records
       set consent_type = coalesce(
               consent_type,
               case
                 when purpose in ('TermsOfService', 'PrivacyPolicy', 'Marketing', 'Analytics', 'DataProcessing') then purpose
                 else 'DataProcessing'
               end),
           policy_version = coalesce(policy_version, document_version, '1.0'),
           granted_at = coalesce(granted_at, recorded_at, now())
     where consent_type is null
        or policy_version is null
        or granted_at is null;

    alter table habitflow.consent_records alter column consent_type set not null;
    alter table habitflow.consent_records alter column policy_version set default '1.0';
    alter table habitflow.consent_records alter column policy_version set not null;
    alter table habitflow.consent_records alter column granted_at set default now();
    alter table habitflow.consent_records alter column granted_at set not null;
  end if;
end $$;

insert into habitflow.schema_compatibility_fixes(name, target_version, checksum, app_version)
values (current_setting('habitflow.compatibility_hook'),
        current_setting('habitflow.compatibility_target'),
        current_setting('habitflow.compatibility_checksum'),
        current_setting('habitflow.compatibility_app_version'))
on conflict (name, target_version) do update
set checksum = excluded.checksum,
    app_version = excluded.app_version,
    applied_at = now();

commit;
