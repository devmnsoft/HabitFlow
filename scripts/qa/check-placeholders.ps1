$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$hits=Get-ChildItem "$root/src/HabitFlow.Web/Views" -Recurse -Include *.cshtml | Select-String -Pattern 'Lorem ipsum|Página em construção|JSON técnico|stack trace' -SimpleMatch:$false
if($hits){ $hits | ForEach-Object { Write-Host $_ }; throw 'Placeholders visíveis encontrados' }
Write-Host 'Placeholders OK'
