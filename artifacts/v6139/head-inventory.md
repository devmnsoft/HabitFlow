# Inventário do HEAD — v6.13.9

- Data/ambiente: 2026-08-17, container Linux em `/workspace/HabitFlow`.
- SHA inicial: `7f845daa7b769abda6ab198c584d87bde65a0849`.
- PR anterior: merge do PR #143 (`7f845da`).
- Workflow encontrado: `.github/workflows/v6138-release-gate.yml`.
- Jobs encontrados: `dotnet-build-publish`, `frontend-security`, `postgres-migrations`, `runtime-smoke-public` e `artifact-summary`.
- Componentes confirmados: `HabitTemplateProjection`, `HabitReminderRow`, `_HabitStatusBadge`, `WeeklyReview`, `AdaptiveHabitService`, helper de smoke autenticado e `-ReportPath` no validador PostgreSQL.
- Evidências anteriores: diretório `artifacts/v6138`, cuja conclusão era não aprovada e execução externa pendente.
- P0s iniciais: execução real do workflow, build/publish, PostgreSQL, smoke público/autenticado, jornada, planos e matriz mobile.
- Limitações: `dotnet`, `pwsh`, Docker e navegador não estão instalados; GitHub CLI não está autenticado e o acesso HTTPS à API do GitHub foi recusado pelo proxy.
