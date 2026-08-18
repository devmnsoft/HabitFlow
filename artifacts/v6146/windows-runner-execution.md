# Execução do runner Windows — v6.14.6

- Horário: 2026-08-18 UTC.
- Ambiente observado: Linux x86_64, sem `pwsh`, `dotnet` e `psql`.
- Fase em que parou: pré-check do Windows real.
- Status: **não executado no ambiente alvo**.

## Comando final (senha mascarada)

```powershell
cd C:\MNSOFT\HabitFlow
# Defina HABITFLOW_LOCAL_CONNECTION somente nesta sessão, sem registrar seu valor.
pwsh .\scripts\validation\run-release-candidate-local-windows.ps1 `
  -BaseUrl "http://localhost:5097" `
  -ConnectionString $env:HABITFLOW_LOCAL_CONNECTION `
  -DevEmail "release-gate@habitflow.local"
```

## Erro bruto

```text
P0: o executor disponível é Linux e não possui pwsh, dotnet ou psql; a validação exigida depende de Windows real.
```

- Primeira causa provável: job alocado no ambiente errado, antes de qualquer falha da aplicação.
- Próximo passo: preparar as ferramentas conforme `windows-precheck.md`, abrir novo PowerShell e executar o comando acima.
