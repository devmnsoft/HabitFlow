-- v6.10.2 - Server-authorized account sessions and revocation.
BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.user_sessions (
    id uuid PRIMARY KEY,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    client_id uuid REFERENCES habitflow.clients(id),
    user_agent varchar(500) NOT NULL,
    ip_address varchar(64) NOT NULL,
    created_at timestamptz NOT NULL,
    last_activity_at timestamptz NOT NULL,
    expires_at timestamptz NOT NULL,
    revoked_at timestamptz,
    revocation_reason varchar(80)
);
CREATE INDEX IF NOT EXISTS ix_user_sessions_owner_active ON habitflow.user_sessions(user_id,client_id,last_activity_at DESC) WHERE revoked_at IS NULL;
CREATE INDEX IF NOT EXISTS ix_user_sessions_expiration ON habitflow.user_sessions(expires_at) WHERE revoked_at IS NULL;
COMMIT;
