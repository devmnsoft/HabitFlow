-- HabitFlow v6.19.6: SaaS vendável — catálogo Team/Enterprise, estados comerciais completos, tenant MNSOFT.
-- Somente aditivo/idempotente; nunca remete valores já existentes.
BEGIN;
SET LOCAL search_path TO habitflow, public;

-- 1) Estados de assinatura: amplia o conjunto aceito (mantém todos os valores antigos).
ALTER TABLE habitflow.subscriptions DROP CONSTRAINT IF EXISTS ck_habitflow_subscriptions_status;
ALTER TABLE habitflow.subscriptions ADD CONSTRAINT ck_habitflow_subscriptions_status
 CHECK(status in ('Pending','PaymentPending','Active','Trial','Trialing','PastDue','Canceled','Expired','ManualReview','Suspended','Failed','Inactive'));

-- 2) Estados comerciais do cliente: adiciona PaymentPending/ManualReview.
ALTER TABLE habitflow.clients DROP CONSTRAINT IF EXISTS ck_habitflow_clients_subscription_status;
ALTER TABLE habitflow.clients ADD CONSTRAINT ck_habitflow_clients_subscription_status
 CHECK(subscription_status in ('Free','Trial','Active','PaymentPending','PastDue','Canceled','Suspended','ManualReview'));
CREATE INDEX IF NOT EXISTS ix_clients_subscription_status ON habitflow.clients(subscription_status);

-- 3) Novas features (controladas pelo backend; não divulgadas como benefício público sem implementação).
INSERT INTO habitflow.feature_catalog(code,name,description,value_type,category,is_active,implementation_status,is_marketable,is_public)
VALUES
 ('teams','Gestão de times','Pessoas, papéis e convites da conta.','Boolean','Conta',true,'Implemented',false,false),
 ('corporate_features','Recursos corporativos','SSO, integrações e contrato corporativo sob negociação.','Boolean','Corporativo',true,'Planned',false,false)
ON CONFLICT(code) DO UPDATE SET
 name=EXCLUDED.name,
 description=EXCLUDED.description,
 value_type=EXCLUDED.value_type,
 category=EXCLUDED.category,
 is_active=true,
 implementation_status=EXCLUDED.implementation_status,
 is_marketable=EXCLUDED.is_marketable,
 is_public=EXCLUDED.is_public;

-- 4) Planos Team e Enterprise (códigos estáveis). Enterprise é venda por contato comercial.
INSERT INTO habitflow.plans(id,code,name,public_name,headline,description,audience_text,badge_text,is_active,is_public,is_sellable,sales_status,is_featured,sort_order,created_at,updated_at)
VALUES
 ('10000000-0000-0000-0000-000000000004','team','Team','Team','Para equipes que organizam rotinas em conjunto.','Hábitos, objetivos, desafios e assistente IA para o time inteiro, com gestão de pessoas.','Equipes pequenas e médias.','Equipe',true,true,true,'Available',false,40,now(),now()),
 ('10000000-0000-0000-0000-000000000005','enterprise','Enterprise','Enterprise','Atendimento corporativo dedicado.','Plataforma completa para organizações, com recursos corporativos e suporte prioritário.','Organizações e contas corporativas.','Sob contrato',true,true,true,'Contact',false,50,now(),now())
ON CONFLICT(code) DO UPDATE SET
 name=EXCLUDED.name,
 public_name=EXCLUDED.public_name,
 headline=EXCLUDED.headline,
 description=EXCLUDED.description,
 audience_text=EXCLUDED.audience_text,
 badge_text=EXCLUDED.badge_text,
 is_active=true,
 is_public=true,
 is_sellable=true,
 sales_status=EXCLUDED.sales_status,
 sort_order=EXCLUDED.sort_order,
 updated_at=now();

-- 5) Preços reais do Team (anual com economia real: 12 x R$ 79,90 = R$ 958,80 -> R$ 799,00, ~17% off).
INSERT INTO habitflow.plan_prices(id,plan_id,billing_cycle,amount,currency,valid_from)
SELECT v.id,p.id,v.cycle,v.amount,'BRL',timestamp '2026-01-01' FROM (VALUES
 ('20000000-0000-0000-0000-000000000007'::uuid,'team','Monthly',79.90),
 ('20000000-0000-0000-0000-000000000008'::uuid,'team','Yearly',799.00)) v(id,code,cycle,amount)
JOIN habitflow.plans p ON p.code=v.code
ON CONFLICT DO NOTHING;

-- 6) Recursos por plano (apenas o que o backend realmente aplica; sem promessa sem regra).
-- Somente features com implementation_status 'Implemented' entram como promessa publica;
-- Partial/Planned (advanced_reports, shared_routines, shared_goals, reminders_per_habit,
-- consolidated_reports, priority_support) entram aqui quando forem implementadas.
INSERT INTO habitflow.plan_features(plan_id,feature_code,bool_value,int_value)
SELECT p.id,f.code,
 CASE WHEN f.value_type='Boolean' THEN f.code IN
  ('full_habit_library','custom_categories','basic_reports','report_export_csv','report_print','full_history',
   'weekly_goals','achievements','advanced_achievements','streak_freeze','missions','progress_dashboard',
   'challenge_7_days','challenge_30_days','challenge_90_days','ai_assistant','teams','user_invitations','client_admin_dashboard')
 END,
 CASE WHEN f.value_type='Integer' THEN CASE f.code
   WHEN 'users_limit' THEN CASE WHEN p.code='team' THEN 10 ELSE -1 END
   WHEN 'active_habits_limit' THEN -1
   WHEN 'active_goals_limit' THEN -1
   WHEN 'history_days_limit' THEN -1 END END
FROM habitflow.plans p CROSS JOIN habitflow.feature_catalog f
WHERE p.code IN ('team','enterprise')
ON CONFLICT(plan_id,feature_code) DO UPDATE SET bool_value=EXCLUDED.bool_value,int_value=EXCLUDED.int_value,updated_at=now();

-- 7) Tenant MNSOFT (plataforma) passa a operar no plano Enterprise.
UPDATE habitflow.clients c
SET contracted_plan_code='enterprise', effective_plan_code='enterprise', access_restriction_reason=NULL, updated_at=now()
WHERE c.document_normalized='18160057000113' AND coalesce(c.contracted_plan_code,'free') IN ('free');

-- 8) Sinalização por tenant/módulo (alimenta os claims tenant_status/tenant_module do cookie de sessão).
CREATE TABLE IF NOT EXISTS habitflow.tenant_feature_flags(
 client_id uuid not null references habitflow.clients(id) on delete cascade,
 module varchar(40) not null,
 enabled boolean not null default true,
 updated_by uuid null references habitflow.users(id) on delete set null,
 updated_at timestamptz not null default now(),
 primary key(client_id,module));

-- 9) Histórico de mudanças de status comercial do tenant (auditoria imutável).
CREATE TABLE IF NOT EXISTS habitflow.tenant_commercial_status(
 id uuid primary key default gen_random_uuid(),
 client_id uuid not null references habitflow.clients(id) on delete cascade,
 previous_status varchar(32) not null,
 new_status varchar(32) not null,
 reason varchar(240) null,
 changed_by uuid null references habitflow.users(id) on delete set null,
 changed_at timestamptz not null default now());
CREATE INDEX IF NOT EXISTS ix_tenant_commercial_status_client_date ON habitflow.tenant_commercial_status(client_id, changed_at desc);

COMMIT;
