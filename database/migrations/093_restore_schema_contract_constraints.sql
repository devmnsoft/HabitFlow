-- 093: Restore named schema contracts missing from the ordered migration set.
BEGIN;

DO $$
DECLARE
    existing_constraint text;
BEGIN
    IF EXISTS (
        SELECT 1
        FROM habitflow.users
        WHERE role NOT IN ('User', 'Admin', 'SuperAdmin', 'ReadOnly', 'Manager', 'TenantAdmin', 'TenantOwner', 'BillingAdmin')
           OR account_status NOT IN ('Active', 'Blocked', 'Suspended', 'DeletedPending')
           OR risk_status NOT IN ('Normal', 'Watchlist', 'Suspicious')
           OR plan NOT IN ('Free', 'Premium')
           OR plan_status NOT IN ('Active', 'Trial', 'Canceled', 'Inactive', 'PastDue')
    ) THEN
        RAISE EXCEPTION 'Migration 093 blocked: existing users contain values outside the supported role, account, risk, plan, or plan-status contracts.';
    END IF;

    IF EXISTS (
        SELECT 1 FROM habitflow.support_tickets
        WHERE status NOT IN ('Open', 'InProgress', 'Resolved', 'Closed')
    ) THEN
        RAISE EXCEPTION 'Migration 093 blocked: existing support tickets contain an unsupported status.';
    END IF;

    IF EXISTS (
        SELECT 1 FROM habitflow.lgpd_requests
        WHERE type NOT IN ('Export', 'Delete', 'Anonymize')
           OR status NOT IN ('Requested', 'InReview', 'Processing', 'Completed', 'Rejected', 'Canceled')
    ) THEN
        RAISE EXCEPTION 'Migration 093 blocked: existing LGPD requests contain an unsupported type or status.';
    END IF;

    IF EXISTS (
        SELECT 1 FROM habitflow.billing_events
        WHERE plan IS NOT NULL AND plan NOT IN ('Free', 'Premium')
    ) THEN
        RAISE EXCEPTION 'Migration 093 blocked: existing billing events contain an unsupported plan.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM habitflow.habit_completions
        GROUP BY habit_id, completed_date
        HAVING count(*) > 1
    ) THEN
        RAISE EXCEPTION 'Migration 093 blocked: duplicate habit completions must be resolved before enforcing uniqueness.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.users'::regclass AND conname = 'ck_habitflow_users_role'
    ) THEN
        ALTER TABLE habitflow.users ADD CONSTRAINT ck_habitflow_users_role
            CHECK (role IN ('User', 'Admin', 'SuperAdmin', 'ReadOnly', 'Manager', 'TenantAdmin', 'TenantOwner', 'BillingAdmin'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.users'::regclass AND conname = 'ck_habitflow_users_account_status'
    ) THEN
        ALTER TABLE habitflow.users ADD CONSTRAINT ck_habitflow_users_account_status
            CHECK (account_status IN ('Active', 'Blocked', 'Suspended', 'DeletedPending'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.users'::regclass AND conname = 'ck_habitflow_users_risk_status'
    ) THEN
        ALTER TABLE habitflow.users ADD CONSTRAINT ck_habitflow_users_risk_status
            CHECK (risk_status IN ('Normal', 'Watchlist', 'Suspicious'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.users'::regclass AND conname = 'ck_habitflow_users_plan'
    ) THEN
        ALTER TABLE habitflow.users ADD CONSTRAINT ck_habitflow_users_plan
            CHECK (plan IN ('Free', 'Premium'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.users'::regclass AND conname = 'ck_habitflow_users_plan_status'
    ) THEN
        ALTER TABLE habitflow.users ADD CONSTRAINT ck_habitflow_users_plan_status
            CHECK (plan_status IN ('Active', 'Trial', 'Canceled', 'Inactive', 'PastDue'));
    END IF;

    IF to_regclass('habitflow.habit_completions') IS NULL THEN
        RAISE EXCEPTION 'Migration 093 blocked: habitflow.habit_completions is missing.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conrelid = 'habitflow.habit_completions'::regclass
          AND conname = 'uq_habitflow_habit_completions_habit_date'
    ) THEN
        SELECT conname
        INTO existing_constraint
        FROM pg_constraint
        WHERE conrelid = 'habitflow.habit_completions'::regclass
          AND contype = 'u'
          AND pg_get_constraintdef(oid) = 'UNIQUE (habit_id, completed_date)'
        ORDER BY conname
        LIMIT 1;

        IF existing_constraint IS NOT NULL THEN
            EXECUTE format(
                'ALTER TABLE habitflow.habit_completions RENAME CONSTRAINT %I TO %I',
                existing_constraint,
                'uq_habitflow_habit_completions_habit_date'
            );
        ELSE
            ALTER TABLE habitflow.habit_completions
                ADD CONSTRAINT uq_habitflow_habit_completions_habit_date UNIQUE (habit_id, completed_date);
        END IF;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.support_tickets'::regclass AND conname = 'ck_habitflow_support_tickets_status'
    ) THEN
        ALTER TABLE habitflow.support_tickets ADD CONSTRAINT ck_habitflow_support_tickets_status
            CHECK (status IN ('Open', 'InProgress', 'Resolved', 'Closed'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.lgpd_requests'::regclass AND conname = 'ck_habitflow_lgpd_requests_type'
    ) THEN
        ALTER TABLE habitflow.lgpd_requests ADD CONSTRAINT ck_habitflow_lgpd_requests_type
            CHECK (type IN ('Export', 'Delete', 'Anonymize'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.lgpd_requests'::regclass AND conname = 'ck_habitflow_lgpd_requests_status'
    ) THEN
        ALTER TABLE habitflow.lgpd_requests ADD CONSTRAINT ck_habitflow_lgpd_requests_status
            CHECK (status IN ('Requested', 'InReview', 'Processing', 'Completed', 'Rejected', 'Canceled'));
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conrelid = 'habitflow.billing_events'::regclass AND conname = 'ck_habitflow_billing_events_plan'
    ) THEN
        ALTER TABLE habitflow.billing_events ADD CONSTRAINT ck_habitflow_billing_events_plan
            CHECK (plan IS NULL OR plan IN ('Free', 'Premium'));
    END IF;
END $$;

COMMIT;
