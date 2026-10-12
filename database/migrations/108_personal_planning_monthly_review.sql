-- HabitFlow 108 - prioridades pessoais, proximo passo e revisao mensal

create table if not exists habitflow.personal_priorities (
    id uuid primary key,
    client_id uuid not null references habitflow.clients(id) on delete cascade,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    goal_id uuid not null references habitflow.user_goals(id) on delete cascade,
    priority_order int not null check (priority_order between 1 and 12),
    weekly_available_minutes int null check (weekly_available_minutes is null or weekly_available_minutes between 1 and 10080),
    effective_from date not null,
    effective_until date null,
    version int not null default 1,
    created_at timestamp not null default now(),
    updated_at timestamp not null default now(),
    constraint ck_personal_priorities_period check (effective_until is null or effective_until >= effective_from),
    constraint ux_personal_priorities_active_order unique (client_id, user_id, effective_from, priority_order),
    constraint ux_personal_priorities_goal_period unique (client_id, user_id, goal_id, effective_from)
);

create index if not exists ix_personal_priorities_user_active
    on habitflow.personal_priorities(client_id, user_id, effective_from, priority_order)
    where effective_until is null;

create table if not exists habitflow.dismissed_next_steps (
    id uuid primary key,
    client_id uuid not null references habitflow.clients(id) on delete cascade,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    suggestion_key varchar(160) not null,
    dismissed_for_date date not null,
    created_at timestamp not null default now(),
    constraint ux_dismissed_next_steps_day unique (client_id, user_id, suggestion_key, dismissed_for_date)
);

create index if not exists ix_dismissed_next_steps_user_day
    on habitflow.dismissed_next_steps(client_id, user_id, dismissed_for_date);

create table if not exists habitflow.monthly_reviews (
    id uuid primary key,
    client_id uuid not null references habitflow.clients(id) on delete cascade,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    period_start date not null,
    period_end date not null,
    status varchar(20) not null default 'Draft' check (status in ('Draft','Completed')),
    reflection text null,
    selected_next_step varchar(120) null,
    confirmed_plan_code varchar(80) null,
    idempotency_key varchar(120) not null,
    version int not null default 1,
    created_at timestamp not null default now(),
    updated_at timestamp not null default now(),
    completed_at timestamp null,
    constraint ck_monthly_reviews_period check (period_end >= period_start),
    constraint ux_monthly_reviews_period unique (client_id, user_id, period_start),
    constraint ux_monthly_reviews_idempotency unique (client_id, user_id, idempotency_key)
);

create index if not exists ix_monthly_reviews_user_history
    on habitflow.monthly_reviews(client_id, user_id, period_start desc);
