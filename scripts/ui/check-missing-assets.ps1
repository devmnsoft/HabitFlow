$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$wwwroot = Join-Path $root 'src/HabitFlow.Web/wwwroot'
$report = Join-Path $root 'docs/missing-assets-report.txt'
$files = Get-ChildItem -Path (Join-Path $root 'src/HabitFlow.Web') -Recurse -Include *.cshtml,*.css,*.js
$missing = @()
$pattern = '((src|href)=\"(?<path>/(img|icons|brand|assets|css|js)/[^\"#?]+)|url\([\"'']?(?<bg>/(img|icons|brand|assets)/[^\)\"'']+))'
foreach ($file in $files) {
  $text = Get-Content $file.FullName -Raw
  foreach ($match in [regex]::Matches($text, $pattern)) {
    $path = if ($match.Groups['path'].Success) { $match.Groups['path'].Value } else { $match.Groups['bg'].Value }
    if ($path -match '^https?://') { continue }
    $disk = Join-Path $wwwroot ($path.TrimStart('/') -replace '/', [IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path $disk)) { $missing += "ERRO: arquivo referenciado não encontrado: $($file.FullName.Replace($root + [IO.Path]::DirectorySeparatorChar,'')) -> $path" }
  }
}
$missing | Set-Content $report
if ($missing.Count -gt 0) { $missing; exit 1 }
'OK: nenhuma referência local ausente encontrada.' | Set-Content $report
Write-Host 'OK: nenhuma referência local ausente encontrada.'
