-- Contract adapter for a deterministic database snapshot immediately before migration 032.
-- Earlier legacy migrations may already have created and self-registered this table.
create schema if not exists habitflow;
create table if not exists habitflow.schema_migrations (
    id varchar(120) primary key,
    name varchar(200) not null,
    applied_at timestamptz not null default now()
);
alter table habitflow.schema_migrations add column if not exists checksum varchar(64);
alter table habitflow.schema_migrations add column if not exists filename varchar(260);
alter table habitflow.schema_migrations add column if not exists app_version varchar(80);
