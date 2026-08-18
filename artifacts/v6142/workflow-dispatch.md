# Dispatch do workflow

| Campo | Resultado |
|---|---|
| Método tentado | GitHub CLI (`gh auth status`, `gh workflow list`) e API pública via `curl` |
| Run ID / URL | Não obtido |
| Branch | `feature/v6142-remote-actions-execution-real-artifacts-rc-closure` (somente local) |
| Commit inicial | `7b1d5bd4f5022bd613b2e37b06a98bbfc9f62e38` |
| Evento | Não disparado |
| Status inicial | Bloqueado antes do dispatch |

`gh auth status` informou que não há login. Não existe remote Git configurado, então a branch não pôde ser publicada. A tentativa à API de `api.github.com` foi recusada pelo proxy com HTTP 403. Nenhum run, URL ou artifact foi inventado.
