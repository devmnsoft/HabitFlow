-- transaction-mode: runner-managed
-- Sessões nomeadas de rotina com continuidade multiaba/dispositivo e histórico imutável por snapshot.
create table if not exists habitflow.routine_sessions (
 id uuid primary key,
 client_id uuid not null references habitflow.clients(id),
 user_id uuid not null references habitflow.users(id),
 name varchar(140) not null,
 source_routine_id uuid null,
 source_routine_type varchar(40) not null default 'Habits',
 status varchar(20) not null default 'Prepared',
 local_date date not null,
 available_minutes integer null,
 active_account_session_id varchar(120) null,
 reflection text null,
 closure_summary text null,
 idempotency_key varchar(120) not null,
 version integer not null default 1,
 created_at timestamptz not null default now(),
 updated_at timestamptz not null default now(),
 started_at timestamptz null,
 paused_at timestamptz null,
 resumed_at timestamptz null,
 closed_at timestamptz null,
 canceled_at timestamptz null,
 constraint ck_routine_sessions_status check(status in ('Prepared','InProgress','Paused','Closed','Canceled')),
 constraint ck_routine_sessions_available_minutes check(available_minutes is null or available_minutes between 1 and 1440),
 constraint ck_routine_sessions_version check(version > 0),
 constraint ux_routine_sessions_idempotency unique(client_id,user_id,idempotency_key)
);

create unique index if not exists ux_routine_sessions_one_active
 on habitflow.routine_sessions(client_id,user_id)
 where status in ('Prepared','InProgress','Paused');
create index if not exists ix_routine_sessions_history
 on habitflow.routine_sessions(client_id,user_id,local_date desc,updated_at desc);
create index if not exists ix_routine_sessions_account_session
 on habitflow.routine_sessions(client_id,user_id,active_account_session_id)
 where active_account_session_id is not null and status in ('Prepared','InProgress','Paused');

create table if not exists habitflow.routine_session_items (
 id uuid primary key,
 session_id uuid not null references habitflow.routine_sessions(id) on delete cascade,
 habit_id uuid not null references habitflow.habits(id),
 habit_name_snapshot varchar(160) not null,
 habit_category_snapshot varchar(80) null,
 sort_order integer not null,
 status varchar(24) not null default 'Pending',
 planned_quantity numeric(10,2) null,
 planned_unit varchar(30) null,
 minimum_quantity numeric(10,2) null,
 minimum_name varchar(80) null,
 recorded_quantity numeric(10,2) null,
 recorded_unit varchar(30) null,
 estimated_minutes integer null,
 effective_minutes integer null,
 skipped boolean not null default false,
 skip_reason varchar(240) null,
 completion_id uuid null references habitflow.habit_completions(id) on delete set null,
 created_at timestamptz not null default now(),
 updated_at timestamptz not null default now(),
 completed_at timestamptz null,
 constraint ck_routine_session_items_status check(status in ('Pending','MinimumReached','Completed','Skipped')),
 constraint ck_routine_session_items_sort_order check(sort_order > 0),
 constraint ck_routine_session_items_estimated check(estimated_minutes is null or estimated_minutes between 1 and 1440),
 constraint ck_routine_session_items_effective check(effective_minutes is null or effective_minutes between 0 and 1440),
 constraint ux_routine_session_items_habit unique(session_id,habit_id)
);
create index if not exists ix_routine_session_items_session
 on habitflow.routine_session_items(session_id,sort_order);
create index if not exists ix_routine_session_items_habit
 on habitflow.routine_session_items(habit_id,session_id);

create table if not exists habitflow.routine_session_transitions (
 id uuid primary key,
 session_id uuid not null references habitflow.routine_sessions(id) on delete cascade,
 command varchar(40) not null,
 from_status varchar(20) not null,
 to_status varchar(20) not null,
 idempotency_key varchar(120) not null,
 correlation_id varchar(120) not null,
 created_at timestamptz not null default now(),
 constraint ux_routine_session_transition_idempotency unique(session_id,idempotency_key)
);
create index if not exists ix_routine_session_transitions_session
 on habitflow.routine_session_transitions(session_id,created_at);
