create table if not exists habitflow.system_settings(key varchar(100) primary key,value jsonb not null,updated_at timestamp not null,updated_by uuid null);
