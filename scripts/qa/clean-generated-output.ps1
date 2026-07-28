$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '../..')
$names = @('bin', 'obj', 'TestResults')
Get-ChildItem -Path $root -Directory -Recurse -Force |
    Where-Object { $names -contains $_.Name } |
    Sort-Object { $_.FullName.Length } -Descending |
    Remove-Item -Recurse -Force
$artifacts = Join-Path $root 'artifacts'
if (Test-Path $artifacts) { Remove-Item $artifacts -Recurse -Force }
