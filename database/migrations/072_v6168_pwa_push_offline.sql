-- HabitFlow v6.16.8: tenant-safe Web Push persistence and idempotent offline events.
begin;
create table if not exists habitflow.push_subscriptions (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 endpoint text not null, p256dh text not null, auth text not null, device_name varchar(80) not null,
 is_active boolean not null default true, created_at timestamptz not null default now(), last_seen_at timestamptz,
 unique(client_id,user_id,endpoint));
create index if not exists ix_push_subscriptions_owner_active on habitflow.push_subscriptions(client_id,user_id,is_active);

create table if not exists habitflow.notification_preferences (
 client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 push_enabled boolean not null default false, internal_enabled boolean not null default true,
 quiet_start time, quiet_end time, maximum_per_day integer not null default 5 check(maximum_per_day between 1 and 20),
 paused_until timestamptz, updated_at timestamptz not null default now(), primary key(client_id,user_id));

create table if not exists habitflow.push_delivery_attempts (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 subscription_id uuid not null references habitflow.push_subscriptions(id) on delete cascade,
 status varchar(20) not null check(status in ('Delivered','Failed')), error_code varchar(80), attempted_at timestamptz not null default now());
create index if not exists ix_push_attempts_owner_date on habitflow.push_delivery_attempts(client_id,user_id,attempted_at desc);

create table if not exists habitflow.offline_sync_events (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 action varchar(30) not null check(action in ('complete','undo','snooze')), entity_id uuid not null,
 status varchar(20) not null default 'Processed', created_at timestamptz not null default now(), expires_at timestamptz not null,
 unique(client_id,user_id,id));
create index if not exists ix_offline_sync_expiry on habitflow.offline_sync_events(expires_at);
commit;
