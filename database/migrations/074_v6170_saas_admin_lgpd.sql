-- HabitFlow v6.17.0: tenant-scoped SaaS administration, RBAC, audit, flags and LGPD.
begin;
set local search_path to habitflow, public;

create table if not exists habitflow.tenant_settings (
 client_id uuid primary key references habitflow.clients(id), slug varchar(80) not null,
 timezone varchar(80) not null default 'America/Sao_Paulo', language varchar(10) not null default 'pt-BR',
 logo_url varchar(500), theme varchar(40), support_email varchar(254), support_whatsapp varchar(24),
 retention_days integer not null default 730 check(retention_days between 30 and 3650), status varchar(20) not null default 'Active' check(status in ('Active','Suspended','Archived')),
 updated_by_user_id uuid references habitflow.users(id), updated_at timestamptz not null default now());
create unique index if not exists ux_tenant_settings_slug_lower on habitflow.tenant_settings(lower(slug));

alter table habitflow.roles add column if not exists created_at timestamptz not null default now();
insert into habitflow.roles(id,code,name,scope,description,is_system,is_active) values
 ('61700000-0000-0000-0000-000000000001','owner','Owner','Client','Controle total do tenant',true,true),
 ('61700000-0000-0000-0000-000000000002','admin','Admin','Client','Operação administrativa',true,true),
 ('61700000-0000-0000-0000-000000000003','support','Support','Client','Atendimento',true,true),
 ('61700000-0000-0000-0000-000000000004','billing_admin','BillingAdmin','Client','Cobrança',true,true),
 ('61700000-0000-0000-0000-000000000005','read_only','ReadOnly','Client','Consulta administrativa',true,true)
on conflict(code) do update set name=excluded.name,description=excluded.description;
insert into habitflow.permissions(code,name,category) select code,replace(code,'_',' '),split_part(code,'.',1) from unnest(array[
 'admin.dashboard.read','users.read','users.invite','users.update_role','users.disable','billing.read','billing.manage',
 'support.read','support.reply','audit.read','feature_flags.manage','privacy.manage','system_health.read']) code on conflict(code) do nothing;
insert into habitflow.role_permissions(role_id,permission_code)
select r.id,p.code from habitflow.roles r cross join habitflow.permissions p where
 r.code='owner' or
 (r.code='admin' and p.code <> 'billing.manage') or
 (r.code='support' and p.code in ('admin.dashboard.read','users.read','support.read','support.reply')) or
 (r.code='billing_admin' and p.code in ('admin.dashboard.read','billing.read','billing.manage')) or
 (r.code='read_only' and p.code in ('admin.dashboard.read','users.read','billing.read','support.read','audit.read','system_health.read'))
on conflict do nothing;

create table if not exists habitflow.user_invitations (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), email_normalized varchar(254) not null,
 token_hash varchar(128) not null unique, role_id uuid not null references habitflow.roles(id), invited_by_user_id uuid not null references habitflow.users(id),
 expires_at timestamptz not null, accepted_at timestamptz, accepted_by_user_id uuid references habitflow.users(id), revoked_at timestamptz,
 created_at timestamptz not null default now(), constraint ck_user_invitations_lifecycle check(accepted_at is null or revoked_at is null));
create index if not exists ix_user_invitations_tenant_email on habitflow.user_invitations(client_id,email_normalized,created_at desc);

create table if not exists habitflow.feature_flags (
 id uuid primary key, code varchar(100) not null, environment varchar(40) not null, client_id uuid references habitflow.clients(id), plan_code varchar(40),
 enabled boolean not null default false, starts_at timestamptz, ends_at timestamptz, updated_by_user_id uuid references habitflow.users(id),
 created_at timestamptz not null default now(), updated_at timestamptz not null default now(), check(ends_at is null or starts_at is null or ends_at>starts_at));
create unique index if not exists ux_feature_flags_scope on habitflow.feature_flags(code,environment,coalesce(client_id,'00000000-0000-0000-0000-000000000000'::uuid),coalesce(plan_code,''));

create table if not exists habitflow.audit_events (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), actor_user_id uuid references habitflow.users(id), target_user_id uuid references habitflow.users(id),
 action varchar(100) not null, resource_type varchar(80) not null, resource_id uuid, occurred_at timestamptz not null default now(), correlation_id varchar(100) not null,
 ip_hash varchar(128), user_agent_summary varchar(200), summary varchar(500) not null, before_data jsonb, after_data jsonb);
create index if not exists ix_audit_events_tenant_time on habitflow.audit_events(client_id,occurred_at desc);

create table if not exists habitflow.privacy_requests (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id), request_type varchar(30) not null check(request_type in ('Export','Deletion','Anonymization')),
 status varchar(30) not null default 'Requested' check(status in ('Requested','InProgress','Completed','Rejected','LegalHold')), legal_hold_reason varchar(300), requested_at timestamptz not null default now(), completed_at timestamptz);
create index if not exists ix_privacy_requests_owner on habitflow.privacy_requests(client_id,user_id,requested_at desc);
create table if not exists habitflow.consent_records (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id), purpose varchar(80) not null,
 document_version varchar(40) not null, granted boolean not null, recorded_at timestamptz not null default now(), source varchar(40) not null);
create index if not exists ix_consent_records_owner on habitflow.consent_records(client_id,user_id,recorded_at desc);
commit;
