param([string]$DatabaseName='habitflow',[string]$Host='localhost',[int]$Port=5432,[string]$User='postgres',[string]$Environment='Production',[switch]$DevSeed)
. "$PSScriptRoot\_common.ps1"; Require-Command psql 'Adicione psql ao PATH.'; $root=Get-RepoRoot; $log=New-LogPath 'apply-database-script'
$sql=Join-Path $root 'database\script_completo.sql'; & psql -h $Host -p $Port -U $User -d $DatabaseName -v ON_ERROR_STOP=1 -f $sql *>&1 | Tee-Object $log
if($LASTEXITCODE -ne 0){throw 'Falha ao aplicar script_completo.sql'}
if($DevSeed){ if($Environment -eq 'Production'){throw 'DevSeed bloqueado em Production'}; & psql -h $Host -p $Port -U $User -d $DatabaseName -v ON_ERROR_STOP=1 -f (Join-Path $root 'database\script_completo_dev.sql') *>&1 | Tee-Object -Append $log; if($LASTEXITCODE -ne 0){throw 'Falha ao aplicar seed dev'} }
