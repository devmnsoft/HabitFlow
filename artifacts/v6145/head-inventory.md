# Inventário do HEAD — v6.14.5

- Data (UTC): 2026-08-18
- SHA inicial: `e80eaac5853f61f4fd8247bf2d815bfdee978385`
- Branch de trabalho criada: `feature/v6145-windows-runner-execution-library-runtime-mvp-closure`
- Último PR detectado no histórico: PR #148 (`e80eaac`)

## Arquivos críticos confirmados

- `HabitTemplateProjection.WithClause` está presente.
- `HabitTemplateRepository` e `HabitTemplateFavoriteRepository` usam `WithClause`.
- `scripts/validation/run-release-candidate-local-windows.ps1` e `smoke-authenticated-routes.ps1` estão presentes.
- Os artifacts v6.14.4 estão presentes.
- `.github/workflows/v6138-release-gate.yml` contém o job de smoke autenticado.

## Estado e plano

O SQL corretivo existe no HEAD. O plano era executar ambiente, build/publish, PostgreSQL/EXPLAIN, startup, smokes e jornada. A execução parou corretamente no gate de ambiente: o contêiner é Linux, não Windows, e não possui `dotnet`, `pwsh` nem `psql`. Esses são P0s de validação, não bugs reproduzidos da aplicação.
