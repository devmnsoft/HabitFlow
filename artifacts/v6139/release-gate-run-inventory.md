# Inventário da execução do release gate — v6.13.9

- Workflow anterior procurado: `v6.13.8 release gate`.
- `gh auth status`: não autenticado; `gh run list` não pôde consultar runs.
- Fallback REST: `https://api.github.com/repos/devmnsoft/HabitFlow/actions/...` bloqueado pelo proxy com HTTP 403.
- Run URL/id/commit/evento/jobs/conclusão/artifacts: **não verificáveis neste ambiente**.
- Correção controlada: o workflow passou a identificar v6.13.9, observar push da branch v6.13.9 e publicar artifacts v6.13.9. O trigger `pull_request` e o `workflow_dispatch` permanecem ativos.
- Estado: execução real deverá ocorrer no push/PR; não há alegação de sucesso sem o run.
