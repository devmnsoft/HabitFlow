#!/usr/bin/env bash
set -euo pipefail

# Canonical entry point for existing databases and migration-based fresh databases.
# Never run this runner and database/script_completo.sql against the same database.
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
migrations_dir="$repo_root/database/migrations"
hooks_dir="$repo_root/scripts/database/compatibility-hooks"
app_version="${HABITFLOW_APP_VERSION:-development}"
connection="${1:-}"
psql_args=(-X -v ON_ERROR_STOP=1)
[[ -z "$connection" ]] || psql_args+=("$connection")

mapfile -t all_sql < <(find "$migrations_dir" -maxdepth 1 -type f -name '*.sql' -printf '%f\n' | LC_ALL=C sort)
[[ ${#all_sql[@]} -gt 0 ]] || { echo "No migrations found in $migrations_dir" >&2; exit 1; }

declare -A versions=()
migrations=()
previous=""
for filename in "${all_sql[@]}"; do
  [[ "$filename" =~ ^([0-9]{3})_[a-z0-9][a-z0-9_-]*\.sql$ ]] || {
    echo "Invalid migration filename: $filename (expected NNN_lowercase_name.sql)" >&2; exit 1;
  }
  version="${BASH_REMATCH[1]}"
  [[ "$version" != "000" ]] || { echo "Invalid migration prefix: 000" >&2; exit 1; }
  [[ -z "${versions[$version]:-}" ]] || { echo "Duplicate migration version: $version" >&2; exit 1; }
  [[ -z "$previous" || "$previous" < "$filename" ]] || { echo "Incoherent lexical order: $previous then $filename" >&2; exit 1; }
  versions[$version]="$filename"; migrations+=("$filename"); previous="$filename"
done

psql "${psql_args[@]}" <<'SQL'
create schema if not exists habitflow;
create table if not exists habitflow.schema_migrations (
  id varchar(120) primary key, name varchar(200) not null,
  applied_at timestamptz not null default now(), checksum varchar(64),
  filename varchar(260), app_version varchar(80)
);
alter table habitflow.schema_migrations add column if not exists checksum varchar(64);
alter table habitflow.schema_migrations add column if not exists filename varchar(260);
alter table habitflow.schema_migrations add column if not exists app_version varchar(80);
create table if not exists habitflow.schema_compatibility_fixes (
  name varchar(160) not null, target_version varchar(3) not null,
  checksum varchar(64) not null, applied_at timestamptz not null default now(),
  app_version varchar(80) not null,
  primary key (name, target_version)
);
alter table habitflow.schema_compatibility_fixes add column if not exists name varchar(160);
alter table habitflow.schema_compatibility_fixes add column if not exists target_version varchar(3);
do $registry$
begin
  if exists (select 1 from information_schema.columns where table_schema='habitflow'
             and table_name='schema_compatibility_fixes' and column_name='filename') then
    execute 'update habitflow.schema_compatibility_fixes
                set name=coalesce(name,filename), target_version=coalesce(target_version,''000'')
              where name is null or target_version is null';
  end if;
end $registry$;
create unique index if not exists ux_schema_compatibility_fixes_name_target
  on habitflow.schema_compatibility_fixes(name, target_version);
SQL

mapfile -t hooks < <(find "$hooks_dir" -maxdepth 1 -type f -name '*.sql' -printf '%f\n' 2>/dev/null | LC_ALL=C sort)
for hook in "${hooks[@]}"; do
  [[ "$hook" =~ ^([0-9]{3})_[a-z0-9][a-z0-9_-]*\.sql$ ]] || { echo "Invalid compatibility hook filename: $hook" >&2; exit 1; }
done

run_hooks() {
  local next_version="$1" hook hook_target hook_name hook_checksum
  for hook in "${hooks[@]}"; do
    hook_target="${hook%%_*}"
    [[ "$hook_target" == "$next_version" ]] || continue
    hook_name="${hook#*_}"; hook_name="${hook_name%.sql}"
    hook_checksum="$(sha256sum "$hooks_dir/$hook" | awk '{print $1}')"
    psql "${psql_args[@]}" -v hook_name="$hook_name" -v hook_target="$hook_target" \
      -v hook_checksum="$hook_checksum" -v hook_app_version="$app_version" \
      -f "$hooks_dir/$hook"
  done
}

printf '%-8s %-52s %-64s %-9s %s\n' VERSION FILENAME CHECKSUM STATUS TRANSACTION
for filename in "${migrations[@]}"; do
  version="${filename%%_*}"; name="${filename#*_}"; name="${name%.sql}"
  checksum="$(sha256sum "$migrations_dir/$filename" | awk '{print $1}')"
  transaction_mode="transactional"
  grep -Eq '^--[[:space:]]*habitflow:transaction=none[[:space:]]*$' "$migrations_dir/$filename" && transaction_mode="none"
  record="$(psql "${psql_args[@]}" -Atqc "select coalesce(checksum,'') || '|' || coalesce(filename,'') from habitflow.schema_migrations where id = '$version'")"
  status="pending"
  if [[ -n "$record" ]]; then
    status="applied"; recorded_checksum="${record%%|*}"; recorded_filename="${record#*|}"
    [[ -z "$recorded_checksum" || "$recorded_checksum" == "$checksum" ]] || { echo "Checksum divergence for migration $version" >&2; exit 1; }
    [[ -z "$recorded_filename" || "$recorded_filename" == "$filename" ]] || { echo "Applied migration filename divergence for $version" >&2; exit 1; }
  fi
  printf '%-8s %-52s %-64s %-9s %s\n' "$version" "$filename" "$checksum" "$status" "$transaction_mode"
  [[ "$status" == "applied" ]] || run_hooks "$version"

  sql_file="$(mktemp)"; trap 'rm -f "$sql_file"' EXIT
  cat >"$sql_file" <<SQL
begin;
select pg_advisory_xact_lock(76467001);
select not exists (select 1 from habitflow.schema_migrations where id = '$version') as should_apply \gset
\if :should_apply
\i $migrations_dir/$filename
insert into habitflow.schema_migrations(id,name,checksum,filename,app_version)
values ('$version','$name','$checksum','$filename','$app_version');
\endif
update habitflow.schema_migrations set checksum=coalesce(checksum,'$checksum'),
 filename=coalesce(filename,'$filename'), app_version=coalesce(app_version,'$app_version') where id='$version';
commit;
SQL
  psql "${psql_args[@]}" -f "$sql_file"
  rm -f "$sql_file"; trap - EXIT
done
