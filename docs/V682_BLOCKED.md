# v6.8.2 — evolução bloqueada

Data da auditoria: 28/07/2026 (UTC)

HEAD local auditado: `86554d3a9e937f2aa738a65958663626e44736a9`

Branch criada: `feature/v682-goals-milestones-reports`

## Decisão

A implementação de **v6.8.2-AutomaticGoals-Milestones-ConsistentReports** não foi iniciada. A condição obrigatória de início não foi atendida: a revisão local disponível não contém a v6.8.1 com todos os contratos e comportamentos exigidos.

Nenhum pré-requisito ausente foi reimplementado parcialmente e nenhum arquivo de produto, migration ou teste foi alterado.

## Pré-requisitos ausentes

| Pré-requisito obrigatório | Evidência da auditoria |
| --- | --- |
| `CompleteHabitUseCase` | Símbolo e arquivo não encontrados. A conclusão ainda é coordenada diretamente por `HabitsController`/`HabitService`. |
| `UndoHabitCompletionUseCase` | Símbolo e arquivo não encontrados. O desfazer ainda é coordenado diretamente por `HabitsController`/`HabitService`. |
| `ProgressSnapshotService` | Símbolo e arquivo não encontrados. Existe `ProgressCalendarService`, mas isso não satisfaz o contrato exigido. |
| `TodayDashboardService` | Símbolo e arquivo não encontrados. |
| Dashboard com métricas reais | Ausente. `DashboardController` monta `DashboardDto` com métricas `0`, e a View usa esse modelo. |
| Conclusão Ajax sem valores fixos | Ausente. As respostas de concluir e desfazer retornam `dailyProgress = 0`, `currentStreak = 0` e `nextHabit = null`. |
| Data local por `UserTimeZoneService` em toda a conclusão/Dashboard | Parcial e, portanto, insuficiente. O serviço existe e é usado no calendário/relatórios, mas o Dashboard renderiza a data com `DateTime.UtcNow` e o fluxo de conclusão não usa o serviço. |
| Conclusão idempotente e transacional | Não foi encontrada evidência de um caso de uso que reúna conclusão/desfazer, idempotência e transação. As ações atuais chamam o serviço e depois consultam a lista, fora de uma unidade transacional canônica. |

## Evidências adicionais

- A documentação existente registra explicitamente que a sincronização transacional após concluir/desfazer permanecia no backlog.
- O repositório local não possui remote Git configurado nem uma referência local `main`; apenas a branch `work` estava disponível no HEAD acima. Por isso não foi possível buscar ou confirmar a `main` remota mais recente. A branch solicitada foi criada a partir da única revisão local fornecida, sem declarar que ela equivale à `main` mais recente.
- `dotnet` não está instalado/disponível no `PATH` deste ambiente.
- `psql` não está instalado/disponível no `PATH` deste ambiente.

## Comandos executados

```text
git status --short
git branch --show-current
git log -12 --oneline
git remote -v
git fetch origin main --prune
git branch -a -vv
git config --get-regexp '^remote\.'
git show-ref --heads
find src -type f (filtros Dashboard/Completion/Snapshot)
rg (símbolos e evidências de Dashboard, Ajax, data local, idempotência e transação)
dotnet --info
psql --version
```

## Comandos não executados

Por causa do bloqueio obrigatório — e também da ausência do SDK .NET — não foram executados:

- `dotnet clean HabitFlow.sln`;
- `dotnet restore HabitFlow.sln`;
- builds por camada e build da solução;
- `dotnet test`;
- `dotnet publish`;
- migrations e testes PostgreSQL;
- testes funcionais e Playwright;
- scanner de secrets;
- qualquer implementação, migration ou alteração funcional da v6.8.2.

## Como desbloquear

Disponibilizar uma `main` que já incorpore integralmente a v6.8.1 e os oito pré-requisitos da condição de início, configurar o remote do repositório e fornecer o SDK .NET compatível. Depois disso, a branch deve ser recriada ou atualizada a partir dessa `main`, e o pré-voo completo deve ser executado antes de iniciar a v6.8.2.
