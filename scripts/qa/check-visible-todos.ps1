$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$hits=Get-ChildItem "$root/src/HabitFlow.Web/Views" -Recurse -Include *.cshtml | Select-String -Pattern 'TODO|FIXME'
if($hits){ $hits | ForEach-Object { Write-Host $_ }; throw 'TODO visível encontrado' }
Write-Host 'Visible TODOs OK'
