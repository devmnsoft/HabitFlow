-- HabitFlow v6.19.8: implantação, templates por tenant e eventos de sucesso.
-- Não guarda prompt, segredo nem mensagem de conversa.
BEGIN;
SET LOCAL search_path TO habitflow, public;

ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS audience varchar(160);
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS goal_text varchar(300);
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS created_by uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL;
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS client_id uuid NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE;

UPDATE habitflow.habit_templates
SET audience = COALESCE(audience, 'Uso pessoal'),
    goal_text = COALESCE(goal_text, benefit_text)
WHERE client_id IS NULL AND (audience IS NULL OR goal_text IS NULL);

INSERT INTO habitflow.habit_templates(
    id, objective_id, name, description, category, suggested_frequency, suggested_color, difficulty,
    estimated_time_minutes, benefit_text, sort_order, is_active, minimum_plan_code, audience, goal_text, published_at)
SELECT v.id, v.objective_id, v.name, v.description, v.category, 'Daily', '#16A34A', 'Easy',
    v.minutes, v.goal_text, v.sort_order, true, v.minimum_plan, v.audience, v.goal_text, now()
FROM (VALUES
    ('01980000-0000-0000-0000-000000000001'::uuid, '10000000-0000-0000-0000-000000000005'::uuid, 'Começar o dia com uma prioridade', 'Escolha uma tarefa curta antes de abrir outras demandas.', 'Rotina matinal', 10, 20, 'free', 'Quem quer abrir o dia com uma ação só.', 'Definir a primeira prioridade do dia.'),
    ('01980000-0000-0000-0000-000000000002'::uuid, '10000000-0000-0000-0000-000000000006'::uuid, 'Encerrar o dia sem telas', 'Reserve os últimos minutos da noite sem tela.', 'Rotina noturna', 10, 21, 'free', 'Quem quer um fechamento curto do dia.', 'Reduzir tela antes de dormir.'),
    ('01980000-0000-0000-0000-000000000003'::uuid, '10000000-0000-0000-0000-000000000003'::uuid, 'Bloco de foco de 25 minutos', 'Um bloco curto, sem trocar de tarefa.', 'Foco', 25, 22, 'free', 'Quem precisa de um intervalo de concentração.', 'Concluir um bloco de foco.'),
    ('01980000-0000-0000-0000-000000000004'::uuid, '10000000-0000-0000-0000-000000000003'::uuid, 'Alinhar a equipe em 10 minutos', 'Uma pauta curta com a equipe, sem expor hábito privado.', 'Corporativo', 10, 23, 'team', 'Times com plano Team ou Enterprise.', 'Alinhar a prioridade do time.'),
    ('01980000-0000-0000-0000-000000000005'::uuid, '10000000-0000-0000-0000-000000000005'::uuid, 'Receber um colega novo', 'Checklist leve de boas-vindas do time.', 'Onboarding de equipe', 15, 24, 'team', 'Quem implanta um novo colega.', 'Concluir a recepção inicial.')
) AS v(id, objective_id, name, description, category, minutes, sort_order, minimum_plan, audience, goal_text)
WHERE NOT EXISTS (
    SELECT 1 FROM habitflow.habit_templates t WHERE t.objective_id = v.objective_id AND t.name = v.name
);

ALTER TABLE habitflow.habit_templates DROP CONSTRAINT IF EXISTS uq_habitflow_habit_templates_objective_name;
CREATE UNIQUE INDEX IF NOT EXISTS ux_habit_templates_scope_name
    ON habitflow.habit_templates (objective_id, name, COALESCE(client_id, '00000000-0000-0000-0000-000000000000'::uuid));
CREATE INDEX IF NOT EXISTS ix_habit_templates_client ON habitflow.habit_templates(client_id, is_active);

CREATE TABLE IF NOT EXISTS habitflow.tenant_onboarding_step_overrides (
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    step_code varchar(40) NOT NULL,
    status varchar(20) NOT NULL,
    reason varchar(500) NULL,
    updated_by uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    updated_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (client_id, step_code),
    CONSTRAINT ck_onboarding_step_status CHECK (status IN ('Pendente', 'EmAndamento', 'Concluido', 'Bloqueado', 'Ignorado')),
    CONSTRAINT ck_onboarding_step_reason CHECK (status <> 'Ignorado' OR (reason IS NOT NULL AND char_length(btrim(reason)) >= 5))
);

CREATE TABLE IF NOT EXISTS habitflow.customer_success_events (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    event_code varchar(80) NOT NULL,
    score integer NULL,
    status varchar(40) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_customer_success_score CHECK (score IS NULL OR score BETWEEN 0 AND 100)
);
CREATE INDEX IF NOT EXISTS ix_customer_success_events_client ON habitflow.customer_success_events(client_id, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.notification_events (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    event_code varchar(80) NOT NULL,
    channel varchar(20) NOT NULL,
    status varchar(20) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_notification_events_channel CHECK (channel IN ('in-app', 'email', 'whatsapp', 'manual')),
    CONSTRAINT ck_notification_events_status CHECK (status IN ('created', 'suppressed', 'unavailable'))
);
CREATE INDEX IF NOT EXISTS ix_notification_events_client ON habitflow.notification_events(client_id, created_at DESC);

COMMIT;
