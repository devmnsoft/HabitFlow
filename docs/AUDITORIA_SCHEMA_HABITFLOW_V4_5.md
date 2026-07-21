# Auditoria de schema HabitFlow v4.5

## SQL encontrado sem schema

As buscas obrigatórias foram executadas em `src`, `database` e `tests`. Não foram encontrados acessos Dapper ou scripts de tabelas principais com `from users`, `from habits`, `insert into users`, `update users`, `delete from users` ou `create table users` sem schema.

## Correções aplicadas

- `database/script_completo.sql` recebeu cabeçalho de produção, constraints e índices com prefixo `habitflow`.
- `database/script_completo_dev.sql` recebeu aviso explícito de uso exclusivo em desenvolvimento.
- Criada migration `015_schema_hardening.sql` para garantir schema, alertar conflitos em `public` e padronizar nomes quando seguro.
- Criado `database/validate_schema_habitflow.sql` e wrappers Windows/BAT.
- Criado `DbNames` para centralizar nomes qualificados.

## Pendências

- A validação com `psql` depende de PostgreSQL disponível no ambiente.
- Tabelas HabitFlow legadas em `public` não são movidas ou apagadas automaticamente; devem ser avaliadas manualmente.

## Validação final

Executar:

```powershell
scripts/database/check-sql-schema-prefix.ps1
scripts/database/validate-schema-habitflow.ps1 -Database habitflow
```
