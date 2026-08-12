# Auditoria do contrato Dapper — v6.12.3

| Repository/query | Problema | Entidade | Correção | Teste |
|---|---|---|---|---|
| `HabitRepository` — consultas de hábitos | A projeção estava duplicada e podia divergir entre consumidores. | `Habit` | Projeção canônica `HabitSql.Columns`, sem a coluna legada `visibility`. | `HabitRepositoryContractTests`, `DapperProjectionTests` |
| `UserGoalRepository.ListLinkedHabitsAsync` | `select h.*` incluía colunas fora do construtor posicional e impedia o Dapper de materializar `Habit`. | `Habit` | Substituído por `HabitSql.AliasedColumns`, mantendo escopo de cliente e usuário. | `UserGoalRepositoryContractTests` |
| `HabitRepository.CreateAsync` | `@StartDate` podia receber `null`, embora a coluna fosse `NOT NULL`. | `Habit` | O application service agora normaliza a data antes do INSERT; a migration faz backfill defensivo. | `HabitRepositoryContractTests` |
| Updates de hábito e link/unlink de objetivo | Risco de alteração cross-tenant se os predicados fossem removidos. | `Habit`, `UserGoal` | Mantidos predicados simultâneos de `client_id` e `user_id` e adicionados testes de regressão. | `CrudSmokeRepositoryTests` |

As ocorrências restantes de `select *` na infraestrutura retornam objetos administrativos/dinâmicos, não `Habit` nem outra entidade de domínio materializada neste escopo. Devem ser convertidas para DTOs numa revisão administrativa dedicada.
