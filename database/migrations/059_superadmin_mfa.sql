begin;

create table if not exists habitflow.user_mfa_settings (
    user_id uuid primary key references habitflow.users(id) on delete cascade,
    client_id uuid null references habitflow.clients(id) on delete cascade,
    protected_secret text not null,
    is_enabled boolean not null default false,
    last_accepted_time_step bigint null,
    created_at timestamptz not null default now(),
    enabled_at timestamptz null
);

create table if not exists habitflow.user_mfa_recovery_codes (
    id uuid primary key,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    client_id uuid null references habitflow.clients(id) on delete cascade,
    code_hash char(64) not null,
    created_at timestamptz not null default now(),
    used_at timestamptz null,
    unique (user_id, code_hash)
);

create table if not exists habitflow.user_mfa_challenges (
    id uuid primary key,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    client_id uuid null references habitflow.clients(id) on delete cascade,
    failed_attempts integer not null default 0 check (failed_attempts between 0 and 5),
    expires_at timestamptz not null,
    verified_at timestamptz null
);

create table if not exists habitflow.user_security_events (
    id uuid primary key,
    user_id uuid not null references habitflow.users(id) on delete cascade,
    client_id uuid null references habitflow.clients(id) on delete cascade,
    event_type varchar(80) not null,
    occurred_at timestamptz not null default now()
);

create index if not exists ix_mfa_recovery_owner on habitflow.user_mfa_recovery_codes(user_id, client_id) where used_at is null;
create index if not exists ix_mfa_challenge_owner on habitflow.user_mfa_challenges(user_id, client_id, expires_at desc);
create index if not exists ix_security_event_owner on habitflow.user_security_events(user_id, client_id, occurred_at desc);

commit;
