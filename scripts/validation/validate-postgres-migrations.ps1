[CmdletBinding()]
# Usage (PowerShell 7+): ./scripts/validation/validate-postgres-migrations.ps1
#   -ConnectionString $env:ConnectionStrings__DefaultConnection [-SkipCreateDatabase]
param(
  [string]$ConnectionString = $env:ConnectionStrings__DefaultConnection,
  [string]$TemporaryDatabase = ("habitflow_v6161_{0}" -f (Get-Date -Format 'yyyyMMddHHmmss')),
  [string]$ReportPath = 'artifacts/v6161/postgres-migrations-validation.md',
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
$migrationFiles = @(Get-ChildItem (Join-Path $root 'database/migrations') -File -Filter '*.sql' | Where-Object { $_.Name -match '^(\d{3})_[a-z0-9][a-z0-9_-]*\.sql$' } | Sort-Object Name)
if ($migrationFiles.Count -eq 0) { throw 'No canonical migrations were found.' }
$latestMigration = [regex]::Match($migrationFiles[-1].Name, '^(\d{3})_').Groups[1].Value
$expectedMigrationCount = $migrationFiles.Count
$results = [System.Collections.Generic.List[string]]::new()
try {
  if (-not $SkipCreateDatabase) {
    Use-Database $settings 'postgres' {
      $exists = Invoke-Psql "select count(*) from pg_database where datname='$TemporaryDatabase'"
      if($exists -ne '0'){ Invoke-Psql "select pg_terminate_backend(pid) from pg_stat_activity where datname='$TemporaryDatabase' and pid<>pg_backend_pid()" | Out-Null; Invoke-Psql ('drop database "{0}"' -f $TemporaryDatabase) | Out-Null }
      Invoke-Psql ('create database "{0}"' -f $TemporaryDatabase) | Out-Null
    }
    Use-Database $settings $TemporaryDatabase { Invoke-Migrations; $results.Add('| Banco novo | Aprovado | stream canônico até $latestMigration aplicado; lembretes e catálogo incluídos na sanidade |') }
    Use-Database $settings $TemporaryDatabase { Invoke-Migrations; $results.Add('| Rerun | Aprovado | segunda execução idempotente |') }
  } else { $results.Add('| Banco novo | Não executado | `-SkipCreateDatabase` informado |') }

  Use-Database $settings $settings.Database {
    Invoke-Migrations
    $results.Add('| Banco existente | Aprovado | stream canônico aplicado |')
    Invoke-Migrations
    $migrationCount=Invoke-Psql "select count(distinct id) from habitflow.schema_migrations where id ~ '^[0-9]{3}$'"
    Assert-Equal $migrationCount $expectedMigrationCount 'migration registry'
    Assert-Equal (Invoke-Psql "select max(id) from habitflow.schema_migrations where id ~ '^[0-9]{3}$'") $latestMigration 'latest migration'
    Assert-Equal (Invoke-Psql "select count(*) from habitflow.schema_migrations where id ~ '^[0-9]{3}$' and (filename is null or checksum is null)") 0 'migration metadata'
    Assert-Equal (Invoke-Psql 'select count(*) from habitflow.habits where start_date is null') 0 'habits.start_date null count'
    Assert-Equal (Invoke-Psql "select count(*) from habitflow.feature_catalog where implementation_status <> 'Implemented' and is_marketable = true") 0 'non-implemented marketable features'
    $publishedTemplates=Invoke-Psql 'select count(*) from habitflow.habit_templates where is_active = true and published_at is not null'; if([int]$publishedTemplates -lt 1){throw 'No active published habit template exists.'}
    $activePrices=Invoke-Psql 'select count(*) from habitflow.plan_prices where is_active = true'; if([int]$activePrices -lt 1){throw 'No active plan price exists.'}
    Assert-Equal (Invoke-Psql 'select count(*) from habitflow.habits where client_id is null') 0 'habits.client_id null count'
    Assert-Equal (Invoke-Psql 'select count(*) from habitflow.user_goals where client_id is null') 0 'user_goals.client_id null count'
    Assert-Equal (Invoke-Psql 'select count(*) from habitflow.notifications where user_id is null') 0 'notifications.user_id null count'
    Assert-Equal (Invoke-Psql 'select count(*) from habitflow.habit_reminders where client_id is null or user_id is null') 0 'habit_reminders scope null count'
    Assert-Equal (Invoke-Psql 'select count(*) from habitflow.habit_template_favorites where client_id is null or user_id is null') 0 'habit_template_favorites scope null count'
    $missingTables=Invoke-Psql "select count(*) from (values('users'),('clients'),('habits'),('user_goals'),('notifications'),('habit_objectives'),('habit_reminders'),('reminder_dispatches'),('habit_templates'),('habit_template_favorites'),('user_onboarding_progress'),('user_onboarding_draft_items'),('plans'),('plan_prices'),('feature_catalog'),('user_reports'),('weekly_reviews'),('user_privacy_consents'),('privacy_request_events'),('schema_migrations')) v(name) where not exists(select 1 from information_schema.tables t where t.table_schema='habitflow' and t.table_name=v.name)"
    Assert-Equal $missingTables 0 'required tables'
    Assert-Equal (Invoke-Psql "select count(*) from information_schema.columns where table_schema='habitflow' and table_name='habit_reminders' and column_name in ('next_trigger_at','last_triggered_at') and data_type='timestamp with time zone'") 2 'reminder UTC instant columns'
    Assert-Equal (Invoke-Psql "select count(*) from pg_indexes where schemaname='habitflow' and indexname in ('ix_habit_reminders_dispatch_due','ix_reminder_dispatches_lease_recovery')") 2 'reminder runtime indexes'
    $consentColumns=Invoke-Psql "select count(*) from (values('user_id','uuid','NO'),('consent_key','character varying','NO'),('granted','boolean','NO'),('updated_at','timestamp without time zone','NO')) expected(column_name,data_type,is_nullable) where not exists(select 1 from information_schema.columns actual where actual.table_schema='habitflow' and actual.table_name='user_privacy_consents' and actual.column_name=expected.column_name and actual.data_type=expected.data_type and actual.is_nullable=expected.is_nullable)"
    Assert-Equal $consentColumns 0 'user_privacy_consents column contract'
    $eventColumns=Invoke-Psql "select count(*) from (values('id','bigint','int8','NO'),('request_id','uuid','uuid','NO'),('event_type','character varying','varchar','NO'),('status','character varying','varchar','NO'),('occurred_at','timestamp without time zone','timestamp','NO')) expected(column_name,data_type,udt_name,is_nullable) where not exists(select 1 from information_schema.columns actual where actual.table_schema='habitflow' and actual.table_name='privacy_request_events' and actual.column_name=expected.column_name and actual.data_type=expected.data_type and actual.udt_name=expected.udt_name and actual.is_nullable=expected.is_nullable)"
    Assert-Equal $eventColumns 0 'privacy_request_events column contract'
    Assert-Equal (Invoke-Psql "select count(*) from pg_constraint where conrelid='habitflow.privacy_request_events'::regclass and contype='f' and confrelid='habitflow.lgpd_requests'::regclass") 1 'privacy_request_events request foreign key'
    Assert-Equal (Invoke-Psql "select count(*) from pg_indexes where schemaname='habitflow' and tablename='privacy_request_events' and indexname='ix_privacy_request_events_request'") 1 'privacy_request_events request index'
    Assert-Equal (Invoke-Psql "select count(*) from pg_trigger where tgrelid='habitflow.lgpd_requests'::regclass and tgname='trg_audit_privacy_request' and not tgisinternal") 1 'LGPD request audit trigger'
    $results.Add('| Sanidade e schema drift | Aprovado | registro stream completo, contratos LGPD, auditoria, escopo de lembretes e regras comerciais validados |')
  }
  @("# Validação PostgreSQL v6.16.1",'',"Executado em: $(Get-Date -Format o)",'','| Cenário | Status | Evidência |','|---|---|---|') + $results | Set-Content $report -Encoding utf8
} catch {
  @('# Validação PostgreSQL v6.16.1','',"Executado em: $(Get-Date -Format o)",'',"**Falhou:** $($_.Exception.Message)") | Set-Content $report -Encoding utf8
  throw
} finally {
  if(-not $SkipCreateDatabase){ try { Use-Database $settings 'postgres' { Invoke-Psql "select pg_terminate_backend(pid) from pg_stat_activity where datname='$TemporaryDatabase' and pid<>pg_backend_pid()"|Out-Null; Invoke-Psql ('drop database if exists "{0}"' -f $TemporaryDatabase)|Out-Null } } catch { Write-Warning 'Temporary database cleanup failed.' } }
}
