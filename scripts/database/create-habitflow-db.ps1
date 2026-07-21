param([string]$User = "postgres", [string]$Database = "habitflow")
$ErrorActionPreference = "Stop"
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw "psql não encontrado no PATH. Instale PostgreSQL ou adicione bin ao PATH." }
$exists = & psql -U $User -d postgres -tAc "select 1 from pg_database where datname='$Database'"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
if ($exists.Trim() -eq "1") { Write-Host "Banco $Database já existe."; exit 0 }
& psql -U $User -d postgres -c "create database $Database;"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Banco $Database criado com sucesso."
