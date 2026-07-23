set search_path to habitflow;

create table if not exists habitflow.user_invites(
    id uuid primary key,
    client_id uuid not null references habitflow.clients(id),
    email varchar(200) not null,
    role varchar(80) not null default 'User',
    token_hash text not null,
    status varchar(40) not null default 'Pending',
    invited_by_user_id uuid null references habitflow.users(id),
    accepted_by_user_id uuid null references habitflow.users(id),
    expires_at timestamp not null,
    accepted_at timestamp null,
    canceled_at timestamp null,
    created_at timestamp not null default now(),
    updated_at timestamp not null default now(),
    constraint ck_habitflow_user_invites_status check (status in ('Pending','Accepted','Expired','Canceled')),
    constraint ck_habitflow_user_invites_role check (role in ('User','Admin'))
);

create unique index if not exists ux_habitflow_user_invites_token_hash on habitflow.user_invites(token_hash);
create index if not exists ix_habitflow_user_invites_client_id on habitflow.user_invites(client_id);
create index if not exists ix_habitflow_user_invites_email_status on habitflow.user_invites(email, status);
