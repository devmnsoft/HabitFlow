# Análise do release gate

Não houve run remoto acessível. A tabela registra todos os jobs, sem tratá-los como aprovados.

| Job | Status | Etapa com falha/bloqueio | Log relevante | Artifact real | Correção necessária |
|---|---|---|---|---|---|
| `dotnet-build-publish` | Não executado | Dispatch bloqueado | `gh`: sem autenticação | Nenhum | Disponibilizar remote e autenticação GitHub |
| `frontend-security` | Não executado remotamente | Dispatch bloqueado | `gh`: sem autenticação | Nenhum | Executar Actions; checks locais passaram |
| `postgres-migrations` | Não executado | Dispatch bloqueado | Sem run; `psql` indisponível localmente | Nenhum | Executar o job com service PostgreSQL |
| `runtime-smoke-public` | Não executado | Dependências não executadas | Sem run | Nenhum | Executar após build e migrations |
| `runtime-smoke-authenticated` | Não executado | Dependências não executadas | Sem run | Nenhum | Executar após build e migrations |
| `artifact-summary` | Não executado | Nenhum run | Sem run | Nenhum | Executar o workflow completo |

Não há logs de CI ou conteúdo em `downloaded-run`, pois criar esses materiais seria fabricar evidência.
