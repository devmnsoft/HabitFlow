# Auditoria inicial v6.8.2

Data: 29/07/2026 UTC. Revisão inicial local: `957d257`.

Branch de trabalho: `feature/v682-canonical-metrics-goals-reports`.

## Pré-requisitos

Foram encontrados `CompleteHabitUseCase`, `UndoHabitCompletionUseCase`,
`ProgressSnapshotService`, `TodayDashboardService`, `HabitOccurrenceService`,
`ConsistencyService`, `ProgressCalendarService`, `UserTimeZoneService`,
`GoalService`, `IUserGoalRepository`, `goal_habits`, `milestones` e
`user_milestones`. Portanto, a condição de bloqueio por ausência de componente
da v6.8.1 não foi acionada.

## Ambiente e resultados reais

- `git status --short`, `git branch --show-current` e `git log -15 --oneline`: exit code 0.
- `git fetch --all --prune`: exit code 0, mas não buscou referências porque o checkout não possui remote configurado.
- `node --version`: exit code 0, versão `v24.15.0`.
- `npm --version`: exit code 0, versão `11.4.2`, com aviso sobre a configuração `http-proxy`.
- `dotnet --info`: exit code 127; primeiro erro real: `dotnet: command not found`.
- `psql --version`: exit code 127; `psql: command not found`.
- `dotnet clean`, restore, quatro builds por camada, build da solução, testes e
  publish foram invocados individualmente e todos terminaram com exit code 127,
  como erros em cascata da ausência do executável `dotnet`.
- `pwsh -File scripts/qa/check-seed-required-ids.ps1`: exit code 127; PowerShell não está instalado.
- migrations/seeds em PostgreSQL, WebApplicationFactory e Playwright não foram
  executados: não há cliente/servidor PostgreSQL, SDK .NET nem credenciais de
  teste disponibilizados. Nenhum workflow remoto foi executado neste checkout.

A branch foi criada a partir do HEAD mais recente disponibilizado no checkout.
Não foi possível confirmar equivalência com a `main` remota. Este documento não
afirma CI verde, validação PostgreSQL, build, testes ou publish bem-sucedidos sem
execução real.
