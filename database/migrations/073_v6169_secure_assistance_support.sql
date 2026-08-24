-- HabitFlow v6.16.9: secure assistant, support contact and tenant-isolated tickets.
begin;
create table if not exists habitflow.assistant_conversations (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 created_at timestamptz not null default now(), updated_at timestamptz not null default now());
create index if not exists ix_assistant_conversations_owner on habitflow.assistant_conversations(client_id,user_id,updated_at desc);
create table if not exists habitflow.assistant_messages (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 conversation_id uuid not null references habitflow.assistant_conversations(id) on delete cascade,
 role varchar(20) not null check(role in ('user','assistant')), message text not null, sanitized_message text not null,
 safety_status varchar(30) not null, provider varchar(40) not null, created_at timestamptz not null default now(), correlation_id varchar(100) not null);
create index if not exists ix_assistant_messages_owner on habitflow.assistant_messages(client_id,user_id,conversation_id,created_at);
create table if not exists habitflow.assistant_feedback (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 message_id uuid not null references habitflow.assistant_messages(id) on delete cascade, helpful boolean not null, comment varchar(500), created_at timestamptz not null default now());
create index if not exists ix_assistant_feedback_owner on habitflow.assistant_feedback(client_id,user_id,created_at desc);

create table if not exists habitflow.support_settings (
 id uuid primary key, company_name varchar(120) not null, company_document varchar(30) not null,
 support_email varchar(254) not null, whatsapp_phone varchar(20), default_message varchar(500) not null,
 business_hours varchar(160) not null, is_active boolean not null default true, button_text varchar(80) not null, updated_at timestamptz not null default now());
insert into habitflow.support_settings(id,company_name,company_document,support_email,whatsapp_phone,default_message,business_hours,is_active,button_text)
values('61690000-0000-0000-0000-000000000001','MNSOFT','18.160.057/0001-13','comercial@mnsoft.com.br',null,'Olá! Preciso de ajuda com o HabitFlow.','Segunda a sexta, 9h às 18h',true,'Falar com a MNSOFT') on conflict(id) do nothing;

create table if not exists habitflow.support_tickets_v2 (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), user_id uuid not null references habitflow.users(id),
 protocol varchar(40) not null unique, category varchar(30) not null check(category in ('Usage','Technical','Subscription','Report','Notifications','Suggestion','Other')),
 status varchar(20) not null check(status in ('Open','InAnalysis','Responded','Closed')), subject varchar(160) not null,
 description text not null, safe_context varchar(1000) not null, created_at timestamptz not null default now(), updated_at timestamptz not null default now(), closed_at timestamptz);
create index if not exists ix_support_tickets_v2_owner on habitflow.support_tickets_v2(client_id,user_id,status,updated_at desc);
create table if not exists habitflow.support_ticket_messages_v2 (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), ticket_id uuid not null references habitflow.support_tickets_v2(id) on delete cascade,
 user_id uuid not null references habitflow.users(id), is_staff boolean not null default false, message text not null, created_at timestamptz not null default now());
create index if not exists ix_support_ticket_messages_v2_tenant on habitflow.support_ticket_messages_v2(client_id,ticket_id,created_at);
create table if not exists habitflow.support_ticket_events (
 id uuid primary key, client_id uuid not null references habitflow.clients(id), ticket_id uuid not null references habitflow.support_tickets_v2(id) on delete cascade,
 actor_user_id uuid references habitflow.users(id), event_type varchar(40) not null, metadata jsonb not null default '{}', created_at timestamptz not null default now());
create index if not exists ix_support_ticket_events_tenant on habitflow.support_ticket_events(client_id,ticket_id,created_at);
commit;
