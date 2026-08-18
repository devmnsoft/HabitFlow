# Dispatch do workflow

- Workflow encontrado localmente: `v6.13.8 release gate`.
- `workflow_dispatch`: presente.
- Tentativa de consulta: `gh run list --workflow "v6.13.8 release gate" --limit 10`.
- Resultado real: bloqueado antes de consultar a API porque o GitHub CLI não está autenticado.
- O checkout também não possui remote Git configurado; portanto não foi possível publicar a branch ou disparar o commit pelo GitHub.
- Run id/URL/commit/event/jobs/status inicial: indisponíveis; nenhum valor foi inventado.
