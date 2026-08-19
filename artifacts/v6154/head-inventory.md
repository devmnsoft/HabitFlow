# HabitFlow .NET head inventory — v6.15.4

Inventory captured before implementation.

## Initial state

- Initial SHA: `4f68a27e6f70ea814153490b795f2482d06dd94f`
- Working branch at capture: `work`
- Working tree: clean

## .NET solution and projects

- `./HabitFlow.sln`
- `./src/HabitFlow.Application/HabitFlow.Application.csproj`
- `./src/HabitFlow.Domain/HabitFlow.Domain.csproj`
- `./src/HabitFlow.Infrastructure/HabitFlow.Infrastructure.csproj`
- `./src/HabitFlow.Shared/HabitFlow.Shared.csproj`
- `./src/HabitFlow.Web/HabitFlow.Web.csproj`
- `./tests/HabitFlow.Tests/HabitFlow.Tests.csproj`

## Required release assets

- `HabitFlow.sln`: present
- `src/HabitFlow.Web/HabitFlow.Web.csproj`: present
- `scripts/validation/validate-postgres-migrations.ps1`: present
- `scripts/validation/run-release-candidate-local-windows.ps1`: present
- `scripts/database/run-migrations.sh`: present

## Migration tail

```text
database/migrations/047_schema_migration_governance.sql
database/migrations/048_password_recovery_transactional_email.sql
database/migrations/049_billing_communication_rule_seed_integrity.sql
database/migrations/050_goal_progress_activation_core.sql
database/migrations/051_system_settings_contract.sql
database/migrations/052_library_v2_onboarding.sql
database/migrations/053_persistent_onboarding_drafts.sql
database/migrations/054_onboarding_engagement_notification_center.sql
database/migrations/055_routine_planner_weekly_review.sql
database/migrations/056_secure_admin_honest_plans_legal_privacy.sql
database/migrations/057_legal_document_immutability.sql
database/migrations/058_user_sessions.sql
database/migrations/059_superadmin_mfa.sql
database/migrations/060_public_privacy_notice.sql
database/migrations/061_habit_lifecycle.sql
database/migrations/062_product_tips.sql
database/migrations/063_account_privacy_center.sql
database/migrations/064_v6121_commercial_plan_integrity.sql
database/migrations/065_v6123_crud_contract_backfill.sql
database/migrations/066_lgpd_privacy_schema_repair.sql
```

The canonical, contiguous stream currently ends at migration `066_lgpd_privacy_schema_repair.sql`.

## Last 30 commits at capture

```text
4f68a27 Merge pull request #153 from devmnsoft/codex/fechar-runtime-habitflow-e-valora
2d79062 fix: harden lgpd runtime schema validation
4efe0df Merge pull request #152 from devmnsoft/codex/fechar-runtime-real-do-habitflow-e-valora
5650fe6 fix: validate habitflow lgpd runtime release gate
9f0278d Merge pull request #151 from devmnsoft/codex/corrigir-esquema-lgpd-e-validacao-jwt
b902530 fix: repair habitflow lgpd privacy schema migration
fae7d51 Merge pull request #150 from devmnsoft/codex/executar-validacao-no-windows-real
469a9b0 fix: prepare v6146 windows release gate evidence
bd2ffdd Merge pull request #149 from devmnsoft/codex/executar-runner-windows-e-corrigir-falhas
833f3ac docs: add v6145 blocked release candidate evidence
e80eaac Merge pull request #148 from devmnsoft/codex/corrigir-sql-da-habittemplateprojection
b9a2c1b fix: stabilize habit template SQL and library validation
aa55b3e Merge pull request #147 from devmnsoft/codex/criar-runner-local-windows-para-mvp
5a5789c chore: add local windows release candidate runner
37af819 Merge pull request #146 from devmnsoft/codex/executar-github-actions-e-fechar-release-candidate
f84f1f1 docs: add v6142 blocked release decision evidence
7b1d5bd Merge pull request #145 from devmnsoft/codex/executar-release-gate-e-corrigir-falhas
9d44ab9 ci: add authenticated smoke to existing release gate
7f845da Merge pull request #143 from devmnsoft/codex/forcar-validacao-real-via-github-actions
9ec4d6b ci: add v6138 real release gate and evidence
41c89b3 Merge pull request #142 from devmnsoft/codex/fechar-p0s-de-runtime-e-corrigir-falhas
d35377d fix: close v6137 runtime validation gaps
2fc1009 Merge pull request #141 from devmnsoft/codex/fechar-e-validar-jornada-principal-do-habitflow
3962f79 fix: validate and stabilize core user journey runtime
ee66750 Merge pull request #140 from devmnsoft/codex/corrigir-materializacao-dapper-de-habittemplate
96cc4ee fix: harden habit template dapper projection
ce21153 Merge pull request #139 from devmnsoft/codex/corrigir-materializacao-do-dapper-em-habitflow
f743cef fix: map habit templates through dapper row dto
80a4a0c Merge pull request #138 from devmnsoft/codex/evoluir-logica-de-rotina-e-interface-premium
aa3b023 feat: add actionable weekly review and adaptive routine
```
