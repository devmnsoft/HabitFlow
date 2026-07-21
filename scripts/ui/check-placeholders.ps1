$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$files = Get-ChildItem -Path (Join-Path $root 'src/HabitFlow.Web/Views') -Recurse -Include *.cshtml
$bad = @()
$patterns = @('Lorem ipsum','Home/Index','Dashboard/Index','Habits/Index','Em construção','TODO visível ao usuário','Placeholder','<img(?![^>]*alt=)')
foreach ($file in $files) {
  $text = Get-Content $file.FullName -Raw
  foreach ($pattern in $patterns) { if ($text -match $pattern) { $bad += "ERRO: conteúdo placeholder ou imagem sem alt em $($file.FullName.Replace($root + [IO.Path]::DirectorySeparatorChar,'')) -> $pattern" } }
}
if ($bad.Count -gt 0) { $bad; exit 1 }
Write-Host 'OK: nenhum placeholder visível encontrado.'
