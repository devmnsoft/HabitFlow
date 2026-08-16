# Inventário do HEAD — v6.13.3

- **SHA inicial:** `780b01b0a2853c734fede0183ade433858ba977b`
- **Branch de trabalho:** `feature/v6133-post-hotfix-runtime-validation-partial-dapper-stabilization`
- **PR anterior detectado:** PR #136, merge `780b01b`, contendo `a0c72a2`.
- **Hotfix confirmado:** `_HabitStatusBadge.cshtml`; referência `Partials/_HabitStatusBadge` em `_HabitCard.cshtml`; `HabitReminderRepository` com `HabitReminderRow`, aliases explícitos, conversão UTC e array tipado; relatórios em `artifacts/v6132-hotfix`.
- **Arquivos do hotfix:** repositório de lembretes, card/badge/CSS de hábitos e cinco relatórios v6132-hotfix.
- **Pendências herdadas:** build, publish, PostgreSQL, runtime autenticado, jornadas e viewports não possuíam execução real no ambiente anterior.
- **Ambiente observado (2026-08-15 UTC):** Ubuntu 24.04 x64, Node/npm disponíveis; SDK .NET, PowerShell, PostgreSQL e navegador indisponíveis. Instalação via `dotnet-install.sh` e APT foi bloqueada pelo proxy HTTP 403.
