[CmdletBinding()]
param(
  [string]$Output = 'publish/windows',
  [string]$Target = '',
  [string]$ConnectionString = $env:HABITFLOW_DB_CONNECTION
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path

dotnet publish (Join-Path $root 'src/HabitFlow.Web/HabitFlow.Web.csproj') -c Release -o (Join-Path $root $Output)
if ($LASTEXITCODE) { throw 'dotnet publish failed.' }
Copy-Item (Join-Path $root 'src/HabitFlow.Web/web.config') (Join-Path $root $Output) -Force

if ($Target) {
  if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    throw 'HABITFLOW_DB_CONNECTION is required before an IIS deployment.'
  }
  # Fail closed before touching the live directory or recycling its app pool.
  & (Join-Path $root 'scripts/windows/run-migrations.ps1') -ConnectionString $ConnectionString -ValidateRerun
  if ($LASTEXITCODE) { throw 'Canonical migrations failed; IIS deployment was aborted.' }
  New-Item -ItemType Directory -Force -Path $Target | Out-Null
  Copy-Item (Join-Path $root "$Output/*") $Target -Recurse -Force
}
'HabitFlow ASP.NET Core publicado; migrations canônicas validadas antes do switch IIS.' |
  Out-File (Join-Path $root "$Output/publish-report.txt")
