BEGIN;
ALTER TABLE habitflow.goal_habits ADD COLUMN IF NOT EXISTS client_id uuid REFERENCES habitflow.clients(id);
UPDATE habitflow.goal_habits gh SET client_id=g.client_id FROM habitflow.user_goals g WHERE g.id=gh.goal_id AND gh.client_id IS NULL;
ALTER TABLE habitflow.goal_habits ALTER COLUMN client_id SET NOT NULL;
CREATE INDEX IF NOT EXISTS ix_goal_habits_goal ON habitflow.goal_habits(client_id,goal_id);
CREATE INDEX IF NOT EXISTS ix_goal_habits_habit ON habitflow.goal_habits(client_id,habit_id);
CREATE TABLE IF NOT EXISTS habitflow.goal_progress_events (
 id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id),
 goal_id uuid NOT NULL REFERENCES habitflow.user_goals(id) ON DELETE CASCADE, previous_value integer NOT NULL, current_value integer NOT NULL,
 source_type varchar(60) NOT NULL, source_id varchar(160), created_at timestamp NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS ix_goal_progress_events_scope ON habitflow.goal_progress_events(client_id,user_id,goal_id,created_at DESC);
COMMIT;
