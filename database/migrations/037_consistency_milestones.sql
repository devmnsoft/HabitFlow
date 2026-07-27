BEGIN;
CREATE TABLE IF NOT EXISTS habitflow.milestones (id uuid PRIMARY KEY, code varchar(80) NOT NULL UNIQUE, title varchar(120) NOT NULL, description varchar(240) NOT NULL, threshold integer, is_active boolean NOT NULL DEFAULT true, created_at timestamp NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS habitflow.user_milestones (id uuid PRIMARY KEY, client_id uuid NOT NULL REFERENCES habitflow.clients(id), user_id uuid NOT NULL REFERENCES habitflow.users(id), milestone_id uuid NOT NULL REFERENCES habitflow.milestones(id), achieved_at timestamp NOT NULL DEFAULT now(), metadata jsonb, UNIQUE(user_id,milestone_id));
CREATE INDEX IF NOT EXISTS ix_user_milestones_client_user ON habitflow.user_milestones(client_id,user_id,achieved_at DESC);
INSERT INTO habitflow.milestones(id,code,title,description,threshold) VALUES
('37100000-0000-0000-0000-000000000001','first_step','Primeiro passo','Você concluiu seu primeiro hábito.',1),
('37100000-0000-0000-0000-000000000003','present_3','3 dias presentes','Você esteve presente por 3 dias.',3),
('37100000-0000-0000-0000-000000000007','rhythm_7','7 dias de ritmo','Você manteve seu ritmo por 7 dias.',7),
('37100000-0000-0000-0000-000000000015','consistency_15','15 dias de constância','Sua constância chegou a 15 dias.',15),
('37100000-0000-0000-0000-000000000030','evolution_30','30 dias de evolução','Você cuidou da sua rotina por 30 dias.',30)
ON CONFLICT(code) DO UPDATE SET title=EXCLUDED.title,description=EXCLUDED.description,threshold=EXCLUDED.threshold;
COMMIT;
