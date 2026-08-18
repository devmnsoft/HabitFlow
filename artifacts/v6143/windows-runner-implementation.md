# Implementação do runner Windows v6.14.3

O comando único está em `scripts/validation/run-release-candidate-local-windows.ps1`.

Ele valida ferramentas sem instalá-las, executa clean/restore/build/publish, valida migrations fresh/existing/rerun, controla a aplicação publicada, executa smokes, cria identidade e seed com senha aleatória em memória, resolve IDs pelo banco e produz relatórios. O `finally` sempre encerra o processo e emite decisão conservadora.

## Uso no Windows

```powershell
$connection = Read-Host 'Connection string local'
pwsh .\scripts\validation\run-release-candidate-local-windows.ps1 `
  -BaseUrl 'http://localhost:5097' `
  -ConnectionString $connection `
  -DevEmail 'release-gate@habitflow.local'
```

A connection string e a senha não são escritas nos artifacts. `artifacts/v6143-publish` permanece ignorado pelo Git.
