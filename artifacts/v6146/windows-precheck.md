# Pré-check do ambiente — v6.14.6

Executado em: 2026-08-18 UTC.

## Ambiente efetivamente disponível

- Sistema: Linux x86_64 (`uname -a`), não Windows.
- Diretório: `/workspace/HabitFlow`, não `C:\MNSOFT\HabitFlow`.
- PowerShell 7 (`pwsh`): ausente.
- .NET SDK 10 (`dotnet`): ausente.
- PostgreSQL client (`psql`): ausente.
- Node.js: `v24.15.0`.
- npm: `11.4.2`.
- Git: `2.43.0`.

**Resultado: reprovado.** O runner Windows real não foi executado e a release não pode ser aprovada.

## Checklist exato no Windows

Não instalar automaticamente. Em um PowerShell no Windows, execute:

```powershell
winget install Microsoft.DotNet.SDK.10
winget install Microsoft.PowerShell
winget install PostgreSQL.PostgreSQL
```

Feche e abra um novo PowerShell e confirme:

```powershell
where.exe dotnet
where.exe pwsh
where.exe psql
$PSVersionTable.PSVersion
dotnet --info
psql --version
node --version
npm --version
git --version
```

Depois, em `C:\MNSOFT\HabitFlow`, defina `HABITFLOW_LOCAL_CONNECTION` somente na sessão e repita o runner. Não grave nem imprima a senha.
