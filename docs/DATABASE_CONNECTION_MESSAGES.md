# Mensagens de conexão com banco

- Pública: explica que o banco não pôde ser acessado sem stack trace.
- Development: orienta Host, Database, Username e Password em `appsettings.Development.local.json`.
- Log: usa códigos como `postgres.invalid_password`, `postgres.database_missing`, `postgres.unavailable`, `postgres.permission_denied` e `postgres.table_missing`.

## Correções
- 28P01: conferir Username/Password e testar com psql.
- 3D000: criar o banco `habitflow` e aplicar `database/script_completo.sql`.
- 42P01: aplicar `database/script_completo.sql` e validar `database/validate_schema_habitflow.sql`.
- Diagnóstico: acesse `/health/db` para JSON ou `/diagnostics/database` para painel visual em Development/Admin.
