-- HabitFlow v6.20.0
-- Migration 096: Homologação SaaS de Produção - Catálogo Comercial, Trial de 15 Dias e Desativação do Free Público
BEGIN;

-- 1. Remove o plano 'free' da oferta pública em /plans (permanece apenas como fallback legado/interno)
UPDATE habitflow.plans
SET is_public = false,
    updated_at = now()
WHERE code = 'free';

-- 2. Garante que os planos comerciais sejam públicos
UPDATE habitflow.plans
SET is_public = true,
    is_active = true,
    updated_at = now()
WHERE code IN ('premium_monthly', 'premium_yearly', 'ritmo', 'team', 'enterprise');

-- 3. Adiciona índice para consulta rápida de catálogo público ativo
CREATE INDEX IF NOT EXISTS ix_plans_public_active
ON habitflow.plans(is_active, is_public, sort_order);

-- 4. Garante coluna de auditoria para ajustes manuais de billing caso necessário
ALTER TABLE habitflow.clients
ADD COLUMN IF NOT EXISTS last_commercial_adjustment_at timestamp without time zone,
ADD COLUMN IF NOT EXISTS last_commercial_adjustment_reason text;

COMMIT;
