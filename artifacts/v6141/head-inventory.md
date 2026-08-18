# Inventário do HEAD v6.14.1

- Data UTC: 2026-08-17.
- SHA inicial: `7f845daa7b769abda6ab198c584d87bde65a0849`.
- Branch de trabalho criada: `feature/v6141-run-existing-release-gate-fix-failures-release-candidate-closure`.
- Workflow existente confirmado: `.github/workflows/v6138-release-gate.yml` (`v6.13.8 release gate`, com `workflow_dispatch`).
- Scripts confirmados: validação PostgreSQL, smoke autenticado, provisionamento dev e seed demo.
- Componentes confirmados no código: `HabitTemplateProjection`, `HabitReminderRow`, `_HabitStatusBadge`, `WeeklyReview` e `AdaptiveHabitService`.
- P0s inicialmente pendentes de evidência real: build/publish Release, migrations, runtime, smoke público/autenticado, jornada MVP, regras de plano e mobile.
- Decisão: executar e corrigir o gate existente; não criar feature nem workflow paralelo.
