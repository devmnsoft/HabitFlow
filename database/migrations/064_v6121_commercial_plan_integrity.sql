-- v6.12.1: a public plan must not offer values for non-marketable capabilities.
BEGIN;
UPDATE habitflow.plan_features pf
SET bool_value = CASE WHEN f.value_type = 'Boolean' THEN false ELSE pf.bool_value END,
    int_value = CASE WHEN f.value_type = 'Integer' THEN null ELSE pf.int_value END,
    string_value = CASE WHEN f.value_type = 'String' THEN null ELSE pf.string_value END,
    updated_at = now()
FROM habitflow.feature_catalog f, habitflow.plans p
WHERE f.code = pf.feature_code
  AND p.id = pf.plan_id
  AND p.code IN ('free', 'ritmo')
  AND (f.implementation_status <> 'Implemented' OR NOT f.is_marketable);

UPDATE habitflow.plans
SET is_public = false, is_sellable = false, sales_status = 'Grandfathered'
WHERE code = 'evolucao';
COMMIT;
