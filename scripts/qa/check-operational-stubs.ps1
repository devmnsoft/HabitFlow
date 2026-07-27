$ErrorActionPreference = 'Stop'
$roots = @('src/HabitFlow.Web/Controllers', 'src/HabitFlow.Application/Services')
$patterns = @(
  'TODO\s+implementar',
  'Task\.FromResult\(false\)',
  'return\s+Task\.CompletedTask\s*;',
  'return\s+(Array\.Empty<[^>]+>\(\)|new\s+List<[^>]+>\(\))\s*;'
)
$findings = @()
foreach ($root in $roots) {
  Get-ChildItem $root -Recurse -File -Include *.cs | ForEach-Object {
    $path = $_.FullName
    foreach ($pattern in $patterns) {
      Select-String -Path $path -Pattern $pattern | ForEach-Object { $findings += "${path}:$($_.LineNumber): $($_.Line.Trim())" }
    }
  }
}
if (Test-Path 'src/HabitFlow.Web/Views/Simple.cshtml') { $findings += 'View operacional Simple.cshtml encontrada.' }
if ($findings.Count -gt 0) { $findings | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Host 'Nenhum stub operacional conhecido foi encontrado.'
