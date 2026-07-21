param([string]$User = "postgres", [string]$Database = "habitflow", [switch]$DevSeed)
$ErrorActionPreference = "Stop"
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw "psql não encontrado no PATH. Instale PostgreSQL ou adicione bin ao PATH." }
& psql -U $User -d $Database -f "database/script_completo.sql"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if ($DevSeed) { & psql -U $User -d $Database -f "database/seed_dev.sql"; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE } }
Write-Host "Schema HabitFlow aplicado com sucesso."
