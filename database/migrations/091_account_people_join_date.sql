-- 091: Preserve the actual tenant-join date separately from the user's signup date.
BEGIN;

ALTER TABLE habitflow.users
    ADD COLUMN IF NOT EXISTS client_joined_at timestamp NULL;

UPDATE habitflow.users
SET client_joined_at = created_at
WHERE client_id IS NOT NULL AND client_joined_at IS NULL;

CREATE INDEX IF NOT EXISTS ix_habitflow_users_client_joined_at
    ON habitflow.users(client_id, client_joined_at DESC);

COMMIT;
