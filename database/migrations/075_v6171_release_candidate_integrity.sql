-- HabitFlow v6.17.1: honest feature states and indexes for tenant-scoped hot paths.
begin;
set local search_path to habitflow, public;

-- Disabled is an explicit product state. Internal remains reserved for operational
-- capabilities which are real but must never be advertised as customer benefits.
update habitflow.feature_catalog
   set implementation_status = 'Disabled', is_marketable = false
 where not is_active;

do $catalog_contract$
begin
  if exists (select 1 from habitflow.feature_catalog
              where implementation_status not in ('Implemented','Partial','Planned','Disabled','Internal','Deprecated')) then
    raise exception 'feature_catalog contains an unsupported implementation status';
  end if;
  if exists (select 1 from habitflow.feature_catalog
              where is_marketable and implementation_status <> 'Implemented') then
    raise exception 'only implemented features may be marketable';
  end if;
end
$catalog_contract$;

create index if not exists ix_habit_completions_tenant_user_day
  on habitflow.habit_completions(client_id,user_id,completed_date desc);
create index if not exists ix_notifications_tenant_user_center
  on habitflow.notifications(client_id,user_id,is_archived,is_read,created_at desc);
create index if not exists ix_habits_tenant_user_active
  on habitflow.habits(client_id,user_id,created_at desc) where not is_archived;

commit;
