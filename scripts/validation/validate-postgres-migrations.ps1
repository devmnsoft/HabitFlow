[CmdletBinding()]
# Usage (PowerShell 7+): ./scripts/validation/validate-postgres-migrations.ps1
#   -ConnectionString $env:ConnectionStrings__DefaultConnection [-SkipCreateDatabase]
param(
  [string]$ConnectionString = $env:ConnectionStrings__DefaultConnection,
  [string]$TemporaryDatabase = ("habitflow_v6137_{0}" -f (Get-Date -Format 'yyyyMMddHHmmss')),
  [string]$ReportPath = 'artifacts/v6137/postgres-migrations-validation.md',
  [switch]$SkipCreateDatabase
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$report = if ([IO.Path]::IsPathRooted($ReportPath)) { $ReportPath } else { Join-Path $root $ReportPath }
New-Item -ItemType Directory -Force (Split-Path $report) | Out-Null

function Convert-Connection([string]$value) {
  if ([string]::IsNullOrWhiteSpace($value)) { throw 'ConnectionStrings__DefaultConnection is required.' }
  $parts = @{}
  foreach ($item in ($value -split ';')) { if ($item -match '^\s*([^=]+)=(.*)$') { $parts[$matches[1].Trim().ToLowerInvariant()] = $matches[2].Trim() } }
  $hostName = $parts['host']; if (-not $hostName) { $hostName = $parts['server'] }
  $user = $parts['username']; if (-not $user) { $user = $parts['user id'] }
  $database = $parts['database']; if (-not $database) { $database = $parts['initial catalog'] }
  if (-not $hostName -or -not $user -or -not $database) { throw 'Connection string must contain Host, Username and Database.' }
  return @{ Host=$hostName; Port=($(if($parts['port']){$parts['port']}else{'5432'})); User=$user; Password=$parts['password']; Database=$database }
}
function Use-Database($settings, [string]$database, [scriptblock]$action) {
  $old = @($env:PGHOST,$env:PGPORT,$env:PGUSER,$env:PGPASSWORD,$env:PGDATABASE)
  try {
    $env:PGHOST=$settings.Host; $env:PGPORT=$settings.Port; $env:PGUSER=$settings.User
    $env:PGPASSWORD=$settings.Password; $env:PGDATABASE=$database
    & $action
  } finally { $env:PGHOST=$old[0]; $env:PGPORT=$old[1]; $env:PGUSER=$old[2]; $env:PGPASSWORD=$old[3]; $env:PGDATABASE=$old[4] }
}
function Invoke-Psql([string]$sql) { $result = & psql -X -v ON_ERROR_STOP=1 -Atqc $sql; if($LASTEXITCODE){throw 'PostgreSQL validation command failed.'}; return ($result | Out-String).Trim() }
function Invoke-Migrations { & bash (Join-Path $root 'scripts/database/run-migrations.sh'); if($LASTEXITCODE){throw 'Migration runner failed.'} }
function Assert-Equal($actual,$expected,$name) { if("$actual" -ne "$expected"){throw "$name expected $expected, received $actual"} }

if (-not (Get-Command psql -ErrorAction SilentlyContinue)) { throw 'psql was not found in PATH.' }
if (-not (Get-Command bash -ErrorAction SilentlyContinue)) { throw 'bash (Git for Windows or equivalent) was not found in PATH.' }
$settings = Convert-Connection $ConnectionString
if ($TemporaryDatabase -notmatch '^[a-zA-Z_][a-zA-Z0-9_]{0,62}$') { throw 'TemporaryDatabase must be a valid PostgreSQL identifier (maximum 63 characters).' }
$results = [System.Collections.Generic.List[string]]::new()
try {
  if (-not $SkipCreateDatabase) {
    Use-Database $settings 'postgres' {
      $exists = Invoke-Psql "select count(*) from pg_database where datname='$TemporaryDatabase'"
      if($exists -ne '0'){ Invoke-Psql "select pg_terminate_backend(pid) from pg_stat_activity where datname='$TemporaryDatabase' and pid<>pg_backend_pid()" | Out-Null; Invoke-Psql "drop database \"$TemporaryDatabase\"" | Out-Null }
      Invoke-Psql "create database \"$TemporaryDatabase\"" | Out-Null
    }
    Use-Database $settings $TemporaryDatabase { Invoke-Migrations; $results.Add('| Banco novo | Aprovado | migrations 001–065 aplicadas; lembretes e catálogo incluídos na sanidade |') }
    Use-Database $settings $TemporaryDatabase { Invoke-Migrations; $results.Add('| Rerun | Aprovado | segunda execução idempotente |') }
  } else { $results.Add('| Banco novo | Não executado | `-SkipCreateDatabase` informado |') }

  Use-Database $settings $settings.Database {
    Invoke-Migrations
    $results.Add('| Banco existente | Aprovado | stream canônico aplicado |')
    Invoke-Migrations
    $migrationCount=Invoke-Psql "select count(distinct id) from habitflow.schema_migrations where id ~ '^[0-9]{3}$' and id::int between 1 and 65"
    Assert-Equal $migrationCount 65 'migration registry'
    Assert-Equal (Invoke-Psql 'select count(*) from habitflow.habits where start_date is null') 0 'habits.start_date null count'
    Assert-Equal (Invoke-Psql "select count(*) from habitflow.feature_catalog where implementation_status <> 'Implemented' and is_marketable = true") 0 'non-implemented marketable features'
    $publishedTemplates=Invoke-Psql 'select count(*) from habitflow.habit_templates where is_active = true and published_at is not null'; if([int]$publishedTemplates -lt 1){throw 'No active published habit template exists.'}
    $activePrices=Invoke-Psql 'select count(*) from habitflow.plan_prices where is_active = true'; if([int]$activePrices -lt 1){throw 'No active plan price exists.'}
    Assert-Equal (Invoke-Psql 'select count(*) from habitflow.habits where client_id is null') 0 'habits.client_id null count'
    Assert-Equal (Invoke-Psql 'select count(*) from habitflow.user_goals where client_id is null') 0 'user_goals.client_id null count'
    Assert-Equal (Invoke-Psql 'select count(*) from habitflow.notifications where user_id is null') 0 'notifications.user_id null count'
    Assert-Equal (Invoke-Psql 'select count(*) from habitflow.habit_reminders where client_id is null or user_id is null') 0 'habit_reminders scope null count'
    Assert-Equal (Invoke-Psql 'select count(*) from habitflow.habit_template_favorites where client_id is null or user_id is null') 0 'habit_template_favorites scope null count'
    $missingTables=Invoke-Psql "select count(*) from (values('users'),('clients'),('habits'),('user_goals'),('notifications'),('habit_reminders'),('habit_templates'),('habit_template_favorites'),('user_onboarding_progress'),('user_onboarding_draft_items'),('plans'),('plan_prices'),('feature_catalog'),('user_reports'),('weekly_reviews'),('schema_migrations')) v(name) where not exists(select 1 from information_schema.tables t where t.table_schema='habitflow' and t.table_name=v.name)"
    Assert-Equal $missingTables 0 'required tables'
    $results.Add('| Sanidade e schema drift | Aprovado | registro 001–065, escopo de lembretes, tabelas e regras comerciais validados |')
  }
  @("# Validação PostgreSQL v6.13.7",'',"Executado em: $(Get-Date -Format o)",'','| Cenário | Status | Evidência |','|---|---|---|') + $results | Set-Content $report -Encoding utf8
} catch {
  @('# Validação PostgreSQL v6.13.7','',"Executado em: $(Get-Date -Format o)",'',"**Falhou:** $($_.Exception.Message)") | Set-Content $report -Encoding utf8
  throw
} finally {
  if(-not $SkipCreateDatabase){ try { Use-Database $settings 'postgres' { Invoke-Psql "select pg_terminate_backend(pid) from pg_stat_activity where datname='$TemporaryDatabase' and pid<>pg_backend_pid()"|Out-Null; Invoke-Psql "drop database if exists \"$TemporaryDatabase\""|Out-Null } } catch { Write-Warning 'Temporary database cleanup failed.' } }
}
