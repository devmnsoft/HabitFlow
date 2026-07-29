-- HabitFlow - seed de desenvolvimento seguro
-- Execute somente em Development: psql -U postgres -d habitflow -f database/seed_dev.sql
-- No user, SuperAdmin, fixed password, or credential is seeded. Create local
-- identities through the application or the documented secure admin command.

insert into habitflow.system_settings (key, value, updated_at)
values ('dev_seed', '{"developmentOnly":true,"credentialsSeeded":false}'::jsonb, now())
on conflict (key) do update set value = excluded.value, updated_at = now();

-- Super Administradores nunca são semeados. Use o comando administrativo seguro documentado.
insert into habitflow.billing_communication_rules(id,code,name,trigger_type,days_offset,channel,title,message_template)
values
(gen_random_uuid(),'due_minus_3','Aviso 3 dias antes','BeforeDueDate',-3,'Internal','Fatura vence em breve','Sua fatura vence em 3 dias.'),
(gen_random_uuid(),'due_today','Aviso no vencimento','OnDueDate',0,'Internal','Fatura vence hoje','Sua fatura vence hoje.'),
(gen_random_uuid(),'due_plus_2','Aviso 2 dias após','AfterDueDate',2,'Internal','Fatura em atraso','Regularize sua fatura.'),
(gen_random_uuid(),'due_plus_5','Aviso 5 dias após','AfterDueDate',5,'Internal','Regularize sua fatura','Benefícios pagos podem estar suspensos.')
on conflict (code) do update set
  name=excluded.name, trigger_type=excluded.trigger_type, days_offset=excluded.days_offset,
  channel=excluded.channel, title=excluded.title, message_template=excluded.message_template,
  is_active=true, updated_at=now();

-- v6.1.2 keeps SuperAdmin global without client_id and client Admin/User with tenant binding.

-- v6.3: catálogo de marcos e benefícios é semeado idempotentemente pelas migrations 033 e 037.
