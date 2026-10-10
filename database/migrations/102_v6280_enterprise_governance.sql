-- HabitFlow v6.28.0: Enterprise SaaS governance foundations.
-- External SSO, DNS verification, payment and AI providers remain disabled until configured with real secrets outside Git.

create table if not exists habitflow.tenant_sso_configurations (
    client_id uuid primary key references habitflow.clients(id) on delete cascade,
    enabled boolean not null default false,
    provider text not null default '',
    authority text not null default '',
    oidc_client_id text not null default '',
    callback_path text not null default '',
    require_signed_tokens boolean not null default true,
    allow_local_login_fallback boolean not null default false,
    tenant_claim text null,
    last_sanitized_error text null,
    updated_at timestamptz not null default now(),
    constraint ck_tenant_sso_provider check (provider in ('','OpenIdConnect','OAuth2','Saml')),
    constraint ck_tenant_sso_no_secret check (last_sanitized_error is null or last_sanitized_error !~* '(client_secret|password|token)[[:space:]]*=')
);

create table if not exists habitflow.tenant_mfa_policies (
    client_id uuid primary key references habitflow.clients(id) on delete cascade,
    require_for_owners boolean not null default true,
    require_for_admins boolean not null default false,
    require_for_sensitive_roles boolean not null default false,
    updated_at timestamptz not null default now()
);

create table if not exists habitflow.tenant_branding (
    client_id uuid primary key references habitflow.clients(id) on delete cascade,
    commercial_name text not null,
    logo_path text null,
    favicon_path text null,
    primary_color text not null default '#2563eb',
    secondary_color text not null default '#111827',
    support_url text null,
    welcome_text text null,
    updated_at timestamptz not null default now(),
    constraint ck_tenant_branding_primary_hex check (primary_color ~ '^#[0-9A-Fa-f]{6}$'),
    constraint ck_tenant_branding_secondary_hex check (secondary_color ~ '^#[0-9A-Fa-f]{6}$'),
    constraint ck_tenant_branding_safe_logo check (logo_path is null or (logo_path like '/uploads/tenant-branding/%' and logo_path not like '%..%'))
);

create table if not exists habitflow.tenant_domains (
    id uuid primary key default gen_random_uuid(),
    client_id uuid not null references habitflow.clients(id) on delete cascade,
    host_name text not null,
    status text not null default 'NotConfigured',
    is_primary boolean not null default false,
    verification_token_hash text null,
    last_error text null,
    updated_at timestamptz not null default now(),
    constraint uq_tenant_domains_host unique (host_name),
    constraint ck_tenant_domains_status check (status in ('NotConfigured','WaitingDns','Verified','Active','Error','Suspended')),
    constraint ck_tenant_domains_no_activation_without_hash check (status <> 'Active' or verification_token_hash is not null)
);

create unique index if not exists ux_tenant_domains_primary
on habitflow.tenant_domains(client_id)
where is_primary;

create table if not exists habitflow.enterprise_ai_policies (
    client_id uuid primary key references habitflow.clients(id) on delete cascade,
    enabled boolean not null default false,
    provider text not null default '',
    model text not null default '',
    allow_sensitive_data boolean not null default false,
    daily_user_limit integer not null default 0,
    updated_at timestamptz not null default now(),
    constraint ck_enterprise_ai_daily_limit check (daily_user_limit >= 0)
);

insert into habitflow.feature_catalog(code, name, description, value_type, category, is_public, implementation_status, is_marketable, is_active)
values
    ('enterprise_sso','SSO corporativo','Base segura para SSO corporativo por tenant.','Boolean','Enterprise',true,'Implemented',true,false),
    ('mandatory_mfa','MFA obrigatorio','Politicas de MFA para perfis sensiveis.','Boolean','Enterprise',true,'Implemented',true,false),
    ('white_label','White label','Identidade visual por tenant com validacao de contraste.','Boolean','Enterprise',true,'Implemented',true,false),
    ('custom_domain','Dominio personalizado','Dominio/subdominio com verificacao DNS antes da ativacao.','Boolean','Enterprise',true,'Implemented',true,false),
    ('api_keys','API keys','Emissao segura de chaves de API.','Boolean','Enterprise',true,'Planned',false,false),
    ('advanced_audit','Auditoria avancada','Trilhas de auditoria avancadas por tenant.','Boolean','Enterprise',true,'Partial',false,false),
    ('sla','SLA contratado','SLA comercial aplicado aos fluxos de suporte.','Boolean','Enterprise',true,'Implemented',true,false),
    ('ai_governance','Governanca de IA','Politicas de IA por tenant, provider e modelo permitido.','Boolean','Enterprise',true,'Implemented',true,false)
on conflict (code) do update set
    name = excluded.name,
    description = excluded.description,
    implementation_status = excluded.implementation_status,
    is_marketable = excluded.is_marketable,
    is_active = true;

insert into habitflow.plan_features(plan_id, feature_code, bool_value, int_value, string_value)
select p.id, f.code,
       case when p.code = 'enterprise' and f.code <> 'api_keys' then true else false end,
       null,
       null
from habitflow.plans p
join habitflow.feature_catalog f on f.code in ('enterprise_sso','mandatory_mfa','white_label','custom_domain','api_keys','advanced_audit','sla','ai_governance')
on conflict (plan_id, feature_code) do update set
    bool_value = excluded.bool_value,
    int_value = null,
    string_value = null;

