-- HabitFlow v6.19.6: multi-provider AI assistant + public feature flags.
BEGIN;
SET LOCAL search_path TO habitflow, public;

ALTER TABLE habitflow.feature_catalog
  ADD COLUMN IF NOT EXISTS is_public boolean NOT NULL DEFAULT true;

-- Internal capabilities exist but are not advertised as public benefits.
UPDATE habitflow.feature_catalog
SET is_public = false
WHERE implementation_status = 'Internal' AND is_public;

INSERT INTO habitflow.feature_catalog(code,name,description,value_type,category,is_active,implementation_status,is_marketable,is_public)
VALUES
 ('ai_assistant','Assistente IA','Chat orientado ao HabitFlow com respostas seguras e isolamento multi-tenant.','Boolean','IA',true,'Implemented',true,true)
ON CONFLICT(code) DO UPDATE SET
 name=EXCLUDED.name,
 description=EXCLUDED.description,
 value_type=EXCLUDED.value_type,
 category=EXCLUDED.category,
 is_active=true,
 implementation_status='Implemented',
 is_marketable=true,
 is_public=true;

INSERT INTO habitflow.plan_features(plan_id,feature_code,bool_value)
SELECT p.id,'ai_assistant',(p.code<>'free')
FROM habitflow.plans p
WHERE p.code IN ('free','ritmo','evolucao')
ON CONFLICT(plan_id,feature_code) DO UPDATE SET
 bool_value=EXCLUDED.bool_value,
 updated_at=now();

COMMIT;
