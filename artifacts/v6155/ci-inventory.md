# Inventário do CI v6.15.5

- SHA inicial: `3c49eb1ea1d5cb607e7f621ef901541b50544aaf`.
- O histórico contém o merge do PR #154 (`c2f6bc5`) e a implementação do gate (`e6c0d1f`).
- Antes da consolidação existiam oito workflows, incluindo o gate canônico `.github/workflows/habitflow-dotnet-release-gate.yml` e o legado `.github/workflows/v6138-release-gate.yml`.
- `scripts/validation/provision-ci-user.ps1` e `scripts/validation/smoke-authenticated-routes.ps1` estavam presentes no HEAD inicial.
- Foram detectadas 66 migrations canônicas, de `001` a `066`.
- O inventário foi obtido com `git status --short`, `git rev-parse HEAD`, `git log -20 --oneline` e os comandos `find` solicitados.
