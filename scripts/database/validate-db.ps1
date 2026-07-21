param([string]$User = "postgres", [string]$Database = "habitflow")
$ErrorActionPreference = "Stop"
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw "psql não encontrado no PATH." }
$required = @('users','habits','habit_completions','system_audit_logs','login_attempts','system_settings')
foreach ($table in $required) {
  $ok = & psql -U $User -d $Database -tAc "select exists(select 1 from information_schema.tables where table_schema='habitflow' and table_name='$table')"
  if ($LASTEXITCODE -ne 0 -or $ok.Trim() -ne 't') { throw "Tabela obrigatória ausente: habitflow.$table" }
}
Write-Host "Banco validado com sucesso."
