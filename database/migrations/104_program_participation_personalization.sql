-- HabitFlow v6.28.1: program participation creates only user-confirmed habits and preserves history.
BEGIN;

SET LOCAL search_path TO habitflow, public;

ALTER TABLE habitflow.habit_journeys ADD COLUMN IF NOT EXISTS status varchar(20) NOT NULL DEFAULT 'Published';
ALTER TABLE habitflow.habit_journeys ADD COLUMN IF NOT EXISTS version integer NOT NULL DEFAULT 1;
ALTER TABLE habitflow.habit_journeys ADD COLUMN IF NOT EXISTS owner_user_id uuid NULL REFERENCES habitflow.users(id);
ALTER TABLE habitflow.habit_journeys ADD COLUMN IF NOT EXISTS change_summary text NULL;
ALTER TABLE habitflow.habit_journeys ADD COLUMN IF NOT EXISTS is_private boolean NOT NULL DEFAULT false;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_habit_journeys_status_v6281'
          AND conrelid = 'habitflow.habit_journeys'::regclass
    ) THEN
        ALTER TABLE habitflow.habit_journeys
            ADD CONSTRAINT ck_habit_journeys_status_v6281
            CHECK (status IN ('Draft','Published','Paused','Ended','Archived'));
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS habitflow.habit_journey_member_habits (
    member_id uuid NOT NULL REFERENCES habitflow.habit_journey_members(id) ON DELETE CASCADE,
    journey_id uuid NOT NULL REFERENCES habitflow.habit_journeys(id) ON DELETE CASCADE,
    step_id uuid NOT NULL REFERENCES habitflow.habit_journey_steps(id) ON DELETE RESTRICT,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    habit_id uuid NOT NULL REFERENCES habitflow.habits(id) ON DELETE RESTRICT,
    habit_name varchar(150) NOT NULL,
    status varchar(20) NOT NULL DEFAULT 'Active',
    created_at timestamptz NOT NULL DEFAULT now(),
    ended_at timestamptz NULL,
    PRIMARY KEY (member_id, habit_id),
    CONSTRAINT ux_habit_journey_member_step UNIQUE (journey_id, user_id, step_id),
    CONSTRAINT ck_habit_journey_member_habits_status CHECK (status IN ('Active','Ended','Removed'))
);

CREATE INDEX IF NOT EXISTS ix_habit_journey_member_habits_owner
    ON habitflow.habit_journey_member_habits(client_id, user_id, journey_id, status);

CREATE INDEX IF NOT EXISTS ix_habit_journeys_public_catalog
    ON habitflow.habit_journeys(status, is_private, minimum_plan)
    WHERE client_id IS NULL;

INSERT INTO habitflow.feature_catalog(code,name,value_type,category,implementation_status,is_marketable)
VALUES
 ('habit_programs','Programas de habitos','Boolean','Programas','Implemented',true),
 ('habit_program_personalization','Adesao personalizada a programas','Boolean','Programas','Implemented',true)
ON CONFLICT(code) DO UPDATE
SET name=excluded.name,
    value_type=excluded.value_type,
    category=excluded.category,
    implementation_status=excluded.implementation_status,
    is_marketable=excluded.is_marketable;

INSERT INTO habitflow.plan_features(plan_id,feature_code,bool_value)
SELECT p.id, f.code, true
FROM habitflow.plans p
CROSS JOIN habitflow.feature_catalog f
WHERE f.code IN ('habit_programs','habit_program_personalization')
ON CONFLICT(plan_id,feature_code) DO UPDATE SET bool_value=excluded.bool_value, updated_at=now();

COMMIT;
