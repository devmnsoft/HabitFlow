-- Transaction mode: required. DDL and legacy normalization are atomic.
BEGIN;

ALTER TABLE habitflow.billing_communication_rules
  ALTER COLUMN id SET DEFAULT gen_random_uuid();

-- Rename a legacy code only when its canonical replacement is absent. This keeps
-- the original row id and created_at.
UPDATE habitflow.billing_communication_rules legacy
SET code = CASE legacy.code
    WHEN 'overdue_plus_2' THEN 'due_plus_2'
    WHEN 'overdue_plus_5' THEN 'due_plus_5'
  END,
  trigger_type = 'AfterDueDate',
  updated_at = now()
WHERE legacy.code IN ('overdue_plus_2', 'overdue_plus_5')
  AND NOT EXISTS (
    SELECT 1 FROM habitflow.billing_communication_rules canonical
    WHERE canonical.code = CASE legacy.code
      WHEN 'overdue_plus_2' THEN 'due_plus_2'
      WHEN 'overdue_plus_5' THEN 'due_plus_5'
    END
  );

-- If both forms already exist, retain the row for audit/history but ensure that
-- only the canonical rule can be dispatched.
UPDATE habitflow.billing_communication_rules
SET is_active = false, updated_at = now()
WHERE code IN ('overdue_plus_2', 'overdue_plus_5');

COMMIT;
