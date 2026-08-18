# Inventário do HEAD v6.14.3

Inventário executado em 2026-08-18 (UTC).

- Branch inicial: `work`
- SHA inicial: `37af819f3b2722d15757d77061f1272f5c748e61`
- Árvore inicial: limpa.

## Contratos confirmados

- [x] `.github/workflows/v6138-release-gate.yml`
- [x] job `runtime-smoke-authenticated`
- [x] `scripts/validation/smoke-authenticated-routes.ps1`
- [x] `scripts/dev/provision-dev-user.ps1`
- [x] `scripts/dev/seed-demo-data.ps1`
- [x] `scripts/validation/validate-postgres-migrations.ps1`
- [x] `HabitTemplateProjection`
- [x] `HabitReminderRow`
- [x] `_HabitStatusBadge`
- [x] `WeeklyReview`
- [x] `AdaptiveHabitService`

Os comandos usados foram `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, `git log -30 --oneline` e buscas `rg` no workflow e em `src`.
