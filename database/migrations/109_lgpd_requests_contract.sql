-- 109_lgpd_requests_contract.sql
-- Alinha as constraints de habitflow.lgpd_requests com os valores canônicos do domínio
-- (LgpdRequestTypes/LgpdRequestStatuses). Sem esta migração, a criação de solicitações
-- LGPD pelo aplicativo falhava com 23514 (check constraint) porque o domínio usa
-- valores em português ("Aberta", "Em análise", ...) e tipos adicionais
-- ("Deletion", "Anonymization", "Rectification") ausentes da constraint original.

do $$
begin
    if exists (select 1 from pg_constraint where conname = 'ck_habitflow_lgpd_requests_status') then
        alter table habitflow.lgpd_requests drop constraint ck_habitflow_lgpd_requests_status;
    end if;

    if exists (select 1 from pg_constraint where conname = 'ck_habitflow_lgpd_requests_type') then
        alter table habitflow.lgpd_requests drop constraint ck_habitflow_lgpd_requests_type;
    end if;
end $$;

alter table habitflow.lgpd_requests
    add constraint ck_habitflow_lgpd_requests_status
    check (status in ('Aberta', 'Em análise', 'Aguardando confirmação', 'Processada', 'Recusada', 'Cancelada',
                      'Requested', 'InReview', 'Processing', 'Completed', 'Rejected', 'Canceled'));

alter table habitflow.lgpd_requests
    add constraint ck_habitflow_lgpd_requests_type
    check (type in ('Export', 'Deletion', 'Anonymization', 'Rectification',
                    'Delete', 'Anonymize'));
