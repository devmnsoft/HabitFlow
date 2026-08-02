-- transaction-mode: runner-managed
create table if not exists habitflow.user_onboarding_draft_items (
 id uuid primary key,
 client_id uuid not null references habitflow.clients(id),
 user_id uuid not null references habitflow.users(id),
 template_id uuid not null references habitflow.habit_templates(id),
 collection_id uuid null references habitflow.habit_template_collections(id),
 name varchar(120) not null,
 frequency varchar(40) not null,
 days smallint[] not null default '{}',
 target_per_week integer null,
 preferred_time time null,
 color varchar(10) not null,
 category varchar(80) null,
 is_required boolean not null default false,
 sort_order integer not null default 0,
 created_at timestamptz not null default now(),
 constraint ck_onboarding_draft_days check(days <@ array[0,1,2,3,4,5,6]::smallint[]),
 constraint ck_onboarding_draft_target check(target_per_week is null or target_per_week between 1 and 7),
 unique(client_id,user_id,template_id)
);
create index if not exists ix_onboarding_draft_owner on habitflow.user_onboarding_draft_items(client_id,user_id,sort_order);
