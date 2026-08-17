# Build e publish — v6.13.7

| Comando | Status | Evidência |
|---|---|---|
| `dotnet --info` | Bloqueado | `dotnet: command not found` |
| `dotnet clean HabitFlow.sln` | Não executável | SDK ausente |
| `dotnet restore HabitFlow.sln` | Não executável | SDK ausente |
| `dotnet build HabitFlow.sln --configuration Release --no-restore` | Não executável | SDK ausente |
| `dotnet publish ... --output artifacts/v6137-publish` | Não executável | SDK ausente |

Foi tentada a instalação do SDK 10 pelo instalador oficial; o proxy do ambiente devolveu HTTP 403. Nenhum erro de compilação foi observado porque o compilador não pôde ser iniciado. Nenhum diretório de publish foi criado ou commitado. Build e publish permanecem P0.
