#!/usr/bin/env bash
set -euo pipefail

# CANONICAL ENTRY POINT FOR EXISTING DATABASES (and migration-based fresh DBs).
# Never run this runner and database/script_completo.sql against the same database.
# PostgreSQL connection settings are supplied
# through the standard PG* variables (or a psql connection URI as $1).
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
migrations_dir="$repo_root/database/migrations"
app_version="${HABITFLOW_APP_VERSION:-development}"
connection="${1:-}"
psql_args=(-X -v ON_ERROR_STOP=1)
if [[ -n "$connection" ]]; then psql_args+=("$connection"); fi

mapfile -t migrations < <(find "$migrations_dir" -maxdepth 1 -type f -name '[0-9][0-9][0-9]_*.sql' -printf '%f\n' | LC_ALL=C sort)
[[ ${#migrations[@]} -gt 0 ]] || { echo "No migrations found in $migrations_dir" >&2; exit 1; }

declare -A versions=()
expected=1
for filename in "${migrations[@]}"; do
  version="${filename%%_*}"
  [[ -z "${versions[$version]:-}" ]] || { echo "Duplicate migration version: $version" >&2; exit 1; }
  versions[$version]=1
  number=$((10#$version))
  [[ $number -eq $expected ]] || { echo "Migration gap: expected $(printf '%03d' "$expected"), found $version" >&2; exit 1; }
  expected=$((expected + 1))
done

psql "${psql_args[@]}" <<'SQL'
create schema if not exists habitflow;
create table if not exists habitflow.schema_migrations (
  id varchar(120) primary key,
  name varchar(200) not null,
  applied_at timestamp with time zone not null default now(),
  checksum varchar(64),
  filename varchar(260),
  app_version varchar(80)
);
alter table habitflow.schema_migrations add column if not exists checksum varchar(64);
alter table habitflow.schema_migrations add column if not exists filename varchar(260);
alter table habitflow.schema_migrations add column if not exists app_version varchar(80);
SQL

for filename in "${migrations[@]}"; do
  version="${filename%%_*}"
  name="${filename#*_}"; name="${name%.sql}"
  checksum="$(sha256sum "$migrations_dir/$filename" | awk '{print $1}')"
  sql_file="$(mktemp)"
  trap 'rm -f "$sql_file"' EXIT
  cat >"$sql_file" <<SQL
begin;
select pg_advisory_xact_lock(76467001);
do \$migration\$
declare recorded varchar(64);
begin
  select checksum into recorded from habitflow.schema_migrations where id = '$version';
  if recorded is not null and recorded <> '$checksum' then
    raise exception 'Checksum divergence for migration $version. Create a forward fix; do not edit an applied migration.';
  end if;
end \$migration\$;
select not exists (select 1 from habitflow.schema_migrations where id = '$version') as should_apply \gset
\if :should_apply
\i $migrations_dir/$filename
insert into habitflow.schema_migrations(id, name, checksum, filename, app_version, applied_at)
values ('$version', '$name', '$checksum', '$filename', '$app_version', now())
on conflict (id) do update set
  name = excluded.name,
  checksum = excluded.checksum,
  filename = excluded.filename,
  app_version = excluded.app_version;
\endif
update habitflow.schema_migrations
set checksum = coalesce(checksum, '$checksum'),
    filename = coalesce(filename, '$filename'),
    app_version = coalesce(app_version, '$app_version')
where id = '$version';
commit;
SQL
  psql "${psql_args[@]}" -f "$sql_file"
  rm -f "$sql_file"
  trap - EXIT
  echo "Migration $version verified: $filename"
done
