-- HabitFlow v6.23.0: Automação Inteligente, Jornadas Guiadas, Marketplace de Templates e IA Aplicada
BEGIN;

SET LOCAL search_path TO habitflow, public;

CREATE TABLE IF NOT EXISTS habitflow.habit_journeys (
    id uuid PRIMARY KEY,
    client_id uuid NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    name varchar(150) NOT NULL,
    description text NOT NULL,
    category varchar(60) NOT NULL,
    goal varchar(150) NOT NULL,
    suggested_duration_days integer NOT NULL DEFAULT 21,
    frequency varchar(50) NOT NULL DEFAULT 'Diário',
    difficulty varchar(30) NOT NULL DEFAULT 'Iniciante',
    tags varchar(255) NULL,
    target_audience varchar(100) NULL,
    minimum_plan varchar(50) NOT NULL DEFAULT 'Free',
    is_active boolean NOT NULL DEFAULT true,
    is_official boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_journey_difficulty CHECK (difficulty IN ('Iniciante', 'Intermediário', 'Avançado'))
);

CREATE INDEX IF NOT EXISTS ix_habit_journeys_client ON habitflow.habit_journeys(client_id, is_active);
CREATE INDEX IF NOT EXISTS ix_habit_journeys_category ON habitflow.habit_journeys(category, difficulty);

ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS language varchar(10) NOT NULL DEFAULT 'pt-BR';
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS origin varchar(40) NOT NULL DEFAULT 'Official';
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS marketplace_status varchar(30) NOT NULL DEFAULT 'Published';
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS review_notes text NULL;
ALTER TABLE habitflow.habit_templates ADD COLUMN IF NOT EXISTS last_reviewed_at timestamptz NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_habit_templates_language_v6230'
          AND conrelid = 'habitflow.habit_templates'::regclass
    ) THEN
        ALTER TABLE habitflow.habit_templates
            ADD CONSTRAINT ck_habit_templates_language_v6230 CHECK (language IN ('pt-BR','en-US','es-ES'));
    END IF;
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'ck_habit_templates_marketplace_status_v6230'
          AND conrelid = 'habitflow.habit_templates'::regclass
    ) THEN
        ALTER TABLE habitflow.habit_templates
            ADD CONSTRAINT ck_habit_templates_marketplace_status_v6230 CHECK (marketplace_status IN ('Draft','PendingReview','Published','Rejected','Archived'));
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_habit_templates_marketplace_filters
    ON habitflow.habit_templates(language, marketplace_status, category, difficulty);

CREATE TABLE IF NOT EXISTS habitflow.habit_journey_steps (
    id uuid PRIMARY KEY,
    journey_id uuid NOT NULL REFERENCES habitflow.habit_journeys(id) ON DELETE CASCADE,
    step_order integer NOT NULL,
    title varchar(150) NOT NULL,
    description text NOT NULL,
    suggested_habit_name varchar(150) NOT NULL,
    suggested_frequency varchar(50) NOT NULL DEFAULT 'Diário',
    suggested_reminder_time varchar(10) NULL DEFAULT '08:00',
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_habit_journey_steps_journey ON habitflow.habit_journey_steps(journey_id, step_order);

CREATE TABLE IF NOT EXISTS habitflow.habit_journey_members (
    id uuid PRIMARY KEY,
    journey_id uuid NOT NULL REFERENCES habitflow.habit_journeys(id) ON DELETE CASCADE,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    status varchar(30) NOT NULL DEFAULT 'Active',
    progress_percentage numeric(5,2) NOT NULL DEFAULT 0.00,
    notes text NULL,
    joined_at timestamptz NOT NULL DEFAULT now(),
    completed_at timestamptz NULL,
    CONSTRAINT ck_journey_member_status CHECK (status IN ('Active', 'Completed', 'Left')),
    CONSTRAINT uk_journey_member UNIQUE (journey_id, user_id)
);

CREATE INDEX IF NOT EXISTS ix_habit_journey_members_user ON habitflow.habit_journey_members(user_id, status);

CREATE TABLE IF NOT EXISTS habitflow.habit_automations (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    habit_id uuid NULL REFERENCES habitflow.habits(id) ON DELETE CASCADE,
    type varchar(50) NOT NULL,
    frequency varchar(30) NOT NULL DEFAULT 'Daily',
    status varchar(30) NOT NULL DEFAULT 'Active',
    quiet_hours_start varchar(5) NOT NULL DEFAULT '22:00',
    quiet_hours_end varchar(5) NOT NULL DEFAULT '07:00',
    settings_json text NULL,
    last_triggered_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_automation_type CHECK (type IN ('SmartReminder', 'WeeklyReview', 'ConsistencyAlert', 'RecoveryPlan', 'RescheduleSuggestion', 'MotivationalBoost', 'MonthlySummary')),
    CONSTRAINT ck_automation_status CHECK (status IN ('Active', 'Paused', 'Completed', 'Canceled', 'Error', 'AwaitingConfirmation'))
);

CREATE INDEX IF NOT EXISTS ix_habit_automations_user ON habitflow.habit_automations(client_id, user_id, status);

CREATE TABLE IF NOT EXISTS habitflow.habit_weekly_reviews (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    period_start date NOT NULL,
    period_end date NOT NULL,
    habits_completed integer NOT NULL DEFAULT 0,
    habits_abandoned integer NOT NULL DEFAULT 0,
    consistency_rate numeric(5,2) NOT NULL DEFAULT 0.00,
    best_streak integer NOT NULL DEFAULT 0,
    goals_achieved integer NOT NULL DEFAULT 0,
    summary_text text NOT NULL,
    recommendations_json text NULL,
    ai_generated boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_habit_weekly_reviews_user ON habitflow.habit_weekly_reviews(client_id, user_id, period_start DESC);

CREATE TABLE IF NOT EXISTS habitflow.habit_monthly_reviews (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    period_start date NOT NULL,
    period_end date NOT NULL,
    habits_completed integer NOT NULL DEFAULT 0,
    consistency_rate numeric(5,2) NOT NULL DEFAULT 0.00,
    summary_text text NOT NULL,
    recommendations_json text NULL,
    ai_generated boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uk_habit_monthly_reviews_period UNIQUE (client_id, user_id, period_start)
);

CREATE INDEX IF NOT EXISTS ix_habit_monthly_reviews_user ON habitflow.habit_monthly_reviews(client_id, user_id, period_start DESC);

CREATE TABLE IF NOT EXISTS habitflow.ai_recommendations (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    category varchar(50) NOT NULL,
    recommendation_type varchar(50) NOT NULL,
    title varchar(150) NOT NULL,
    content text NOT NULL,
    status varchar(30) NOT NULL DEFAULT 'Pending',
    ai_model varchar(100) NULL,
    provider varchar(50) NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    resolved_at timestamptz NULL,
    CONSTRAINT ck_ai_recommendation_status CHECK (status IN ('Pending', 'Accepted', 'Rejected'))
);

CREATE INDEX IF NOT EXISTS ix_ai_recommendations_user ON habitflow.ai_recommendations(client_id, user_id, status);

CREATE TABLE IF NOT EXISTS habitflow.ai_usage_events (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    provider varchar(50) NOT NULL,
    model varchar(100) NOT NULL,
    event_code varchar(80) NOT NULL,
    status varchar(30) NOT NULL,
    correlation_id varchar(80) NOT NULL,
    duration_ms integer NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ai_usage_events_tenant_month
    ON habitflow.ai_usage_events(client_id, user_id, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.ai_prompt_audit (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NOT NULL REFERENCES habitflow.users(id) ON DELETE CASCADE,
    provider varchar(50) NOT NULL,
    model varchar(100) NOT NULL,
    purpose varchar(80) NOT NULL,
    safety_status varchar(40) NOT NULL,
    prompt_hash varchar(128) NOT NULL,
    response_hash varchar(128) NULL,
    correlation_id varchar(80) NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_ai_prompt_audit_client ON habitflow.ai_prompt_audit(client_id, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.billing_trial_events (
    id uuid PRIMARY KEY,
    client_id uuid NOT NULL REFERENCES habitflow.clients(id) ON DELETE CASCADE,
    user_id uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    event_code varchar(80) NOT NULL,
    plan_code varchar(50) NOT NULL,
    trial_started_at timestamptz NULL,
    trial_ends_at timestamptz NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_billing_trial_events_client ON habitflow.billing_trial_events(client_id, created_at DESC);

CREATE TABLE IF NOT EXISTS habitflow.security_audit_events (
    id uuid PRIMARY KEY,
    client_id uuid NULL REFERENCES habitflow.clients(id) ON DELETE SET NULL,
    user_id uuid NULL REFERENCES habitflow.users(id) ON DELETE SET NULL,
    event_code varchar(80) NOT NULL,
    severity varchar(20) NOT NULL DEFAULT 'Info',
    result varchar(30) NOT NULL,
    correlation_id varchar(80) NOT NULL,
    metadata_json text NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT ck_security_audit_severity CHECK (severity IN ('Info','Warning','Critical'))
);

CREATE INDEX IF NOT EXISTS ix_security_audit_events_lookup ON habitflow.security_audit_events(client_id, user_id, created_at DESC);

-- Seed Jornadas Guiadas Oficiais
INSERT INTO habitflow.habit_journeys (id, client_id, name, description, category, goal, suggested_duration_days, frequency, difficulty, tags, target_audience, minimum_plan, is_active, is_official)
VALUES
('62300000-0000-4000-8000-000000000001', NULL, 'Dormir Melhor', 'Estabeleça uma rotina noturna consistente para melhorar a qualidade do sono e disposição matinal.', 'Saúde e Bem-estar', 'Melhorar a qualidade do sono e ter mais energia pela manhã', 21, 'Diário', 'Iniciante', 'sono,saúde,descanso,bem-estar', 'Pessoas com insônia ou cansaço matinal', 'Free', true, true),
('62300000-0000-4000-8000-000000000002', NULL, 'Produtividade e Foco', 'Técnicas diárias de blocos de tempo e eliminação de distrações para render mais no trabalho e estudos.', 'Produtividade', 'Aumentar foco e entrega sem burnout', 30, 'Diário', 'Intermediário', 'foco,tempo,produtividade,organização', 'Profissionais e estudantes', 'Free', true, true),
('62300000-0000-4000-8000-000000000003', NULL, 'Hábito da Leitura', 'Leia diariamente de forma sustentável começando com metas acessíveis até criar o prazer pelo livro.', 'Educação', 'Ler pelo menos 1 livro por mês com consistência', 21, 'Diário', 'Iniciante', 'leitura,livros,estudos,conhecimento', 'Qualquer pessoa que quer ler mais', 'Free', true, true),
('62300000-0000-4000-8000-000000000004', NULL, 'Atividade Física Constante', 'Saia do sedentarismo com passos graduais e comemore cada sessão concluída.', 'Saúde e Movimento', 'Praticar exercícios pelo menos 4 vezes por semana', 30, '4x por semana', 'Iniciante', 'exercícios,corpo,movimento,energia', 'Iniciantes em atividades físicas', 'Free', true, true),
('62300000-0000-4000-8000-000000000005', NULL, 'Saúde Mental e Atenção Plena', 'Pequenas pausas respiratórias e check-ins emocionais para reduzir a ansiedade do dia a dia.', 'Bem-estar', 'Reduzir estresse e cultivar clareza mental', 21, 'Diário', 'Iniciante', 'mindfulness,respiração,paz,ansiedade', 'Pessoas sob rotina acelerada', 'Free', true, true),
('62300000-0000-4000-8000-000000000006', NULL, 'Rotina Matinal Poderosa', 'Comece seus dias com intenção: hidratação, planejamento e foco nas primeiras horas.', 'Organização', 'Dominar a primeira hora do dia sem celular', 15, 'Diário', 'Iniciante', 'manhã,energia,intencionalidade,planejamento', 'Pessoas que acordam correndo', 'Free', true, true),
('62300000-0000-4000-8000-000000000007', NULL, 'Foco nos Estudos', 'Desenvolva disciplina de estudos com revisões periódicas e sessões sem celular.', 'Educação', 'Consistência em cursos, faculdade ou certificações', 30, 'Diário', 'Intermediário', 'estudo,provas,disciplina,concurso', 'Estudantes e concurseiros', 'Free', true, true),
('62300000-0000-4000-8000-000000000008', NULL, 'Organização Financeira', 'Registre despesas diariamente, revise metas semanais e evite gastos impulsivos.', 'Finanças', 'Controlar orçamento e formar reserva de emergência', 30, 'Diário', 'Intermediário', 'finanças,dinheiro,planejamento,economia', 'Adultos organizando despesas', 'Free', true, true),
('62300000-0000-4000-8000-000000000009', NULL, 'Hábitos Corporativos de Alto Impacto', 'Alinhamentos ágeis, comunicação assíncrona clara e encerramento pontual de expediente.', 'Corporativo', 'Aumentar a colaboração e harmonia em times', 30, 'Dias úteis', 'Intermediário', 'trabalho,equipe,alinhamento,saúde corporativa', 'Times e gestores corporativos', 'Team', true, true),
('62300000-0000-4000-8000-000000000010', NULL, 'Onboarding de Colaboradores', 'Checklist guiado de primeiros passos na empresa, rotina de boas-vindas e metas de 15 dias.', 'Corporativo', 'Acelerar integração de novos membros no workspace', 15, 'Dias úteis', 'Iniciante', 'onboarding,rh,integração,cultura', 'Novos colaboradores e gestores', 'Team', true, true)
ON CONFLICT (id) DO NOTHING;

-- Seed Passos das Jornadas
INSERT INTO habitflow.habit_journey_steps (id, journey_id, step_order, title, description, suggested_habit_name, suggested_frequency, suggested_reminder_time)
VALUES
('62300001-0000-4000-8000-000000000001', '62300000-0000-4000-8000-000000000001', 1, 'Desconectar Telas 30min Antes', 'Desligue smartphone e TV 30 minutos antes de deitar.', 'Desconectar telas à noite', 'Diário', '22:30'),
('62300001-0000-4000-8000-000000000002', '62300000-0000-4000-8000-000000000001', 2, 'Horário Fixo de Deitar', 'Mantenha o mesmo horário de dormir para sincronizar o ciclo circadiano.', 'Dormir no horário planejado', 'Diário', '23:00'),
('62300001-0000-4000-8000-000000000003', '62300000-0000-4000-8000-000000000002', 1, 'Planejar as 3 Prioridades do Dia', 'Defina apenas 3 grandes tarefas no início da jornada.', 'Definir 3 prioridades diárias', 'Dias úteis', '08:30'),
('62300001-0000-4000-8000-000000000004', '62300000-0000-4000-8000-000000000002', 2, 'Bloco de Foco sem Notificações', 'Trabalhe 45 minutos com celular em modo foco.', 'Bloco de foco profundo', 'Dias úteis', '10:00'),
('62300001-0000-4000-8000-000000000005', '62300000-0000-4000-8000-000000000003', 1, 'Leitura de 15 Minutos', 'Leia pelo menos 10 páginas ou 15 minutos de um livro.', 'Ler 15 minutos', 'Diário', '20:00'),
('62300001-0000-4000-8000-000000000006', '62300000-0000-4000-8000-000000000004', 1, 'Caminhada ou Treino 30min', 'Pratique 30 minutos de caminhada, musculação ou esporte.', 'Atividade física 30min', '4x por semana', '07:00'),
('62300001-0000-4000-8000-000000000007', '62300000-0000-4000-8000-000000000005', 1, 'Pausa de Respiração Consciente', 'Reserve 5 minutos para respiração lenta e desacelerar a mente.', 'Respiração consciente 5min', 'Diário', '14:00'),
('62300001-0000-4000-8000-000000000008', '62300000-0000-4000-8000-000000000006', 1, 'Beber 500ml de Água ao Acordar', 'Hidrate o corpo imediatamente ao levantar.', 'Água matinal 500ml', 'Diário', '07:00'),
('62300001-0000-4000-8000-000000000009', '62300000-0000-4000-8000-000000000007', 1, 'Sessão Pomodoro de Estudo', 'Estude 25 a 50 minutos com dedicação exclusiva.', 'Sessão focada de estudos', 'Diário', '19:00'),
('62300001-0000-4000-8000-000000000010', '62300000-0000-4000-8000-000000000008', 1, 'Anotar Gastos do Dia', 'Registre todas as despesas diárias para conscientização.', 'Registro de gastos diários', 'Diário', '21:00')
ON CONFLICT (id) DO NOTHING;

COMMIT;
