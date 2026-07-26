create schema if not exists habitflow;

create index if not exists ix_habitflow_clients_created_at on habitflow.clients(created_at);
create index if not exists ix_habitflow_clients_person_type on habitflow.clients(person_type);
create index if not exists ix_habitflow_clients_document_normalized on habitflow.clients(document_normalized);
create index if not exists ix_habitflow_users_client_id on habitflow.users(client_id);
create index if not exists ix_habitflow_users_role on habitflow.users(role);

create or replace view habitflow.vw_clients_without_admin as
select c.* from habitflow.clients c
where not exists (select 1 from habitflow.users u where u.client_id = c.id and u.role = 'Admin');

create or replace view habitflow.vw_users_without_client as
select u.id, u.name, u.email, u.role, u.created_at from habitflow.users u
where u.role <> 'SuperAdmin' and u.client_id is null;

create or replace view habitflow.vw_client_registration_quality as
select c.id client_id, c.created_at, c.person_type, c.name, c.document, c.document_normalized, c.email, c.plan, c.benefits_status, c.payment_status,
       exists(select 1 from habitflow.users u where u.client_id = c.id and u.role = 'Admin') has_admin,
       (c.document_normalized ~ '^[0-9]{11}$|^[0-9]{14}$') document_shape_valid
from habitflow.clients c;

insert into habitflow.schema_migrations(id, name, applied_at)
values ('031','registration_claims_onboarding_quality',now())
on conflict (id) do nothing;
