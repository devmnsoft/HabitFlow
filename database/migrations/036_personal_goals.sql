BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.user_goals (
 id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id),
 objective_slug varchar(100), title varchar(160) NOT NULL, description text, target_type varchar(80) NOT NULL,
 target_value integer NOT NULL CHECK(target_value > 0), current_value integer NOT NULL DEFAULT 0 CHECK(current_value >= 0),
 start_date date NOT NULL, end_date date, status varchar(40) NOT NULL DEFAULT 'Active', color varchar(20), icon varchar(80),
 created_at timestamp NOT NULL DEFAULT now(), updated_at timestamp NOT NULL DEFAULT now(), completed_at timestamp,
 CONSTRAINT ck_user_goals_target_type CHECK(target_type IN ('HabitCompletions','ActiveDays','StreakDays','WeeklyCompletions','Custom')),
 CONSTRAINT ck_user_goals_status CHECK(status IN ('Active','Completed','Paused','Canceled')), CONSTRAINT ck_user_goals_dates CHECK(end_date IS NULL OR end_date >= start_date)
);
CREATE INDEX IF NOT EXISTS ix_user_goals_client_user_status ON habitflow.user_goals(client_id,user_id,status);
CREATE TABLE IF NOT EXISTS habitflow.goal_habits (
 goal_id uuid NOT NULL REFERENCES habitflow.user_goals(id) ON DELETE CASCADE, habit_id uuid NOT NULL REFERENCES habitflow.habits(id) ON DELETE CASCADE,
 created_at timestamp NOT NULL DEFAULT now(), PRIMARY KEY(goal_id,habit_id)
);
COMMIT;
