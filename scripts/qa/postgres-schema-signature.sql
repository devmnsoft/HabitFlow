-- Stable structural signature used to compare aggregate and canonical bootstraps.
\pset tuples_only on
\pset format unaligned
select md5(string_agg(item, E'\n' order by item))
from (
  select 'column|' || table_name || '|' || column_name || '|' || data_type || '|' ||
         coalesce(character_maximum_length::text, '') || '|' || is_nullable || '|' || coalesce(column_default, '') item
  from information_schema.columns where table_schema = 'habitflow'
  union all
  select 'constraint|' || c.relname || '|' || con.conname || '|' || con.contype || '|' || pg_get_constraintdef(con.oid)
  from pg_constraint con join pg_class c on c.oid=con.conrelid join pg_namespace n on n.oid=c.relnamespace
  where n.nspname='habitflow'
  union all
  select 'index|' || tablename || '|' || indexname || '|' || indexdef
  from pg_indexes where schemaname='habitflow'
) signature_items;
