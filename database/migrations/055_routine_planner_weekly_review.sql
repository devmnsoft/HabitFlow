-- transaction-mode: runner-managed
-- Planejamento diário e revisão semanal, sempre isolados no schema habitflow.
create table if not exists habitflow.habit_schedule_exceptions (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id), habit_id uuid not null references habitflow.habits(id),
 local_date date not null, type varchar(16) not null, destination_date date null, reason varchar(240), version integer not null default 1,
 created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 constraint ck_schedule_exception_type check(type in ('Excused','Moved','Added')), constraint ck_schedule_exception_version check(version>0),
 constraint ck_schedule_exception_move check((type='Moved' and destination_date>local_date) or (type<>'Moved' and destination_date is null)), unique(client_id,user_id,habit_id,local_date)
);
create index if not exists ix_schedule_exceptions_range on habitflow.habit_schedule_exceptions(client_id,user_id,local_date);
create table if not exists habitflow.daily_routine_overrides (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id), habit_id uuid not null references habitflow.habits(id), local_date date not null,
 preferred_time time null, sort_order integer not null default 0, version integer not null default 1, created_at timestamptz not null default now(), updated_at timestamptz not null default now(),
 constraint ck_daily_override_version check(version>0), unique(client_id,user_id,habit_id,local_date)
);
create index if not exists ix_daily_overrides_day on habitflow.daily_routine_overrides(client_id,user_id,local_date,sort_order);
create table if not exists habitflow.weekly_reviews (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id), period_start date not null, period_end date not null,
 status varchar(16) not null default 'Draft', idempotency_key varchar(80) not null, version integer not null default 1, created_at timestamptz not null default now(), completed_at timestamptz null,
 constraint ck_weekly_review_status check(status in ('Draft','Completed')), constraint ck_weekly_review_period check(period_end=period_start+6), constraint ck_weekly_review_version check(version>0),
 unique(client_id,user_id,period_start), unique(client_id,user_id,idempotency_key)
);
create index if not exists ix_weekly_reviews_user_period on habitflow.weekly_reviews(client_id,user_id,period_start desc);
