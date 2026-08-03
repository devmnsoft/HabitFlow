-- v6.10.0. Additive/idempotent governance; never seeds an administrator credential.
BEGIN;
ALTER TABLE habitflow.users ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT false;
ALTER TABLE habitflow.plans ADD COLUMN IF NOT EXISTS is_sellable boolean NOT NULL DEFAULT false;
ALTER TABLE habitflow.plans ADD COLUMN IF NOT EXISTS sales_status varchar(24) NOT NULL DEFAULT 'Hidden';
ALTER TABLE habitflow.feature_catalog ADD COLUMN IF NOT EXISTS implementation_status varchar(24) NOT NULL DEFAULT 'Planned';
ALTER TABLE habitflow.feature_catalog ADD COLUMN IF NOT EXISTS is_marketable boolean NOT NULL DEFAULT false;

UPDATE habitflow.plans SET is_public=true,is_sellable=(code='ritmo'),sales_status='Available' WHERE code IN ('free','ritmo');
UPDATE habitflow.plans SET is_public=false,is_sellable=false,sales_status='Grandfathered' WHERE code='evolucao';
UPDATE habitflow.feature_catalog SET implementation_status='Implemented',is_marketable=true
 WHERE code IN ('active_habits_limit','active_goals_limit','full_habit_library','basic_reports','report_export_csv','report_print','full_history','history_days_limit','custom_categories');
UPDATE habitflow.feature_catalog SET implementation_status='Partial',is_marketable=false
 WHERE code IN ('reminders_per_habit','advanced_reports','shared_routines');
UPDATE habitflow.feature_catalog SET implementation_status='Planned',is_marketable=false
 WHERE code IN ('shared_goals','consolidated_reports','priority_support');
UPDATE habitflow.feature_catalog SET implementation_status='Internal',is_marketable=false
 WHERE code IN ('users_limit','user_invitations','client_admin_dashboard','internal_communications');

CREATE TABLE IF NOT EXISTS habitflow.legal_documents(
 id uuid PRIMARY KEY, document_type varchar(40) NOT NULL UNIQUE, created_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS habitflow.legal_document_versions(
 id uuid PRIMARY KEY, document_id uuid NOT NULL REFERENCES habitflow.legal_documents(id), version varchar(30) NOT NULL,
 locale varchar(12) NOT NULL DEFAULT 'pt-BR', title varchar(180) NOT NULL, summary text NOT NULL,
 sanitized_content text NOT NULL, content_hash varchar(64) NOT NULL, effective_at timestamptz NOT NULL,
 published_at timestamptz, requires_reacceptance boolean NOT NULL DEFAULT false, status varchar(20) NOT NULL DEFAULT 'Draft',
 created_by_user_id uuid REFERENCES habitflow.users(id), created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
 UNIQUE(document_id,version,locale), CHECK(status IN ('Draft','Published','Superseded','Archived')));
CREATE TABLE IF NOT EXISTS habitflow.user_legal_acceptances(
 id uuid PRIMARY KEY, client_id uuid REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id),
 document_type varchar(40) NOT NULL, version varchar(30) NOT NULL, content_hash varchar(64) NOT NULL,
 accepted_at timestamptz NOT NULL DEFAULT now(), source varchar(30) NOT NULL, correlation_id varchar(80) NOT NULL,
 ip_hmac varchar(64), user_agent_hmac varchar(64), revoked_at timestamptz, UNIQUE(user_id,document_type,version));
CREATE TABLE IF NOT EXISTS habitflow.user_consents(
 id uuid PRIMARY KEY, client_id uuid REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id),
 purpose varchar(50) NOT NULL, granted boolean NOT NULL DEFAULT false, recorded_at timestamptz NOT NULL DEFAULT now(),
 revoked_at timestamptz, correlation_id varchar(80) NOT NULL, UNIQUE(user_id,purpose));
CREATE TABLE IF NOT EXISTS habitflow.plan_public_benefits(
 id uuid PRIMARY KEY, plan_code varchar(40) NOT NULL, feature_code varchar(80) NOT NULL REFERENCES habitflow.feature_catalog(code),
 title varchar(120) NOT NULL, description text NOT NULL, icon_code varchar(80) NOT NULL, sort_order integer NOT NULL DEFAULT 0,
 comparison_group varchar(80) NOT NULL, is_highlighted boolean NOT NULL DEFAULT false, UNIQUE(plan_code,feature_code));
CREATE INDEX IF NOT EXISTS ix_legal_versions_current ON habitflow.legal_document_versions(document_id,locale,effective_at DESC) WHERE status='Published';
COMMIT;
