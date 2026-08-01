# Auditoria inicial — v6.9.5B

Data da execução: 2026-08-01 (UTC). HEAD inicial: `018230cbb9f6a6373a4faec317c29cb63d904e73`.

## Git e inventário

- `git status --short`: árvore limpa no início.
- `git branch --show-current`: `work`; criada `feature/v695b-activation-journey-completion` a partir do HEAD que contém os merges dos PRs #80 a #85.
- `git log -20 --oneline`: confirmou os merges #80, #81, #82, #83, #84 e #85.
- `git fetch --all --prune`: executado; o checkout fornecido não possui remoto configurado, portanto não houve atualização remota.
- O inventário confirmou migrations até `052_library_v2_onboarding.sql`, metadados V2 de `HabitTemplate`, origem em `Habit`, favoritos, coleções, `user_onboarding_progress`, `GoalProgressEngine` e `MilestoneEvaluationService`.

## Ferramentas

- `dotnet --info`: não executável neste container (`dotnet: command not found`).
- `psql --version`: não executável neste container (`psql: command not found`).
- `node --version`: `v24.15.0`.
- `npm --version`: `11.4.2` (com aviso da configuração legada `http-proxy`).

## Restore e compilação

Os comandos de `dotnet clean`, `dotnet restore` e os builds separados solicitados não puderam ser executados porque o SDK .NET não está instalado no ambiente. Essa limitação é registrada como resultado real, sem declarar build aprovado. O CI permanece como autoridade para .NET 10, Razor, PostgreSQL e publish Release.

## Falhas iniciais confirmadas

- `HabitLibraryController` invocava extension methods de identidade sem o receptor `this`, impedindo compilação C#.
- O fluxo principal ainda chamava o adapter obsoleto `HabitLibraryService.AddTemplateToUserHabitsAsync` e aceitava apenas o nome.
- O cenário de upgrade recriava `schema_migrations` sem `IF NOT EXISTS`.
- Os pipes `psql | tee` não ativavam `pipefail`.
