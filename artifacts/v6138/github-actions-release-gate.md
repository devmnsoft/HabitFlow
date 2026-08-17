# GitHub Actions release gate — v6.13.8

- Workflow: `.github/workflows/v6138-release-gate.yml`.
- Triggers: pull request, push da branch v6.13.8 e dispatch manual.
- Jobs: `dotnet-build-publish`, `frontend-security`, `postgres-migrations`, `runtime-smoke-public` e `artifact-summary`.
- Evidência configurada: publish, relatório PostgreSQL, log/smoke público e resumo agregado são artifacts com `if-no-files-found: error`.
- Política: nenhuma etapa usa `continue-on-error`; falhas impedem os jobs dependentes.
- Status neste commit: aguardando execução externa; URL ainda indisponível porque `gh` não está autenticado e não há remote neste checkout.
