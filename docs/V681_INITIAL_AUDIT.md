# Auditoria inicial v6.8.1

- Commit inicial: `5bd9fac`; branch original: `work`. O clone não possuía remote `origin` nem branch `main`, portanto a feature partiu do commit disponível.
- Antes, `seed_dev.sql` criava `superadmin.dev@habitflow.local` com hash placeholder não recuperável; o PowerShell apenas produzia SQL manual. Não houve tentativa de recuperar senha/hash nem senha de produção válida encontrada.
- Antes, Dashboard e AJAX preenchiam métricas com zero; conclusão usava `DateTime.UtcNow` e listava hábitos para autorizar.
- `git status`, branch e `git log -12` foram executados. `dotnet --info`, `psql --version`, clean e restore foram tentados e falharam porque os executáveis não existem.
- Builds por camada/solução, tests, publish, runner PostgreSQL, testes funcionais e Playwright não puderam ser executados neste ambiente.
- Não há workflow run/CI verde a declarar.
