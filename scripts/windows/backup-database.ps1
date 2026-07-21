param(
  [string]$DatabaseName='habitflow',
  [string]$Host='localhost',
  [int]$Port=5432,
  [string]$User='postgres',
  [string]$OutputDir='backups\database',
  [switch]$HabitflowSchemaOnly,
  [switch]$SchemaOnly,
  [switch]$DataOnly
)
. "$PSScriptRoot\_common.ps1"
Require-Command pg_dump 'Adicione pg_dump ao PATH.'
$root=Get-RepoRoot; $dir=Join-Path $root $OutputDir; New-Item -ItemType Directory -Force $dir|Out-Null
$out=Join-Path $dir ("$DatabaseName-{0}.dump" -f (Get-Date -Format 'yyyyMMdd-HHmmss')); $log=New-LogPath 'backup-database'
$args=@('--format=custom','--no-owner','--no-privileges',"--host=$Host","--port=$Port","--username=$User","--file=$out")
if($HabitflowSchemaOnly){ $args += '--schema=habitflow' }
if($SchemaOnly){ $args += '--schema-only' }
if($DataOnly){ $args += '--data-only' }
$args += $DatabaseName
& pg_dump @args *>&1 | Tee-Object $log
if($LASTEXITCODE -ne 0){throw 'Backup falhou'}
Write-Check OK "Backup criado: $out"
Write-Host 'Use -HabitflowSchemaOnly para restringir o dump ao schema habitflow; combine -SchemaOnly ou -DataOnly quando necessário.'
