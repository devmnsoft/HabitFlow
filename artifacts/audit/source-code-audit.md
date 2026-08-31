# HabitFlow v6.18.7 — auditoria geral do código-fonte

Data: 2026-08-31
Escopo: arquivos texto versionados em `src/`, `tests/`, `database/`, `scripts/` e configuração. Artefatos binários foram desconsiderados.

## Método

- Busca estática por bloqueios síncronos (`Task.Result`, `.Wait()` e `async void`), conversões frágeis, horário local, SQL interpolado e filtros de tenant.
- Inspeção dos registros de DI, contratos/implementações, controllers, serviços, repositórios Dapper, Razor, CSS, JavaScript, migrations e testes existentes.
- Execução dos comandos de restauração, build, testes, frontend, segurança e consistência disponíveis no ambiente; limitações estão registradas abaixo.

## Achados e correções

| Severidade | Arquivo | Causa / risco | Correção aplicada |
|---|---|---|---|
| Alta | `src/HabitFlow.Infrastructure/Repositories/SaaSOperationsRepositories.cs` | `UpdateStepAsync` interpolava um identificador recebido como `string` no SQL. Embora existisse uma allow-list, o padrão dificultava auditoria e poderia voltar a permitir injeção após uma manutenção incompleta. | Substituição por `switch` exaustivo com seis comandos SQL constantes; valores continuam parametrizados e entradas desconhecidas falham antes do acesso ao banco. |
| Média | `src/HabitFlow.Infrastructure/Repositories/SaaSOperationsRepositories.cs` | Reabrir um onboarding (`completed=false`) mantinha `completed_at`, produzindo estado contraditório e métricas administrativas incorretas. | A coluna agora é definida atomicamente com `case`: recebe `now()` ao concluir e `null` ao reabrir. |
| Média | `src/HabitFlow.Application/Observability/ApplicationEvents.cs` | O catálogo não continha os nove eventos operacionais mínimos da v6.18.7, incentivando mensagens livres e dificultando agregação. | Inclusão de `EventId`s estáveis e sem dados sensíveis para auditoria, validação, falhas funcionais, negação de tenant e health check. |
| Baixa | `tests/HabitFlow.Tests/SourceCodeAuditV6187Tests.cs` | Não havia regressão automatizada para o SQL dinâmico nem para o catálogo mínimo de eventos. | Testes de contrato verificam SQL fixo, limpeza do timestamp, rejeição explícita e nomes estruturados. |

## Verificações sem ocorrência confirmada

- Não foram encontrados `async void`, `.Wait()` ou uso de `Task.Result` no código de produção pesquisado.
- As consultas de hábitos, metas, progresso e comunicações inspecionadas preservam os filtros de `client_id`/`user_id` nos fluxos tenant-scoped.
- A interpolação remanescente em repositórios é usada principalmente para projeções/cláusulas constantes internas; valores externos permanecem parâmetros Dapper. O caso com identificador externo foi removido.
- Registros de aplicação e infraestrutura incluem os serviços e implementações usados pelos fluxos auditados.

## Validação e limitações reais

- O checkout fornecido não possui remoto Git `origin`; por isso `git fetch origin` e `git pull origin main` não puderam ser executados.
- O SDK `dotnet` não está instalado no ambiente; restore, build, test e publish .NET não puderam ser executados localmente.
- Os resultados finais dos comandos Node, segurança e Git são registrados na descrição da entrega/PR.

## Pendências

- Executar a suíte .NET e os testes de integração com PostgreSQL em agente que possua o SDK e banco de teste.
- Repetir fetch/pull e abrir a PR contra `main` quando o remoto do repositório estiver configurado.
- A suíte Playwright existe em `tests/HabitFlow.Playwright`, mas não pôde concluir porque o executável Chromium não está instalado no ambiente.
