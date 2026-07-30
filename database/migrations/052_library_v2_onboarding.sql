-- transaction-mode: runner-managed
create extension if not exists unaccent;

alter table habitflow.habit_templates add column if not exists suggested_days smallint[] not null default '{}';
alter table habitflow.habit_templates add column if not exists suggested_target_per_week integer null;
alter table habitflow.habit_templates add column if not exists suggested_reminder_time time null;
alter table habitflow.habit_templates add column if not exists icon_code varchar(80) null;
alter table habitflow.habit_templates add column if not exists why_it_helps text null;
alter table habitflow.habit_templates add column if not exists how_to_start text null;
alter table habitflow.habit_templates add column if not exists first_action text null;
alter table habitflow.habit_templates add column if not exists tags text[] not null default '{}';
alter table habitflow.habit_templates add column if not exists minimum_plan_code varchar(40) not null default 'free';
alter table habitflow.habit_templates add column if not exists is_featured boolean not null default false;
alter table habitflow.habit_templates add column if not exists content_version integer not null default 1;
alter table habitflow.habit_templates add column if not exists published_at timestamp null;
alter table habitflow.habit_templates add constraint ck_habit_templates_days check (suggested_days <@ array[0,1,2,3,4,5,6]::smallint[]);
alter table habitflow.habit_templates add constraint ck_habit_templates_week_target check (suggested_target_per_week is null or suggested_target_per_week between 1 and 7);

alter table habitflow.habits add column if not exists client_id uuid null references habitflow.clients(id);
alter table habitflow.habits add column if not exists source_template_id uuid null references habitflow.habit_templates(id);
alter table habitflow.habits add column if not exists source_collection_id uuid null;
alter table habitflow.habits add column if not exists objective_id uuid null references habitflow.habit_objectives(id);
alter table habitflow.habits add column if not exists icon_code varchar(80) null;
alter table habitflow.habits add column if not exists difficulty varchar(50) null;
alter table habitflow.habits add column if not exists estimated_time_minutes integer null;
alter table habitflow.habits add column if not exists start_date date not null default current_date;
alter table habitflow.habits add column if not exists template_content_version integer null;
alter table habitflow.habits add column if not exists is_template_variation boolean not null default false;
alter table habitflow.habits add column if not exists template_idempotency_key uuid null;
create index if not exists ix_habits_source_template on habitflow.habits(client_id,user_id,source_template_id) where is_archived=false;
create unique index if not exists ux_habits_template_idempotency on habitflow.habits(client_id,user_id,template_idempotency_key) where template_idempotency_key is not null;

create table if not exists habitflow.habit_template_favorites (
 client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 template_id uuid not null references habitflow.habit_templates(id), created_at timestamptz not null default now(),
 primary key(client_id,user_id,template_id));

create table if not exists habitflow.habit_template_collections (
 id uuid primary key, slug varchar(100) not null unique, name varchar(140) not null, description text not null,
 objective_id uuid null references habitflow.habit_objectives(id), icon_code varchar(80), estimated_time_minutes integer,
 difficulty varchar(50) not null, minimum_plan_code varchar(40) not null default 'free', is_featured boolean not null default false,
 status varchar(20) not null default 'Draft', content_version integer not null default 1, sort_order integer not null default 0,
 created_at timestamp not null default now(), updated_at timestamp not null default now());
alter table habitflow.habits add constraint fk_habits_source_collection foreign key(source_collection_id) references habitflow.habit_template_collections(id);
create table if not exists habitflow.habit_template_collection_items (
 collection_id uuid not null references habitflow.habit_template_collections(id), template_id uuid not null references habitflow.habit_templates(id),
 sort_order integer not null default 0, is_required boolean not null default false, default_reminder_time time null,
 can_customize boolean not null default true, primary key(collection_id,template_id));

create table if not exists habitflow.user_onboarding_progress (
 client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id), current_step smallint not null default 1,
 selected_objective_slug varchar(80), available_minutes integer, preferred_frequency varchar(50), preferred_days smallint[] not null default '{}',
 preferred_time time, selected_template_ids uuid[] not null default '{}', selected_collection_id uuid null references habitflow.habit_template_collections(id),
 create_goal boolean not null default false, goal_target_type varchar(40), goal_target_value numeric(12,2), started_at timestamptz not null default now(),
 last_activity_at timestamptz not null default now(), completed_at timestamptz, skipped_at timestamptz, version integer not null default 1,
 primary key(client_id,user_id), constraint ck_onboarding_days check(preferred_days <@ array[0,1,2,3,4,5,6]::smallint[]),
 constraint ck_onboarding_terminal check(completed_at is null or skipped_at is null));
