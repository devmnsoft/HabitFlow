-- Seed mínimo de produção v6.1: planos públicos e regras internas. Não cria usuários reais nem senhas.
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

-- v6.3: catálogo de marcos e benefícios é semeado idempotentemente pelas migrations 033 e 037.
