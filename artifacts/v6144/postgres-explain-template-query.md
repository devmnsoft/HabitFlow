# EXPLAIN da consulta de templates favoritos

## Estado nesta execução Linux

**Pendente por limitação do ambiente:** `dotnet`, `psql` e uma instância PostgreSQL não estão instalados/disponíveis no container. Portanto, não se declara que o `EXPLAIN` passou nem que o schema runtime foi inspecionado.

O runner Windows v6.14.4 agora consulta `information_schema.columns` para `suggested_days`, `tags`, `difficulty`, `suggested_reminder_time` e `published_at`, e executa `EXPLAIN` com `psql -v ON_ERROR_STOP=1`. Ao ser executado com a connection string local, ele substitui este arquivo com tipos reais e o plano retornado, sem persistir credenciais ou identificadores de usuários.

A definição versionada usa `suggested_days smallint[]` e `tags text[]`, coerente com `unnest(t.suggested_days)` e `array[]::text[]` na projeção.
