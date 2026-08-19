# Contrato do relatório PostgreSQL no CI

O job `postgres-migrations` gera, durante a execução real, o relatório homônimo com maior migration detectada, totais esperado e aplicado, segundo run idempotente e resultado. A validação inclui as 11 tabelas essenciais, `trg_audit_privacy_request`, `ix_privacy_request_events_request` e a FK de `privacy_request_events` para `lgpd_requests`.

A senha de banco do service container é descartável, usada apenas dentro do GitHub Actions e não é segredo de produção. Este documento descreve o contrato; não simula resultado de uma execução.
