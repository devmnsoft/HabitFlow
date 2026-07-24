$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
if(!(Test-Path "$root/assets/css/style.css")){ throw 'CSS principal ausente' }
if(!(Test-Path "$root/assets/js/app.js")){ throw 'JS principal ausente' }
Write-Host 'Assets OK'
