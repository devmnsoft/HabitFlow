param([string]$DatabaseName='habitflow',[string]$Host='localhost',[int]$Port=5432,[string]$User='postgres')
. "$PSScriptRoot\_common.ps1"; Require-Command psql 'Adicione a pasta bin do PostgreSQL ao PATH.'
$exists=& psql -h $Host -p $Port -U $User -d postgres -tAc "select 1 from pg_database where datname='$DatabaseName'"
if($LASTEXITCODE -ne 0){throw 'Falha ao conectar no PostgreSQL'}
if($exists.Trim() -eq '1'){Write-Check OK "Banco $DatabaseName já existe"}else{& psql -h $Host -p $Port -U $User -d postgres -v ON_ERROR_STOP=1 -c "create database $DatabaseName"; if($LASTEXITCODE -ne 0){throw 'Falha ao criar banco'}; Write-Check OK "Banco $DatabaseName criado"}
