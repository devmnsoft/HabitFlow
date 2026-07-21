create table if not exists habitflow.clients (
    id uuid primary key,
    name varchar(180) not null,
    legal_name varchar(220) null,
    document varchar(30) null,
    email varchar(200) null,
    phone varchar(40) null,
    contact_name varchar(160) null,
    plan varchar(80) not null default 'Free',
    status varchar(80) not null default 'Active',
    notes text null,
    is_active boolean not null default true,
    created_at timestamp not null default now(),
    updated_at timestamp not null default now(),
    constraint ck_habitflow_clients_status check (status in ('Active', 'Inactive', 'Blocked')),
    constraint ck_habitflow_clients_plan check (plan in ('Free', 'Premium', 'Enterprise'))
);
create unique index if not exists ux_habitflow_clients_document_not_empty on habitflow.clients(document) where document is not null and btrim(document) <> '';
create index if not exists ix_habitflow_clients_name on habitflow.clients(name);
create index if not exists ix_habitflow_clients_email on habitflow.clients(email);
create index if not exists ix_habitflow_clients_document on habitflow.clients(document);
create index if not exists ix_habitflow_clients_status on habitflow.clients(status);
create index if not exists ix_habitflow_clients_created_at on habitflow.clients(created_at);
