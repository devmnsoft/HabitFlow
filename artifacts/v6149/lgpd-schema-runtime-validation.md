# Validação runtime do schema LGPD

**Status: não executada — P0 pendente.**

O container não possui `pwsh` nem `psql`, e nenhuma connection string local foi fornecida. Não foi fabricada evidência de banco. O validador v6.14.9 foi endurecido para conferir as quatro colunas obrigatórias de `user_privacy_consents` (tipo e nulabilidade) e o trigger `trg_audit_privacy_request`, além das tabelas já verificadas.

Reexecutar no Windows real o comando solicitado com `HABITFLOW_LOCAL_CONNECTION` definido somente na sessão.
