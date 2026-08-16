# Auditoria de materialização Dapper — v6.13.3

## Método

Foi executado `rg -n "QueryAsync<|QuerySingleOrDefaultAsync<|QueryFirstOrDefaultAsync<|select \\*|select [a-zA-Z]+\\.\\*" src/HabitFlow.Infrastructure`. A classificação abaixo é estática; execução real ficou bloqueada.

## Resultado prioritário

| Área | Risco | Evidência/decisão |
|---|---|---|
| Reminders | Baixo após hotfix | `HabitReminderRow`, aliases C# completos, array inteiro tipado e conversão UTC explícita. |
| Habits | Baixo após hardening anterior | Queries centrais já usam rows/aliases em vez de `select *` para o agregado principal. |
| Goals/progresso | Médio | Existem materializações de records com projeções explícitas; requer confirmação runtime, mas não foi encontrado `select *` na rota central. |
| Templates/Library | Médio | Projeções `Columns` explícitas; records continuam sensíveis a evolução de schema, sem sinal de incompatibilidade comprovada. |
| Admin operacional | Alto fora do escopo central | `select *` para resultados `object`/administrativos em `AdminOperationalRepositories`; é dynamic administrativo documentado e não foi reescrito nesta versão. |

Nenhum novo risco alto foi identificado estaticamente nas rotas centrais prioritárias. Isso **não equivale** a materialização validada contra PostgreSQL.
