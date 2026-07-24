-- v6.1.1 - Public SaaS registration with CPF/CNPJ.
set search_path to habitflow;

alter table habitflow.clients add column if not exists person_type varchar(20) not null default 'LegalPerson';
alter table habitflow.clients add column if not exists document_type varchar(10) not null default 'CNPJ';
alter table habitflow.clients add column if not exists document_raw varchar(30);
alter table habitflow.clients add column if not exists document_normalized varchar(20);
alter table habitflow.clients add column if not exists legal_name varchar(180);
alter table habitflow.clients add column if not exists trade_name varchar(180);
alter table habitflow.clients add column if not exists billing_responsible_name varchar(160);
alter table habitflow.clients add column if not exists billing_email varchar(200);
alter table habitflow.clients add column if not exists billing_phone varchar(40);
alter table habitflow.users add column if not exists client_id uuid null references habitflow.clients(id);

alter table habitflow.clients drop constraint if exists ck_habitflow_clients_person_type;
alter table habitflow.clients add constraint ck_habitflow_clients_person_type check (person_type in ('NaturalPerson','LegalPerson'));
alter table habitflow.clients drop constraint if exists ck_habitflow_clients_document_type;
alter table habitflow.clients add constraint ck_habitflow_clients_document_type check (document_type in ('CPF','CNPJ'));
alter table habitflow.clients drop constraint if exists ck_habitflow_clients_person_document_match;
alter table habitflow.clients add constraint ck_habitflow_clients_person_document_match check ((person_type = 'NaturalPerson' and document_type = 'CPF') or (person_type = 'LegalPerson' and document_type = 'CNPJ'));

alter table habitflow.users drop constraint if exists ck_habitflow_users_role;
alter table habitflow.users add constraint ck_habitflow_users_role check (role in ('User','Admin','SuperAdmin'));

update habitflow.clients set document_normalized = regexp_replace(coalesce(document_raw, document, ''), '\D', '', 'g') where document_normalized is null and coalesce(document_raw, document) is not null;
create unique index if not exists ux_habitflow_clients_document_normalized_not_null on habitflow.clients(document_normalized) where document_normalized is not null and btrim(document_normalized) <> '';
create index if not exists ix_habitflow_clients_document_normalized on habitflow.clients(document_normalized);
create index if not exists ix_habitflow_clients_person_type on habitflow.clients(person_type);
create index if not exists ix_habitflow_clients_document_type on habitflow.clients(document_type);

insert into habitflow.schema_migrations(id, name, applied_at) values ('030','client_registration_cpf_cnpj_real_flow', now()) on conflict (id) do nothing;
