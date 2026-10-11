-- transaction-mode: runner-managed
-- Experimentos pessoais de rotina com histórico e aprendizados privados.
create table if not exists habitflow.routine_experiments (
 id uuid primary key,
 client_id uuid not null references habitflow.clients(id),
 user_id uuid not null references habitflow.users(id),
 habit_id uuid not null references habitflow.habits(id),
 name varchar(120) not null,
 difficulty varchar(500) not null,
 adjustment_type varchar(40) not null,
 proposed_adjustment varchar(1000) not null,
 test_start date not null,
 test_end date not null,
 baseline_start date not null,
 baseline_end date not null,
 primary_indicator varchar(40) not null,
 status varchar(20) not null default 'Draft',
 reflection varchar(1000) null,
 previous_configuration_json jsonb not null default '{}'::jsonb,
 proposed_configuration_json jsonb not null default '{}'::jsonb,
 decision varchar(24) not null default 'None',
 version integer not null default 1,
 created_at timestamptz not null default now(),
 updated_at timestamptz not null default now(),
 started_at timestamptz null,
 closed_at timestamptz null,
 constraint ck_routine_experiment_adjustment check(adjustment_type in ('ChangeTime','ReduceFrequency','UseMinimumVersion','AdjustEstimatedDuration','ModifyReminder')),
 constraint ck_routine_experiment_indicator check(primary_indicator in ('CompletionCount','CompletionRate','MinimumVersionCount','FullTargetCount')),
 constraint ck_routine_experiment_status check(status in ('Draft','Active','Paused','Completed','Canceled')),
 constraint ck_routine_experiment_decision check(decision in ('KeepCurrent','RestorePrevious','ReviewManually','None')),
 constraint ck_routine_experiment_periods check(test_end >= test_start and baseline_end >= baseline_start and test_start > baseline_end),
 constraint ck_routine_experiment_version check(version > 0)
);
create index if not exists ix_routine_experiments_habit_period on habitflow.routine_experiments(client_id,user_id,habit_id,test_start desc);
create index if not exists ix_routine_experiments_active_conflict on habitflow.routine_experiments(client_id,user_id,habit_id,adjustment_type,status,test_start,test_end);

create table if not exists habitflow.routine_experiment_events (
 id uuid primary key,
 client_id uuid not null references habitflow.clients(id),
 user_id uuid not null references habitflow.users(id),
 experiment_id uuid not null references habitflow.routine_experiments(id) on delete cascade,
 event_type varchar(40) not null,
 message varchar(500) not null,
 created_at timestamptz not null default now()
);
create index if not exists ix_routine_experiment_events_experiment on habitflow.routine_experiment_events(client_id,user_id,experiment_id,created_at);

create table if not exists habitflow.routine_experiment_learnings (
 id uuid primary key,
 client_id uuid not null references habitflow.clients(id),
 user_id uuid not null references habitflow.users(id),
 habit_id uuid not null references habitflow.habits(id),
 experiment_id uuid not null references habitflow.routine_experiments(id) on delete cascade,
 outcome varchar(20) not null,
 observation varchar(2000) not null,
 is_private boolean not null default true,
 created_at timestamptz not null default now(),
 constraint ck_routine_learning_outcome check(outcome in ('Useful','Inconclusive','NotUseful'))
);
create index if not exists ix_routine_learnings_habit on habitflow.routine_experiment_learnings(client_id,user_id,habit_id,created_at desc);
