# Windows local follow-up — HabitFlow v6.15.4

The GitHub Actions gate validates Release build/publish, PostgreSQL migrations and idempotent rerun, published ASP.NET Core startup, public routes, and authenticated MVC navigation including privacy/LGPD. Windows local validation remains mandatory for IIS, a real browser, responsive/mobile behavior, UX, and the mutational MVP journey. Production is approved only after **both** gates are green.

```powershell
cd C:\MNSOFT\HabitFlow

$env:HABITFLOW_LOCAL_CONNECTION = "Host=<DB_HOST>;Port=<DB_PORT>;Database=<DB_NAME>;Username=<DB_USER>;Password=<DB_PASSWORD>"

pwsh .\scripts\validation\run-release-candidate-local-windows.ps1 `
  -BaseUrl "http://localhost:5097" `
  -ConnectionString $env:HABITFLOW_LOCAL_CONNECTION `
  -DevEmail "release-gate@habitflow.local"
```

Never save the local connection string or password in Git or an artifact.
