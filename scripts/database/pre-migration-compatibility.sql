\set ON_ERROR_STOP on

create schema if not exists habitflow;
create table if not exists habitflow.schema_compatibility_fixes (
    id bigserial primary key,
    filename varchar(260) not null unique,
    checksum varchar(64) not null,
    applied_at timestamptz not null default now(),
    app_version varchar(80) not null
);

select set_config('habitflow.compatibility_filename', :'compatibility_filename', false);
select set_config('habitflow.compatibility_checksum', :'compatibility_checksum', false);
select set_config('habitflow.compatibility_app_version', :'compatibility_app_version', false);

do $compatibility$
declare
    migration_035_pending boolean;
    fix_recorded boolean;
begin
    if exists (
        select 1 from habitflow.schema_compatibility_fixes
        where filename = current_setting('habitflow.compatibility_filename')
          and checksum <> current_setting('habitflow.compatibility_checksum')
    ) then
        raise exception 'Checksum divergence for compatibility fix %', current_setting('habitflow.compatibility_filename');
    end if;

    select not exists (
        select 1 from habitflow.schema_migrations where id = '035'
    ) into migration_035_pending;

    select exists (
        select 1 from habitflow.schema_compatibility_fixes
        where filename = current_setting('habitflow.compatibility_filename')
    ) into fix_recorded;

    if to_regclass('habitflow.system_settings') is not null
       and migration_035_pending and not fix_recorded then
        alter table habitflow.system_settings add column if not exists description text null;
        alter table habitflow.system_settings add column if not exists is_public boolean not null default false;
        alter table habitflow.system_settings add column if not exists created_at timestamp not null default now();

        insert into habitflow.schema_compatibility_fixes(filename, checksum, app_version)
        values (current_setting('habitflow.compatibility_filename'), current_setting('habitflow.compatibility_checksum'), current_setting('habitflow.compatibility_app_version'));
    end if;
end
$compatibility$;
