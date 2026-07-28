alter table habitflow.schema_migrations add column if not exists filename varchar(260);
alter table habitflow.schema_migrations add column if not exists app_version varchar(80);
alter table habitflow.schema_migrations alter column checksum type varchar(64);

