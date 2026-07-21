# Scripts Windows/IIS sem Docker
Use PowerShell 5.1+ ou PowerShell 7. Os scripts nunca apagam banco sem confirmação explícita e gravam relatórios em `publish/logs`.

Fluxo: `check-environment.ps1`, `setup-postgres-database.ps1`, `apply-database-script.ps1`, `generate-production-config.ps1`, `publish-iis.ps1`, `smoke-test.ps1`. Backups ficam em `backups/` e não devem ser versionados.
