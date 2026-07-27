BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.sharing_consents (
 id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
 consent_type varchar(80) NOT NULL CHECK(consent_type IN ('AggregateProgress','IndividualProgress','SharedGoals','SharedRoutines')),
 granted boolean NOT NULL DEFAULT false, granted_at timestamp, revoked_at timestamp, updated_at timestamp NOT NULL DEFAULT now(),
 UNIQUE(user_id,consent_type)
);
CREATE INDEX IF NOT EXISTS ix_sharing_consents_scope ON habitflow.sharing_consents(client_id,user_id);
COMMIT;
