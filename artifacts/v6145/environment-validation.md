# Validação de ambiente — v6.14.5

Executada em 2026-08-18 no contêiner Linux `/workspace/HabitFlow`; este não é o host Windows solicitado (`C:\MNSOFT\HabitFlow`).

| Ferramenta | Resultado real |
|---|---|
| `dotnet --info` | P0 — comando ausente (exit 127) |
| `pwsh --version` | P0 — comando ausente (exit 127) |
| `psql --version` | P0 — comando ausente (exit 127) |
| `node --version` | aprovado — v24.15.0 |
| `npm --version` | aprovado — 11.4.2 |
| `git --version` | aprovado — 2.43.0 |

Nenhuma ferramenta foi baixada automaticamente. No Windows, instalar .NET SDK 10 (`winget install Microsoft.DotNet.SDK.10`), PowerShell 7 (`winget install Microsoft.PowerShell`) e PostgreSQL/psql (`winget install PostgreSQL.PostgreSQL`), então repetir o runner com uma credencial local mantida somente em memória.
