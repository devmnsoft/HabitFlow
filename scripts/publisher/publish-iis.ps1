param(
  [switch]$NoZip,
  [switch]$Zip,
  [switch]$CopyToIis,
  [switch]$Open
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Set-Location $ProjectRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "HabitFlow - Publicador IIS" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$argsList = @("scripts/publisher/publish-iis.js")
if ($NoZip) { $argsList += "--no-zip" }
if ($Zip) { $argsList += "--zip" }
if ($CopyToIis) { $argsList += "--copy-to-iis" }
if ($Open) { $argsList += "--open" }

try {
  & node @argsList
  if ($LASTEXITCODE -ne 0) { throw "Publicador retornou código $LASTEXITCODE" }
  Write-Host "Publicação IIS concluída com sucesso." -ForegroundColor Green
  Write-Host "Pacote final: $ProjectRoot\publish\iis\HabitFlow-IIS" -ForegroundColor Yellow
} catch {
  Write-Host "Falha na publicação IIS: $_" -ForegroundColor Red
  exit 1
}
