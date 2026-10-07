-- 092: Make the existing invitation entitlement enforceable and expose it to backend feature checks.
BEGIN;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM (VALUES ('user_invitations'), ('users_limit')) req(code)
        WHERE NOT EXISTS (SELECT 1 FROM habitflow.feature_catalog f WHERE f.code = req.code)
    ) THEN
        RAISE EXCEPTION 'Migration 092 blocked: invitation or seat-limit feature seed is missing; restore the feature seeds before retrying.';
    END IF;
END $$;

UPDATE habitflow.feature_catalog
SET implementation_status = 'Implemented',
    is_marketable = true,
    is_public = false
WHERE code IN ('user_invitations', 'users_limit');

INSERT INTO habitflow.plan_features(plan_id, feature_code, int_value)
SELECT p.id, 'users_limit', 1
FROM habitflow.plans p
WHERE p.code IN ('free', 'ritmo')
ON CONFLICT(plan_id, feature_code) DO UPDATE
SET int_value = EXCLUDED.int_value,
    updated_at = now();

COMMIT;
