-- 090: Keep pending invite reservations unique per tenant and email.
-- Resolve expired rows first; stop visibly if pre-existing live duplicates need review.
BEGIN;

UPDATE habitflow.user_invites
SET status = 'Expired', updated_at = now()
WHERE status = 'Pending' AND expires_at <= now();

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM habitflow.user_invites
        WHERE status = 'Pending'
        GROUP BY client_id, lower(trim(email))
        HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION 'Migration 090 blocked: duplicate pending invites exist for the same tenant and email; reconcile them before retrying.';
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_habitflow_user_invites_pending_client_email
    ON habitflow.user_invites(client_id, lower(trim(email)))
    WHERE status = 'Pending';

CREATE INDEX IF NOT EXISTS ix_habitflow_user_invites_client_status_expiry
    ON habitflow.user_invites(client_id, status, expires_at);

COMMIT;
