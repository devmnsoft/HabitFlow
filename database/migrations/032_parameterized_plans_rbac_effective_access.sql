-- v6.2: catálogo de produtos separado do ciclo de cobrança.
set search_path to habitflow, public;

alter table habitflow.plans add column if not exists public_name varchar(120);
alter table habitflow.plans add column if not exists headline varchar(200);
alter table habitflow.plans add column if not exists audience_text text;
alter table habitflow.plans add column if not exists badge_text varchar(100);
alter table habitflow.plans add column if not exists is_featured boolean not null default false;
alter table habitflow.plans add column if not exists sort_order integer not null default 0;
alter table habitflow.plans add column if not exists created_by_user_id uuid null;
alter table habitflow.plans add column if not exists updated_by_user_id uuid null;
update habitflow.plans set public_name=coalesce(public_name,name);
alter table habitflow.plans alter column public_name set not null;

insert into habitflow.plans(id,code,name,public_name,headline,description,is_active,is_public,is_featured,sort_order,created_at,updated_at)
values
 ('10000000-0000-0000-0000-000000000001','free','Gratuito','Gratuito','Comece com leveza.','O essencial para cuidar da sua rotina.',true,true,false,10,now(),now()),
 ('10000000-0000-0000-0000-000000000002','ritmo','Ritmo','Ritmo','Tudo o que você precisa para manter sua rotina em movimento.','Mais liberdade para criar constância.',true,true,true,20,now(),now()),
 ('10000000-0000-0000-0000-000000000003','evolucao','Evolução','Evolução','Para evoluir junto com sua família, grupo ou pequena equipe.','Uma jornada compartilhada, no ritmo de vocês.',true,true,false,30,now(),now())
on conflict(code) do update set public_name=excluded.public_name, headline=excluded.headline, sort_order=excluded.sort_order;

alter table habitflow.clients add column if not exists contracted_plan_code varchar(80);
alter table habitflow.clients add column if not exists effective_plan_code varchar(80);
alter table habitflow.clients add column if not exists access_restriction_reason text;
alter table habitflow.clients add column if not exists access_restricted_at timestamp null;
alter table habitflow.clients add column if not exists access_restored_at timestamp null;
update habitflow.clients set contracted_plan_code=case plan::text when 'Premium' then 'ritmo' when 'Enterprise' then 'evolucao' else 'free' end where contracted_plan_code is null;
update habitflow.clients set effective_plan_code=case when benefits_status::text in ('PremiumBlocked','EnterpriseBlocked','RestrictedByPayment') then 'free' else contracted_plan_code end where effective_plan_code is null;
alter table habitflow.clients alter column contracted_plan_code set default 'free';
alter table habitflow.clients alter column effective_plan_code set default 'free';

alter table habitflow.client_subscriptions add column if not exists plan_code varchar(80);
alter table habitflow.client_subscriptions add column if not exists billing_cycle varchar(40);
update habitflow.client_subscriptions set plan_code=case when lower(coalesce(plan_code,'')) in ('premium_monthly','premium_yearly','premium') then 'ritmo' when lower(coalesce(plan_code,''))='enterprise' then 'evolucao' else coalesce(nullif(lower(plan_code),''),'free') end;
update habitflow.client_subscriptions set billing_cycle=case when lower(coalesce(billing_cycle,''))='yearly' or lower(coalesce(plan_code,''))='premium_yearly' then 'Yearly' else 'Monthly' end where billing_cycle is null;
comment on column habitflow.plans.price_monthly is 'LEGADO: remover somente após validação da migração em produção.';
comment on column habitflow.plans.price_yearly is 'LEGADO: usar habitflow.plan_prices.';

