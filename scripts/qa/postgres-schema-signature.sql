-- Stable structural signature used to compare aggregate and canonical bootstraps.
-- Runner bookkeeping tables are excluded on purpose:
--   * schema_compatibility_fixes only exists on the canonical runner (the
--     aggregate bundle inlines the hook statements instead of the hook registry);
--   * schema_migrations is bootstrapped by each path with its own DDL
--     (runner preamble vs aggregate include), which is covered separately by the
--     registry contract check, not by application-schema parity.
\pset tuples_only on
\pset format unaligned
select md5(string_agg(item, E'\n' order by item))
from (
  select 'column|' || table_name || '|' || column_name || '|' || data_type || '|' ||
         coalesce(character_maximum_length::text, '') || '|' || is_nullable || '|' || coalesce(column_default, '') item
  from information_schema.columns
  where table_schema = 'habitflow'
    and table_name not in ('schema_migrations', 'schema_compatibility_fixes')
  union all
  select 'constraint|' || c.relname || '|' || con.conname || '|' || con.contype::text || '|' || pg_get_constraintdef(con.oid)
  from pg_constraint con join pg_class c on c.oid=con.conrelid join pg_namespace n on n.oid=c.relnamespace
  where n.nspname='habitflow'
    and c.relname not in ('schema_migrations', 'schema_compatibility_fixes')
  union all
  select 'index|' || tablename || '|' || indexname || '|' || indexdef
  from pg_indexes
  where schemaname='habitflow'
    and tablename not in ('schema_migrations', 'schema_compatibility_fixes')
) signature_items;
