# Validação PostgreSQL v6.14.8

**Não executada neste contêiner:** `pwsh` e `psql` não estão instalados e não foi fornecida uma connection string local. O validador foi atualizado para exigir o registro 001–066 e incluir as duas relações LGPD na lista de tabelas obrigatórias.

## Comando pendente no Windows

```powershell
.\scripts\validation\validate-postgres-migrations.ps1 `
  -ConnectionString $env:HABITFLOW_LOCAL_CONNECTION `
  -TemporaryDatabase habitflow_v6148_fresh `
  -ReportPath artifacts\v6148\postgres-migrations-validation.md
```
