# Inventário de CI v6.13.2

- Executado em: 2026-08-13 (UTC)
- SHA inicial: `d1506602377f6f525ddcfbdf87b6b34112324507`
- Base confirmada: merge do PR #134 (`d150660`), incluindo workflow, cinco helpers de validação/desenvolvimento e evidências v6.13.1.
- Workflow detectado: `.github/workflows/release-validation.yml`
- Triggers detectados: `pull_request` e `workflow_dispatch`
- Jobs detectados: `build-dotnet`, `frontend-security`, `postgres-migrations`, `artifact-report`
- Runs para o SHA: **Não executado** — o ambiente não possui autenticação GitHub (`gh auth status` falhou) e a tentativa de consultar a API pública recebeu HTTP 403.
- Disparo: **Bloqueado** — sem `GH_TOKEN`/sessão GitHub não é possível publicar a branch nem chamar `workflow_dispatch`; nenhum token foi solicitado, inventado ou persistido.

| Job remoto | Status | started_at | completed_at | conclusion | Artifact/log |
|---|---|---|---|---|---|
| build-dotnet | Não executado | — | — | — | Sem run real |
| frontend-security | Não executado | — | — | — | Sem run real |
| postgres-migrations | Não executado | — | — | — | Sem run real |
| artifact-report | Não executado | — | — | — | Sem run real |
