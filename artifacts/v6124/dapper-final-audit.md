# v6.12.4 — auditoria final Dapper

Data: 2026-08-12 (UTC)

## Comandos executados

```bash
rg -ni 'select\s+(h\.)?\*' src tests database
rg -n 'Query(Async|SingleOrDefaultAsync)<Habit' src
rg -ni 'ExecuteAsync\s*\(\s*[@$]?"(update|delete)' src
rg -n '\[HttpPost|ValidateAntiForgeryToken' src/HabitFlow.Web/Controllers
```

## Resultado

- As consultas que materializam `Habit` usam `HabitSql.Columns` ou `HabitSql.AliasedColumns`; nenhum `select *`/`select h.*` foi encontrado nesse caminho.
- `UserGoalRepository.ListLinkedHabitsAsync` usa a projeção canônica e restringe objetivo, hábito e vínculo por tenant/usuário.
- As mutações críticas de hábitos, objetivos, lembretes e notificações possuem filtros de usuário e tenant (notificações validam o tenant pelo join com `users`).
- Os POSTs dos módulos solicitados possuem antiforgery. O webhook de pagamentos é a exceção técnica esperada: autenticação do provedor não usa token antiforgery de navegador.
- Ocorrências de `select *` remanescentes estão em repositórios operacionais administrativos que materializam `object`, não entidades posicionais de domínio. Elas foram registradas, mas não alteradas sem falha de runtime e sem um contrato tipado de destino.
- Foram encontradas APIs legadas que recebem somente `userId` (`ListByUserAsync`/`CountActiveByUserAsync`). Como o identificador de usuário é global e os chamadores não fornecem `clientId`, mudar a assinatura seria uma evolução ampla; as APIs CRUD atuais usam as variantes explicitamente tenant-scoped.

## Conclusão

A inspeção estática não encontrou regressão no contrato posicional de `Habit`. Este resultado não substitui materialização Dapper real, que não pôde ser executada sem .NET/PostgreSQL no ambiente.
