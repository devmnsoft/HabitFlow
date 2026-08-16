# Build e publish — v6.13.3

Executado em 2026-08-15 UTC.

| Comando | Resultado |
|---|---|
| `dotnet --info` | Bloqueado: `dotnet: command not found` (exit 127) |
| `dotnet clean HabitFlow.sln` | Bloqueado: SDK ausente |
| `dotnet restore HabitFlow.sln` | Bloqueado: SDK ausente |
| `dotnet build HabitFlow.sln --configuration Release --no-restore` | Bloqueado: SDK ausente |
| `dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj --configuration Release --output artifacts/v6133-publish` | Bloqueado: SDK ausente; nenhum publish foi produzido/commitado |

Tentativas reais de provisionamento: `dotnet-install.sh` retornou HTTP 403 e APT retornou HTTP 403 pelo proxy para os pacotes Ubuntu. Portanto, build e publish **não são declarados aprovados**.
