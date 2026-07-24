-- Seed mínimo de produção v6.1: planos públicos e regras internas. Não cria usuários reais nem senhas.
insert into habitflow.billing_communication_rules(code,name,trigger_type,days_offset,channel,title,message_template)
values ('due_minus_3','Aviso 3 dias antes','BeforeDue',-3,'Internal','Fatura vence em breve','Sua fatura vence em 3 dias.'),('due_today','Aviso no vencimento','DueToday',0,'Internal','Fatura vence hoje','Sua fatura vence hoje.'),('overdue_plus_2','Aviso atraso 2 dias','AfterDue',2,'Internal','Fatura em atraso','Regularize sua fatura.'),('overdue_plus_5','Aviso atraso 5 dias','AfterDue',5,'Internal','Regularize sua fatura','Benefícios pagos podem estar suspensos.')
on conflict (code) do nothing;
