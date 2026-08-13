# Validação de banco — v6.13.0

## Resultado

**Bloqueada pelo ambiente, não aprovada.** `psql --version` retornou `command not found` e Docker também não existe no container. Nenhuma migration foi executada contra banco novo ou existente, e nenhum rerun foi realizado.

## Escopo inspecionado estaticamente

O repositório contém migrations numeradas de `001` a `065`, incluindo contratos de hábitos, clientes, objetivos, templates/biblioteca, onboarding, notificações, lembretes, planos, subscriptions e privacidade. Esta constatação de arquivos não substitui a execução.

## Consultas pendentes

```sql
select count(*) from habitflow.habits where start_date is null;
select count(*) from habitflow.habits where client_id is null;
select count(*) from habitflow.user_goals where client_id is null;
select count(*) from habitflow.notifications where user_id is null;
```

Também permanecem pendentes: criação limpa, upgrade de banco existente, rerun idempotente, inspeção de `habitflow.schema_migrations`, constraints e tabelas efetivas.
