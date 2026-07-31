# Auditoria inicial v6.9.5

Data UTC: 2026-07-31.

## Git

- HEAD inicial: `81e2748` (`Merge pull request #83`).
- Branch encontrada: `work`, sem alterações locais.
- Os merges locais dos PRs #80 (`28c3da1`), #81 (`ca92382`), #82 (`e23f5f4`) e #83 (`81e2748`) estão no histórico.
- `git fetch --all --prune` não pôde acessar o GitHub: o túnel HTTP do ambiente retornou 403. A branch `feature/v695-activation-journey` foi criada no HEAD local que contém o merge #83.

## Ferramentas e pré-build

- Node.js: `v24.15.0`.
- npm: `11.4.2` (com aviso sobre a configuração legada `http-proxy`).
- O SDK `dotnet` e o cliente `psql` não estão instalados neste contêiner; por isso clean, restore, builds .NET e validações PostgreSQL não puderam ser executados localmente.
- `npm ci` passou após a geração do lockfile ausente.
- `npm audit` não obteve os advisories porque o registry respondeu HTTP 403.
- `npm run security:scan` executou e falhou, reportando achados preexistentes que ainda exigem classificação/remediação; o scanner não foi desativado.
