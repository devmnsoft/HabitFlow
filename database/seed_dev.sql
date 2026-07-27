-- HabitFlow - seed de desenvolvimento seguro
-- Execute somente em Development: psql -U postgres -d habitflow -f database/seed_dev.sql
-- Credencial dev: admin@habitflow.local / Admin@123

insert into habitflow.users (id, name, email, password_hash, role, account_status, risk_status, plan, plan_status, onboarding_completed, created_at, updated_at)
values
('00000000-0000-0000-0000-000000000001','Admin Local','admin@habitflow.local','$2a$11$C6UzMDM.H6dfI/f/IKcEeO6UAbPpsQKHBqNDpOoFBqBa6hG7vMA9G','Admin','Active','Normal','Premium','Active',true,now(),now())
on conflict (email) do update set
  name = excluded.name,
  password_hash = excluded.password_hash,
  role = excluded.role,
  account_status = 'Active',
  plan = 'Premium',
  plan_status = 'Active',
  updated_at = now();

insert into habitflow.habits (id, user_id, name, color, category, created_at, updated_at)
values
('10000000-0000-0000-0000-000000000001','00000000-0000-0000-0000-000000000001','Beber água','#2563eb','Saúde',now(),now()),
('10000000-0000-0000-0000-000000000002','00000000-0000-0000-0000-000000000001','Caminhada leve','#10b981','Atividade física',now(),now())
on conflict (id) do nothing;

insert into habitflow.system_settings (key, value, updated_at)
values ('dev_seed', '{"admin":"admin@habitflow.local","password":"Admin@123","developmentOnly":true}'::jsonb, now())
on conflict (key) do update set value = excluded.value, updated_at = now();

-- v6.1 dev seed additions (non-production demo data)
insert into habitflow.users(id,name,email,password_hash,role,account_status,plan,plan_status,created_at,updated_at)
values(gen_random_uuid(),'SuperAdmin Dev','superadmin.dev@habitflow.local','$2a$11$devplaceholderhashdonotuseinproduction','SuperAdmin','Active','Free','Active',now(),now())
on conflict (email) do update set role='SuperAdmin', updated_at=now();
insert into habitflow.billing_communication_rules(code,name,trigger_type,days_offset,channel,title,message_template)
values ('due_minus_3','Aviso 3 dias antes','BeforeDue',-3,'Internal','Fatura vence em breve','Sua fatura vence em 3 dias.'),('due_today','Aviso no vencimento','DueToday',0,'Internal','Fatura vence hoje','Sua fatura vence hoje.'),('overdue_plus_2','Aviso atraso 2 dias','AfterDue',2,'Internal','Fatura em atraso','Regularize sua fatura.'),('overdue_plus_5','Aviso atraso 5 dias','AfterDue',5,'Internal','Regularize sua fatura','Benefícios pagos podem estar suspensos.')
on conflict (code) do nothing;

-- v6.1.2 keeps SuperAdmin global without client_id and client Admin/User with tenant binding.

-- v6.3: catálogo de marcos e benefícios é semeado idempotentemente pelas migrations 033 e 037.
