[CmdletBinding()] param([string]$ConnectionString=$env:HABITFLOW_DB_CONNECTION,[switch]$ValidateRerun)
$ErrorActionPreference='Stop'; . "$PSScriptRoot/_common.ps1"; $root=Get-RepoRoot
Require-Command bash 'Instale Git for Windows e use o Git Bash.'; Require-Command psql 'Instale o cliente PostgreSQL e adicione ao PATH.'
$runner=Join-Path $root 'scripts/database/run-migrations.sh'; $a=@($runner);if($ConnectionString){$a+=$ConnectionString}
Write-Host 'Executando exclusivamente o runner canônico; o aggregate não será aplicado neste banco.'
& bash @a;if($LASTEXITCODE){throw 'Migrations falharam.'}
if($ValidateRerun){Write-Host 'Executando novamente para provar idempotência e checksums...'; & bash @a;if($LASTEXITCODE){throw 'Rerun das migrations falhou.'}}
Write-Check OK 'Migrations concluídas.'
