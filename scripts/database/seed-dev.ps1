param([string]$User = "postgres", [string]$Database = "habitflow")
$ErrorActionPreference = "Stop"
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw "psql não encontrado no PATH." }
& psql -U $User -d $Database -f "database/seed_dev.sql"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Seed dev aplicado. Usuário: admin@habitflow.local / Admin@123"
