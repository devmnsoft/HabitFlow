# Inventário do HEAD — v6.14.2

- Data UTC: 2026-08-18.
- SHA inicial: `7b1d5bd4f5022bd613b2e37b06a98bbfc9f62e38`.
- Branch recebida no ambiente: `work`; branch de trabalho criada: `feature/v6142-remote-actions-execution-real-artifacts-rc-closure`.
- PR anterior detectado no histórico: PR #145, merge `7b1d5bd`.
- Remote Git: nenhum configurado (`git remote -v` vazio).

## Workflow e inventário confirmado

O arquivo `.github/workflows/v6138-release-gate.yml` existe, chama-se `v6.13.8 release gate` e contém `workflow_dispatch`. Jobs: `dotnet-build-publish`, `frontend-security`, `postgres-migrations`, `runtime-smoke-public`, `runtime-smoke-authenticated` e `artifact-summary`.

Scripts presentes: `smoke-authenticated-routes.ps1`, `provision-dev-user.ps1`, `seed-demo-data.ps1` e `validate-postgres-migrations.ps1`. Também foram localizados `HabitTemplateProjection`, `HabitReminderRow`, `_HabitStatusBadge`, `WeeklyReview` e `AdaptiveHabitService`.

## P0s abertos na entrada

Build/publish .NET, migrations PostgreSQL, inicialização, ambos os smokes, jornada MVP, regras de plano e mobile ainda careciam de evidência real.

## Plano de execução real

Autenticar GitHub CLI, publicar a branch, disparar `workflow_dispatch`, acompanhar todos os jobs, baixar artifacts e então executar validação manual da jornada e dos viewports. O ambiente não forneceu autenticação, remote, .NET, PostgreSQL nem navegador; portanto o plano não pôde avançar além das verificações locais disponíveis.
