# Auditoria de runtime v6.6.1

## Pré-voo (2026-07-28 UTC)

* Commit inicial: `1c65cdd` (`Merge pull request #66 ...`). Branch criada: `fix/dapper-contracts-navigation-resilience-v661`.
* `git status --short --branch`: branch inicial `work`, árvore limpa.
* `git log -10 --oneline`: executado; topo `1c65cdd`.
* `dotnet --info`: **não executável**, `/bin/bash: dotnet: command not found` (exit 127).
* Como o primeiro comando da cadeia falhou, `dotnet clean`, `dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release` e `dotnet format --verify-no-changes` não foram executados no pré-voo. Eles permanecem obrigatórios no CI .NET 10.
* `psql --version`: **não executável**, `/bin/bash: psql: command not found` (exit 127).

## Incidente e correção

Erro real fornecido: `InvalidOperationException` ao materializar `ClientPlanAccess`. A rota observada pertence à construção de layout autenticado; o ambiente entregue não contém conexão/credencial para reproduzir uma rota concreta, portanto nenhuma rota ou `client_id` real foi inventado. Identificador de exemplo em logs é sempre mascarado (`xxxxxxxx…`).

Query anterior: `select id client_id, contracted_plan_code, effective_plan_code, benefits_status, grace_period_until from habitflow.clients where id=@clientId`. `habitflow.clients.grace_period_until` é PostgreSQL `date`, retornado pelo Npgsql como `DateOnly`; o contrato anterior era `DateTime?`. A correção usa `DateOnly?`, aliases citados, `coalesce`, Row DTO mutável com construtor padrão e mapping explícito. Não há migration: o schema já representa corretamente uma data civil.

## Auditoria e resultados

Foram localizadas 77 chamadas Dapper na infraestrutura e priorizados os agregados solicitados. A matriz e a política de projeções estão nos documentos desta versão. Testes unitários de contrato, navegação básica/falha e data civil foram adicionados, mas não puderam ser executados neste container sem SDK. Testes PostgreSQL também não foram executados: `HABITFLOW_TEST_CONNECTION_STRING` não está definida e `psql` não está instalado. `git diff --check` foi executado sem erros antes da documentação; a validação final está registrada no relatório da entrega.

## Verificação final local

Em nova execução individual, `dotnet clean`, `dotnet restore`, `dotnet build -c Release`, `dotnet test -c Release`, `dotnet format --verify-no-changes` e `dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj -c Release -o /tmp/habitflow-v661-publish` retornaram exit 127 (`dotnet` ausente). Portanto nenhum deles é declarado aprovado localmente. `psql --version` também permaneceu indisponível. `git diff --check` e a guarda de extensões de secrets/binários foram executados separadamente antes do commit.
