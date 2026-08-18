# Ambiente desta execução v6.14.3

Executado em 2026-08-18 no container Linux disponibilizado ao agente, não no host Windows solicitado.

| Ferramenta | Resultado |
|---|---|
| `pwsh --version` | P0: ausente (`command not found`) |
| `dotnet --version` | P0: ausente (`command not found`) |
| `psql --version` | P0: ausente (`command not found`) |
| `node --version` | aprovado: v24.15.0 |
| `npm --version` | aprovado: 11.4.2 |
| `git --version` | aprovado: 2.43.0 |

O runner Windows não pôde ser executado. Nenhum SDK foi baixado e nenhuma evidência Windows foi fabricada. Instalações sugeridas pelo próprio runner: `winget install Microsoft.PowerShell`, `winget install Microsoft.DotNet.SDK.10` e `winget install PostgreSQL.PostgreSQL`.
