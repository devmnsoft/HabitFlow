# Inventário do HEAD — v6.14.6

- SHA inicial: `bd2ffddb6e2adf1b53d0e307e0062c91fc724604`
- Branch inicial observada: `work`
- Último PR detectado: PR #149 (`bd2ffdd`, merge de `executar-runner-windows-e-corrigir-falhas`)
- Decisão: **corrigir o runner a partir do inventário e encaminhar a execução para Windows real**.

## Arquivos críticos

| Item | Resultado |
|---|---|
| `scripts/validation/run-release-candidate-local-windows.ps1` | Presente |
| `artifacts/v6145/` | Presente (evidência bloqueada anterior) |
| `HabitTemplateProjection.WithClause` | Presente |
| `HabitTemplateRepository` usando `WithClause` | Presente |
| `HabitTemplateFavoriteRepository` usando `WithClause` | Presente |
| `scripts/validation/smoke-authenticated-routes.ps1` | Presente |
| `scripts/validation/provision-dev-user.ps1` | Ausente; implementação está em `scripts/dev/provision-dev-user.ps1` |
| `scripts/validation/seed-demo-data.ps1` | Ausente; implementação está em `scripts/dev/seed-demo-data.ps1` |

## P0s abertos

Build/publish em Windows, migrations PostgreSQL, EXPLAIN, startup, smokes público e autenticado, biblioteca, jornada MVP, regras Free/Ritmo/Evolução e mobile mínimo aguardam execução no Windows real.
