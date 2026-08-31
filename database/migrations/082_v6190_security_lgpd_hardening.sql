-- HabitFlow v6.19.0: LGPD governance, immutable consent history and tenant-safe portability.
begin;
set local search_path to habitflow, public;

create table if not exists security_audit_events (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid references users(id),
 event_type varchar(100) not null, severity varchar(20) not null check(severity in ('Info','Warning','Critical')),
 correlation_id varchar(100), sanitized_details jsonb not null default '{}'::jsonb,
 occurred_at timestamptz not null default now());
create index if not exists ix_security_audit_tenant_type_time on security_audit_events(client_id,event_type,occurred_at desc);
create index if not exists ix_security_audit_tenant_severity_time on security_audit_events(client_id,severity,occurred_at desc);

create table if not exists user_consent_history (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 consent_key varchar(60) not null check(consent_key in ('terms','privacy','analytics','notifications','assistant_context')),
 document_version varchar(40) not null, granted boolean not null, occurred_at timestamptz not null default now());
create index if not exists ix_consent_history_owner_purpose_time on user_consent_history(client_id,user_id,consent_key,occurred_at desc);

create table if not exists data_exports (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 format varchar(10) not null check(format in ('JSON','CSV')), status varchar(20) not null check(status in ('Requested','Processing','Completed','Failed','Expired')),
 storage_key varchar(300), expires_at timestamptz, created_at timestamptz not null default now(), completed_at timestamptz,
 check(storage_key is null or storage_key not like '%..%'));
create index if not exists ix_data_exports_owner_status_time on data_exports(client_id,user_id,status,created_at desc);

create table if not exists account_deletion_requests (
 id uuid primary key, client_id uuid not null references clients(id), user_id uuid not null references users(id),
 status varchar(20) not null check(status in ('Requested','Confirmed','Processing','Completed','Canceled','Failed')),
 confirmation_token_hash varchar(128), requested_at timestamptz not null default now(), confirmed_at timestamptz,
 processing_started_at timestamptz, completed_at timestamptz, canceled_at timestamptz,
 failure_code varchar(80));
create unique index if not exists ux_account_deletion_active on account_deletion_requests(client_id,user_id) where status in ('Requested','Confirmed','Processing');
create index if not exists ix_account_deletion_tenant_status_time on account_deletion_requests(client_id,status,requested_at desc);

-- SECURITY DEFINER is intentionally not used: the caller retains the application's DB privileges.
-- Every branch is anchored to both tenant and user. Secrets, auth/session and billing tables are excluded.
create or replace function export_user_data_json(p_client_id uuid, p_user_id uuid)
returns jsonb language sql stable as $$
 select case when exists(select 1 from users u where u.id=p_user_id and u.client_id=p_client_id) then jsonb_build_object(
  'schemaVersion','6.19.0','exportedAtUtc',now(),
  'profile',(select to_jsonb(x) from (select u.id,u.name,u.email,u.created_at,u.accepted_terms_at,u.accepted_privacy_at from users u where u.id=p_user_id and u.client_id=p_client_id) x),
  'habits',coalesce((select jsonb_agg(to_jsonb(x)) from (select h.id,h.name,h.category,h.is_archived,h.created_at,h.updated_at from habits h where h.user_id=p_user_id and h.client_id=p_client_id order by h.created_at) x),'[]'::jsonb),
  'goals',coalesce((select jsonb_agg(to_jsonb(x)) from (select g.id,g.name,g.week_start,g.week_end,g.target_completions,g.current_completions,g.status,g.created_at from weekly_goals g where g.client_id=p_client_id and g.user_id=p_user_id order by g.created_at) x),'[]'::jsonb),
  'routines',coalesce((select jsonb_agg(to_jsonb(x)) from (select r.id,r.habit_id,r.local_date,r.preferred_time,r.sort_order,r.created_at,r.updated_at from daily_routine_overrides r where r.client_id=p_client_id and r.user_id=p_user_id order by r.local_date,r.sort_order) x),'[]'::jsonb),
  'completions',coalesce((select jsonb_agg(to_jsonb(x)) from (select c.id,c.habit_id,c.completed_date,c.created_at from habit_completions c join habits h on h.id=c.habit_id where c.user_id=p_user_id and h.client_id=p_client_id order by c.completed_date) x),'[]'::jsonb),
  'preferences',coalesce((select jsonb_agg(to_jsonb(x)) from (select p.habits_private,p.share_program_progress,p.updated_at from privacy_preferences p where p.client_id=p_client_id and p.user_id=p_user_id) x),'[]'::jsonb),
  'notifications',coalesce((select jsonb_agg(to_jsonb(x)) from (select n.id,n.type,n.title,n.is_read,n.created_at,n.read_at from notifications n join users u on u.id=n.user_id where n.user_id=p_user_id and u.client_id=p_client_id order by n.created_at) x),'[]'::jsonb),
  'achievements',coalesce((select jsonb_agg(to_jsonb(x)) from (select a.achievement_code,a.status,a.unlocked_at from user_achievements a where a.client_id=p_client_id and a.user_id=p_user_id order by a.unlocked_at) x),'[]'::jsonb),
  'consents',coalesce((select jsonb_agg(to_jsonb(x)) from (select c.consent_key,c.granted,c.updated_at from user_privacy_consents c join users u on u.id=c.user_id where c.user_id=p_user_id and u.client_id=p_client_id order by c.consent_key) x),'[]'::jsonb)
 ) else '{}'::jsonb end;
$$;
commit;
