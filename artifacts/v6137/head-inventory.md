# Inventário do HEAD — v6.13.7

- Data: 2026-08-17 UTC
- SHA inicial: `2fc10090ee53ee6bce9275f6a35f5c7f345367a6`
- Último PR detectado: #141 (`Merge pull request #141...`).
- Branch de trabalho: `feature/v6137-p0-runtime-closure-windows-postgres-smoke-fixes`.
- Ambiente: Ubuntu 24.04.4 LTS, contêiner Linux x64; não é Windows.
- .NET: indisponível (`dotnet: command not found`). A instalação foi tentada, mas o proxy retornou HTTP 403.
- PowerShell: indisponível (`pwsh: command not found`).
- PostgreSQL: binários/runtime indisponíveis; somente `pg_config` 16.13 está presente. `psql` não existe e não há serviço PostgreSQL.
- Node/npm: Node v24.15.0; npm 11.4.2.

## Conteúdo confirmado no histórico

O histórico contém PR #141; correções de `HabitTemplateProjection`, `HabitReminderRow` e `_HabitStatusBadge`; revisão semanal/rotina adaptativa; scripts de validação; e artifacts v6.13.6.

## Módulos existentes

Dashboard, Meu Dia, hábitos, biblioteca/templates/favoritos, onboarding, objetivos, lembretes, notificações, revisão semanal, relatórios, planos/uso, privacidade, perfil e acessibilidade.

## P0 pendentes na entrada

Build/publish .NET, migrations PostgreSQL em banco novo/existente/rerun, startup HTTP, smoke público/autenticado, jornada principal, regras de plano em runtime e QA visual nos viewports solicitados.
