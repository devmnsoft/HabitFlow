# Inventário do HEAD e ambiente — v6.13.8

- SHA inicial: `41c89b3d6e5e66cffc0a5b3dd125086c5bc31f73` (merge do PR #142).
- Branch inicial: `work`; branch de entrega: `feature/v6138-real-ci-release-gate-windows-postgres-smoke-evidence`.
- Conteúdo confirmado: scripts v6.13.7, `HabitTemplateProjection`, `HabitReminderRow`, `_HabitStatusBadge`, `WeeklyReview` e `AdaptiveHabitService`.
- Host Codex: Ubuntu 24.04.4 LTS, kernel 6.18.35, x86_64.
- .NET, PowerShell, PostgreSQL/psql e navegador: indisponíveis no host Codex.
- Node: v24.15.0; npm: 11.4.2.
- GitHub CLI: 2.96.0, sem autenticação; repositório sem remote configurado.
- Ambiente real preparado: GitHub-hosted `ubuntu-latest`, .NET 10, PowerShell do runner, PostgreSQL 17 service e Node 22, definido no workflow versionado.
- Limitação objetiva: o host não consegue publicar/disparar o workflow; resultados de execução devem vir do run criado pelo PR/push.
