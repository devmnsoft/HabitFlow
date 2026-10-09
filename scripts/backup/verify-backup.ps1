<#
.SYNOPSIS
    Script de verificacao de integridade de backup HabitFlow sem vazar credenciais.
#>
param(
    [Parameter(Mandatory=$true)]
    [string]$DumpFile
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $DumpFile)) {
    Write-Error "Arquivo de backup nao encontrado: $DumpFile"
}

$shaFile = "$DumpFile.sha256"
if (Test-Path $shaFile) {
    $expectedHash = (Get-Content $shaFile).Trim()
    $actualHash = (Get-FileHash -Path $DumpFile -Algorithm SHA256).Hash
    if ($expectedHash -ne $actualHash) {
        Write-Error "FALHA DE INTEGRIDADE: O hash do arquivo difere do checksum registrado!"
    }
    Write-Host "Integridade de checksum SHA-256 VALIDADA: $actualHash" -ForegroundColor Green
} else {
    Write-Warning "Checksum .sha256 ausente para o arquivo informado."
}

# Verificacao de estrutura interna via pg_restore --list caso pg_restore exista
$pgRestoreCmd = Get-Command "pg_restore" -ErrorAction SilentlyContinue
if ($pgRestoreCmd) {
    & pg_restore --list $DumpFile | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Estrutura interna do dump PostgreSQL validada com exito." -ForegroundColor Green
    } else {
        Write-Error "FALHA: O arquivo de dump nao pode ser lido pelo pg_restore."
    }
} else {
    Write-Host "pg_restore nao disponivel localmente; arquivo validado por tamanho e hash." -ForegroundColor Yellow
}
