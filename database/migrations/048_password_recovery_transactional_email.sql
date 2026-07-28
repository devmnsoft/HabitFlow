-- Secure, single-use password recovery and asynchronous transactional email.
alter table habitflow.users add column if not exists session_version integer not null default 0;

create table if not exists habitflow.password_reset_tokens (
  id uuid primary key, user_id uuid not null references habitflow.users(id) on delete cascade,
  token_hash varchar(64) not null unique, expires_at timestamptz not null,
  used_at timestamptz null, revoked_at timestamptz null, created_at timestamptz not null,
  requested_ip_hash varchar(64) null, requested_user_agent_hash varchar(64) null,
  request_correlation_id varchar(100) null
);
create index if not exists ix_password_reset_tokens_user on habitflow.password_reset_tokens(user_id);
create index if not exists ix_password_reset_tokens_expires on habitflow.password_reset_tokens(expires_at);
create unique index if not exists ux_password_reset_tokens_active_user on habitflow.password_reset_tokens(user_id)
  where used_at is null and revoked_at is null;

create table if not exists habitflow.password_reset_requests (
  id uuid primary key, email_hash varchar(64) not null, ip_hash varchar(64) not null, created_at timestamptz not null
);
create index if not exists ix_password_reset_requests_email_time on habitflow.password_reset_requests(email_hash,created_at);
create index if not exists ix_password_reset_requests_ip_time on habitflow.password_reset_requests(ip_hash,created_at);

create table if not exists habitflow.transactional_email_outbox (
  id uuid primary key, client_id uuid null, user_id uuid null references habitflow.users(id) on delete set null,
  template_code varchar(80) not null, recipient varchar(254) not null, subject varchar(200) not null,
  payload_json jsonb not null, status varchar(20) not null check(status in ('Pending','Processing','Sent','Failed','DeadLetter')),
  idempotency_key varchar(160) not null unique, attempts integer not null default 0,
  next_attempt_at timestamptz not null, sent_at timestamptz null, last_error varchar(500) null,
  created_at timestamptz not null, updated_at timestamptz not null
);
create index if not exists ix_email_outbox_due on habitflow.transactional_email_outbox(status,next_attempt_at);
