create table if not exists habitflow.client_onboarding (
  id uuid primary key,
  client_id uuid not null references habitflow.clients(id),
  company_data_completed boolean not null default false,
  billing_data_completed boolean not null default false,
  first_user_invited boolean not null default false,
  first_habit_created boolean not null default false,
  plan_reviewed boolean not null default false,
  completed boolean not null default false,
  completed_at timestamp null,
  created_at timestamp not null default now(),
  updated_at timestamp not null default now(),
  unique(client_id)
);

create table if not exists habitflow.billing_communication_rules (
  id uuid primary key,
  code varchar(80) not null unique,
  name varchar(160) not null,
  trigger_type varchar(80) not null,
  days_offset integer not null default 0,
  channel varchar(80) not null,
  title varchar(180) not null,
  message_template text not null,
  is_active boolean not null default true,
  created_at timestamp not null default now(),
  updated_at timestamp not null default now()
);

create table if not exists habitflow.client_communications (
  id uuid primary key,
  client_id uuid not null references habitflow.clients(id),
  user_id uuid null references habitflow.users(id),
  invoice_id uuid null,
  type varchar(80) not null,
  channel varchar(80) not null,
  title varchar(180) not null,
  message text not null,
  status varchar(80) not null default 'Created',
  sent_at timestamp null,
  read_at timestamp null,
  created_at timestamp not null default now()
);
create unique index if not exists ux_client_communications_no_duplicate_billing on habitflow.client_communications(client_id, invoice_id, type, channel) where invoice_id is not null and status <> 'Canceled';

create table if not exists habitflow.job_execution_logs (
  id uuid primary key,
  job_name varchar(120) not null,
  status varchar(80) not null,
  started_at timestamp not null,
  finished_at timestamp null,
  duration_ms bigint null,
  processed_count integer not null default 0,
  error_message text null,
  created_at timestamp not null default now()
);

alter table habitflow.support_tickets add column if not exists client_id uuid null references habitflow.clients(id);
alter table habitflow.support_tickets add column if not exists assigned_to_user_id uuid null references habitflow.users(id);
alter table habitflow.support_tickets add column if not exists priority varchar(40) not null default 'Normal';
alter table habitflow.support_tickets add column if not exists sla_due_at timestamp null;
alter table habitflow.support_tickets add column if not exists category varchar(80) not null default 'Dúvida';
alter table habitflow.support_tickets add column if not exists source varchar(80) not null default 'Web';

insert into habitflow.billing_communication_rules(id,code,name,trigger_type,days_offset,channel,title,message_template) values
(gen_random_uuid(),'due_minus_3','Aviso 3 dias antes','BeforeDueDate',-3,'Internal','Seu plano vence em breve','Identificamos uma cobrança com vencimento em {dueDate}. Regularize para manter seus benefícios Premium ativos.'),
(gen_random_uuid(),'due_today','Aviso no vencimento','OnDueDate',0,'Internal','Pagamento pendente','Seu pagamento está pendente. Você ainda pode usar o HabitFlow, mas os benefícios Premium podem ser suspensos após o período de tolerância.'),
(gen_random_uuid(),'due_plus_2','Aviso 2 dias após','AfterDueDate',2,'Internal','Pagamento pendente','Seu pagamento está pendente. Você ainda pode usar o HabitFlow, mas os benefícios Premium podem ser suspensos após o período de tolerância.'),
(gen_random_uuid(),'due_plus_5','Aviso 5 dias após','AfterDueDate',5,'Internal','Pagamento pendente','Seu pagamento está pendente. Você ainda pode usar o HabitFlow, mas os benefícios Premium podem ser suspensos após o período de tolerância.'),
(gen_random_uuid(),'benefits_blocked','Benefícios bloqueados','BenefitsBlocked',0,'Internal','Benefícios Premium suspensos','Os recursos pagos foram temporariamente suspensos. A área gratuita continua disponível.'),
(gen_random_uuid(),'payment_approved','Pagamento aprovado','PaymentApproved',0,'Internal','Pagamento confirmado','Seus benefícios pagos estão ativos novamente.'),
(gen_random_uuid(),'benefits_released','Benefícios liberados','BenefitsReleased',0,'Internal','Benefícios liberados','Seus benefícios pagos estão ativos novamente.'),
(gen_random_uuid(),'engagement_first_habit','Engajamento primeiro hábito','AfterDueDate',0,'Internal','Comece com seu primeiro hábito','Você ainda não criou hábitos. Use a biblioteca para começar em poucos segundos.'),
(gen_random_uuid(),'engagement_free_limit','Engajamento limite Free','AfterDueDate',0,'Internal','Você chegou ao limite gratuito','Considere revisar os hábitos arquivados ou conhecer o Premium.')
on conflict(code) do nothing;
