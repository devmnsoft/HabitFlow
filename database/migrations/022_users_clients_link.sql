alter table habitflow.users add column if not exists client_id uuid null references habitflow.clients(id);
create index if not exists ix_habitflow_users_client_id on habitflow.users(client_id);
