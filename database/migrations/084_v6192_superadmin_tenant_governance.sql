-- HabitFlow v6.19.2 - additive SaaS tenant governance. Safe to re-run.
begin;
create schema if not exists habitflow;

alter table if exists habitflow.users drop constraint if exists ck_habitflow_users_role;
alter table if exists habitflow.users add constraint ck_habitflow_users_role check(role in
 ('User','Admin','SuperAdmin','ReadOnly','Manager','TenantAdmin','TenantOwner','BillingAdmin'));
alter table if exists habitflow.user_invites drop constraint if exists ck_habitflow_user_invites_role;
alter table if exists habitflow.user_invites add constraint ck_habitflow_user_invites_role check(role in
 ('User','Admin','ReadOnly','Manager','TenantAdmin','TenantOwner','BillingAdmin'));

create table if not exists habitflow.tenant_modules (
 tenant_id uuid not null references habitflow.clients(id), module_code varchar(40) not null,
 enabled boolean not null default true, blocked_reason varchar(500), updated_by uuid references habitflow.users(id),
 created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 primary key (tenant_id,module_code), constraint ck_tenant_module_code check(module_code in
 ('habits','goals','routines','calendar','notifications','analytics','gamification','assistant','teams','integrations','billing','support')),
 constraint ck_tenant_module_reason check(enabled or nullif(btrim(blocked_reason),'') is not null));

create table if not exists habitflow.tenant_manual_charges (
 id uuid primary key, tenant_id uuid not null references habitflow.clients(id), amount numeric(12,2) not null,
 due_date date not null, description varchar(240) not null, reason varchar(500) not null,
 status varchar(20) not null default 'Pending', approved_at timestamptz, created_by uuid not null references habitflow.users(id),
 approved_by uuid references habitflow.users(id), created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 constraint ck_manual_charge_amount check(amount > 0), constraint ck_manual_charge_reason check(nullif(btrim(reason),'') is not null),
 constraint ck_manual_charge_status check(status in ('Pending','Approved','Canceled','Overdue')),
 constraint ck_manual_charge_approval check((status='Approved')=(approved_at is not null and approved_by is not null)));

create table if not exists habitflow.tenant_access_audit (
 id uuid primary key, tenant_id uuid references habitflow.clients(id), user_id uuid references habitflow.users(id),
 actor_user_id uuid not null references habitflow.users(id), event_code varchar(80) not null, reason varchar(500),
 correlation_id varchar(100), metadata_json jsonb not null default '{}'::jsonb, created_at timestamptz not null default now(),
 constraint ck_tenant_audit_event check(event_code in ('tenant.created','tenant.updated','tenant.blocked','tenant.unblocked',
 'tenant.module_enabled','tenant.module_disabled','tenant.user_created','tenant.user_blocked','tenant.role_changed',
 'billing.manual_charge_created','billing.manual_payment_approved','superadmin.tenant_accessed','login.document_attempted')));

create table if not exists habitflow.user_documents (
 id uuid primary key, user_id uuid not null references habitflow.users(id), tenant_id uuid not null references habitflow.clients(id),
 document_type varchar(4) not null, document_normalized varchar(14) not null, enabled_for_login boolean not null default false,
 created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 constraint ck_user_document_type check((document_type='CPF' and length(document_normalized)=11) or (document_type='CNPJ' and length(document_normalized)=14)),
 constraint ck_user_document_digits check(document_normalized ~ '^[0-9]+$'), unique(user_id,tenant_id,document_type));

create index if not exists ix_tenant_modules_status on habitflow.tenant_modules(tenant_id,enabled,module_code);
create index if not exists ix_manual_charges_tenant_status on habitflow.tenant_manual_charges(tenant_id,status,due_date desc);
create index if not exists ix_tenant_access_audit_tenant_event on habitflow.tenant_access_audit(tenant_id,event_code,created_at desc);
create index if not exists ix_tenant_access_audit_actor on habitflow.tenant_access_audit(actor_user_id,created_at desc);
create unique index if not exists ux_user_documents_login on habitflow.user_documents(document_normalized,tenant_id) where enabled_for_login;
commit;
