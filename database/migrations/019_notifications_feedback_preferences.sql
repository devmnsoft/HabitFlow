create table if not exists habitflow.notifications(
  id uuid primary key,
  user_id uuid not null references habitflow.users(id) on delete cascade,
  type varchar(80) not null,
  title varchar(160) not null,
  message text not null,
  severity varchar(40) not null default 'Info',
  is_read boolean not null default false,
  action_url text null,
  related_entity_type varchar(80) null,
  related_entity_id uuid null,
  created_at timestamp not null default now(),
  read_at timestamp null
);
alter table habitflow.notifications add column if not exists severity varchar(40) not null default 'Info';
alter table habitflow.notifications add column if not exists action_url text null;
alter table habitflow.notifications add column if not exists related_entity_type varchar(80) null;
alter table habitflow.notifications add column if not exists related_entity_id uuid null;
create index if not exists ix_notifications_user_id on habitflow.notifications(user_id);
create index if not exists ix_notifications_unread on habitflow.notifications(user_id, is_read, created_at desc);
