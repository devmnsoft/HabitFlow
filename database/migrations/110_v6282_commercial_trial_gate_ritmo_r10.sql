-- HabitFlow v6.28.2: regras comerciais — Free = trial de 15 dias com bloqueio total após expirar,
-- Ritmo R$ 10/mês com até 5 hábitos ativos, plano interno 'blocked' para clientes sem assinatura válida.
-- Somente aditivo/idempotente; nunca remove dados nem promete recurso não implementado.
BEGIN;
SET LOCAL search_path TO habitflow, public;

-- 1) Fim do trial por cliente: gate central de acesso (o trial do cliente, não do usuário).
ALTER TABLE habitflow.clients ADD COLUMN IF NOT EXISTS trial_ends_at timestamp null;
CREATE INDEX IF NOT EXISTS ix_clients_trial_ends_at ON habitflow.clients(trial_ends_at) WHERE trial_ends_at is not null;

-- Backfill: clientes em Trial herdam created_at + 15 dias (mesma regra do cadastro público).
UPDATE habitflow.clients
SET trial_ends_at = created_at + interval '15 days'
WHERE subscription_status = 'Trial' AND trial_ends_at IS NULL;

-- 2) Ritmo a R$ 10,00/mês e R$ 100,00/ano (12 meses pelo preço de ~10; economia real de 2 meses).
UPDATE habitflow.plan_prices pp
SET amount = 10.00
FROM habitflow.plans p
WHERE pp.plan_id = p.id AND p.code = 'ritmo' AND pp.billing_cycle = 'Monthly'
  AND pp.amount <> 10.00;

UPDATE habitflow.plan_prices pp
SET amount = 100.00
FROM habitflow.plans p
WHERE pp.plan_id = p.id AND p.code = 'ritmo' AND pp.billing_cycle = 'Yearly'
  AND pp.amount <> 100.00;

UPDATE habitflow.plans
SET price_monthly = 10.00, price_yearly = 100.00, updated_at = now()
WHERE code = 'ritmo' AND (price_monthly IS DISTINCT FROM 10.00 OR price_yearly IS DISTINCT FROM 100.00);

-- 3) Ritmo limitado a 5 hábitos ativos (era ilimitado).
UPDATE habitflow.plan_features pf
SET int_value = 5, updated_at = now()
FROM habitflow.plans p, habitflow.feature_catalog f
WHERE pf.plan_id = p.id AND pf.feature_code = f.code
  AND p.code = 'ritmo' AND f.code = 'active_habits_limit' AND pf.int_value IS DISTINCT FROM 5;

-- 4) Plano interno 'blocked': nenhuma feature, limites zerados; histórico preservado para leitura
--    (history_days_limit 30) e check-ins de hábitos existentes permanecem possíveis.
INSERT INTO habitflow.plans(id,code,name,public_name,headline,description,audience_text,badge_text,is_active,is_public,is_sellable,sales_status,is_featured,sort_order,created_at,updated_at)
VALUES ('10000000-0000-0000-0000-00000000000b','blocked','Bloqueado','Acesso bloqueado','Assinatura necessária','Acesso bloqueado: o período grátis de 15 dias terminou ou o pagamento está pendente. Assine um plano para reativar sua conta.','Interno — nunca exibido no catálogo público.',null,true,false,false,'Hidden',false,990,now(),now())
ON CONFLICT(code) DO UPDATE SET is_active=true, is_public=false, is_sellable=false, sales_status='Hidden', updated_at=now();

INSERT INTO habitflow.plan_features(plan_id,feature_code,bool_value,int_value)
SELECT p.id,f.code,
 CASE WHEN f.value_type='Boolean' THEN false END,
 CASE WHEN f.value_type='Integer' THEN CASE WHEN f.code='history_days_limit' THEN 30 ELSE 0 END END
FROM habitflow.plans p CROSS JOIN habitflow.feature_catalog f
WHERE p.code='blocked'
ON CONFLICT(plan_id,feature_code) DO UPDATE SET bool_value=excluded.bool_value,int_value=excluded.int_value,updated_at=now();

-- 5) Catálogo público: Free passa a ser comunicado como trial de 15 dias que converge para o Ritmo R$ 10/mês.
UPDATE habitflow.plans
SET public_name='Grátis por 15 dias',
    headline='Teste completo por 15 dias, sem cartão.',
    description='Acesso completo a todos os recursos por 15 dias. Após o período, escolha o plano Ritmo por R$ 10,00/mês com até 5 hábitos ativos para continuar.',
    badge_text='15 dias grátis',
    is_public=true,
    is_active=true,
    updated_at = now()
WHERE code='free';

COMMIT;
