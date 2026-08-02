#!/usr/bin/env bash
set -euo pipefail

# Canonical entry point for existing databases and migration-based fresh databases.
# Never run this runner and database/script_completo.sql against the same database.
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
migrations_dir="$repo_root/database/migrations"
hooks_dir="$repo_root/scripts/database/compatibility-hooks"
execution_modes_file="$repo_root/scripts/database/migration-execution-modes.conf"
app_version="${HABITFLOW_APP_VERSION:-development}"
connection=""
to_version=""

usage() {
  cat <<'USAGE'
Usage: run-migrations.sh [connection] [--to-version NNN]

Applies the canonical migration stream, optionally stopping after NNN. The
connection can be omitted when the standard PG* environment variables are set.
USAGE
}

while (($#)); do
  case "$1" in
    --to-version)
      (($# >= 2)) || { echo "--to-version requires a value" >&2; usage >&2; exit 2; }
      to_version="$2"; shift 2 ;;
    --to-version=*) to_version="${1#*=}"; shift ;;
    -h|--help) usage; exit 0 ;;
    --*) echo "Unknown option: $1" >&2; usage >&2; exit 2 ;;
    *)
      [[ -z "$connection" ]] || { echo "Only one connection argument is supported" >&2; usage >&2; exit 2; }
      connection="$1"; shift ;;
  esac
done

[[ -z "$to_version" || "$to_version" =~ ^[0-9]{3}$ ]] || {
  echo "Invalid --to-version value: $to_version (expected NNN)" >&2; exit 2;
}
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

if [[ -n "$to_version" && -z "${versions[$to_version]:-}" ]]; then
  echo "Migration version $to_version does not exist" >&2
  exit 2
fi

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

declare -A manifest_modes=()
if [[ -f "$execution_modes_file" ]]; then
  while IFS='=' read -r manifest_version manifest_mode; do
    [[ -z "$manifest_version" || "$manifest_version" == \#* ]] && continue
    [[ "$manifest_version" =~ ^[0-9]{3}$ && "$manifest_mode" =~ ^(runner|self|none)$ ]] || {
      echo "Invalid migration execution mode: $manifest_version=$manifest_mode" >&2; exit 1;
    }
    manifest_modes[$manifest_version]="$manifest_mode"
  done < "$execution_modes_file"
fi

classify_migration() {
  local file="$1" version="$2" header
  header="$(sed -nE 's/^--[[:space:]]*habitflow:transaction=(runner|self|none)[[:space:]]*$/\1/p' "$file" | head -1)"
  if [[ -n "$header" ]]; then printf '%s|header' "$header"; return; fi
  if [[ -n "${manifest_modes[$version]:-}" ]]; then printf '%s|manifest' "${manifest_modes[$version]}"; return; fi
  if grep -Eiq '^[[:space:]]*(begin|commit)[[:space:]]*;' "$file"; then printf 'self|legacy-detection'; return; fi
  printf 'runner|legacy-detection'
}

printf '%-8s %-52s %-64s %-9s %-10s %s\n' VERSION FILENAME CHECKSUM STATUS MODE SOURCE
for filename in "${migrations[@]}"; do
  version="${filename%%_*}"; name="${filename#*_}"; name="${name%.sql}"
  [[ -z "$to_version" || ! "$version" > "$to_version" ]] || continue
  checksum="$(sha256sum "$migrations_dir/$filename" | awk '{print $1}')"
  classification="$(classify_migration "$migrations_dir/$filename" "$version")"
  transaction_mode="${classification%%|*}"; mode_source="${classification#*|}"
  record="$(psql "${psql_args[@]}" -Atqc "select coalesce(checksum,'') || '|' || coalesce(filename,'') from habitflow.schema_migrations where id = '$version'")"
  status="pending"
  if [[ -n "$record" ]]; then
    status="applied"; recorded_checksum="${record%%|*}"; recorded_filename="${record#*|}"
    [[ -z "$recorded_checksum" || "$recorded_checksum" == "$checksum" ]] || { echo "Checksum divergence for migration $version" >&2; exit 1; }
    [[ -z "$recorded_filename" || "$recorded_filename" == "$filename" ]] || { echo "Applied migration filename divergence for $version" >&2; exit 1; }
  fi
  printf '%-8s %-52s %-64s %-9s %-10s %s\n' "$version" "$filename" "$checksum" "$status" "$transaction_mode" "$mode_source"
  [[ "$status" == "applied" ]] || run_hooks "$version"

  sql_file="$(mktemp)"; trap 'rm -f "$sql_file"' EXIT
  cat >"$sql_file" <<SQL
select pg_advisory_lock(76467001);
select not exists (select 1 from habitflow.schema_migrations where id = '$version') as should_apply \gset
\if :should_apply
create temporary table hf_schema_migrations_before on commit preserve rows as
select id from habitflow.schema_migrations;
SQL
  if [[ "$transaction_mode" == "runner" ]]; then echo 'begin;' >>"$sql_file"; fi
  cat >>"$sql_file" <<SQL
\i $migrations_dir/$filename
SQL
  if [[ "$transaction_mode" == "runner" ]]; then
    # Reconciliation remains in the runner transaction, making schema and registry atomic.
    :
  elif [[ "$transaction_mode" == "self" ]]; then
    # The included file has completed its own transaction. Reconcile separately while
    # the session advisory lock and PRESERVE ROWS snapshot are still alive.
    echo 'begin;' >>"$sql_file"
  else
    # Non-transactional DDL is complete; registry reconciliation is transactional.
    echo 'begin;' >>"$sql_file"
  fi
  cat >>"$sql_file" <<SQL
do \$migration_registry\$
declare
  unexpected_ids text;
  registered habitflow.schema_migrations%rowtype;
begin
  -- A legacy migration may register itself. It must not register any other id.
  select string_agg(m.id, ', ' order by m.id) into unexpected_ids
    from habitflow.schema_migrations m
    left join hf_schema_migrations_before b on b.id = m.id
   where b.id is null and m.id <> '$version';
  if unexpected_ids is not null then
    raise exception 'Migration $version registered unexpected version(s): %', unexpected_ids;
  end if;

  select * into registered
    from habitflow.schema_migrations
   where id = '$version'
   for update;

  if found and registered.checksum is not null and registered.checksum <> '$checksum' then
    raise exception 'Checksum divergence for migration $version';
  end if;
  if found and registered.filename is not null and registered.filename <> '$filename' then
    raise exception 'Applied migration filename divergence for $version';
  end if;

  insert into habitflow.schema_migrations(id,name,checksum,filename,app_version)
  values ('$version','$name','$checksum','$filename','$app_version')
  on conflict (id) do update
    set checksum = coalesce(habitflow.schema_migrations.checksum, excluded.checksum),
        filename = coalesce(habitflow.schema_migrations.filename, excluded.filename),
        app_version = coalesce(habitflow.schema_migrations.app_version, excluded.app_version);
end
\$migration_registry\$;
drop table hf_schema_migrations_before;
commit;
\endif
do \$migration_registry_validation\$
declare registered habitflow.schema_migrations%rowtype;
begin
  select * into registered from habitflow.schema_migrations where id = '$version' for update;
  if not found then
    raise exception 'Migration $version did not register the expected version';
  end if;
  if registered.checksum is not null and registered.checksum <> '$checksum' then
    raise exception 'Checksum divergence for migration $version';
  end if;
  if registered.filename is not null and registered.filename <> '$filename' then
    raise exception 'Applied migration filename divergence for $version';
  end if;
  update habitflow.schema_migrations
     set checksum = coalesce(checksum, '$checksum'),
         filename = coalesce(filename, '$filename'),
         app_version = coalesce(app_version, '$app_version')
   where id = '$version';
end
\$migration_registry_validation\$;
select pg_advisory_unlock(76467001);
SQL
  psql "${psql_args[@]}" -f "$sql_file"
  rm -f "$sql_file"; trap - EXIT
done
