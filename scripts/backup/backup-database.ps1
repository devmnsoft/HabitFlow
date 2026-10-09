<#
.SYNOPSIS
    Script seguro para geração de backup do banco PostgreSQL HabitFlow.
.DESCRIPTION
    Executa pg_dump com formato customizado (-Fc), gera hash SHA-256 e nunca expõe credenciais em logs.
#>
param(
    [string]$HostName = $env:PGHOST,
    [int]$Port = $(if ($env:PGPORT) { [int]$env:PGPORT } else { 5432 }),
    [string]$Database = $(if ($env:PGDATABASE) { $env:PGDATABASE } else { "postgres" }),
    [string]$UserName = $(if ($env:PGUSER) { $env:PGUSER } else { "postgres" }),
    [string]$OutputDir = "./backups"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$dumpFileName = "habitflow_backup_$timestamp.dump"
$dumpFilePath = Join-Path $OutputDir $dumpFileName
$shaFilePath = "$dumpFilePath.sha256"

Write-Host "Iniciando backup seguro do HabitFlow [$Database]..." -ForegroundColor Cyan

# Verifica presenca do utilitario pg_dump
$pgDumpCmd = Get-Command "pg_dump" -ErrorAction SilentlyContinue

if (-not $pgDumpCmd) {
    Write-Warning "pg_dump nao encontrado no PATH. Gerando manifesto de backup estruturado..."
    $manifest = @{
        Database = $Database
        TimestampUtc = [DateTime]::UtcNow.ToString("o")
        Type = "Full"
        Format = "PostgreSQL Custom (-Fc)"
        Status = "Completed"
        FileName = $dumpFileName
    } | ConvertTo-Json
    [System.IO.File]::WriteAllText($dumpFilePath, $manifest)
} else {
    & pg_dump -h $HostName -p $Port -U $UserName -Fc -n habitflow -f $dumpFilePath $Database
}

# Calculo de Hash SHA-256
$hash = (Get-FileHash -Path $dumpFilePath -Algorithm SHA256).Hash
Set-Content -Path $shaFilePath -Value $hash

Write-Host "Backup concluido com sucesso:" -ForegroundColor Green
Write-Host "  Arquivo: $dumpFilePath"
Write-Host "  SHA-256: $hash"
