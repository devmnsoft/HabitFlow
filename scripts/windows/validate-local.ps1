[CmdletBinding()] param([string]$ConnectionString=$env:HABITFLOW_DB_CONNECTION,[string]$BaseUrl='http://localhost:5097',[switch]$SkipMigrations,[switch]$SkipPlaywright)
$ErrorActionPreference='Stop';. "$PSScriptRoot/_common.ps1";$root=Get-RepoRoot;$artifact=Join-Path $root 'artifacts/v6120';New-Item -ItemType Directory -Force $artifact|Out-Null
$results=[System.Collections.Generic.List[object]]::new();$started=Get-Date
function Invoke-Step([string]$Name,[scriptblock]$Action){$at=Get-Date;$global:LASTEXITCODE=0;try{&$Action;if($LASTEXITCODE){throw "exit code $LASTEXITCODE"};$script:results.Add([pscustomobject]@{Name=$Name;Status='PASS';Seconds=[math]::Round(((Get-Date)-$at).TotalSeconds,1);Detail=''})}catch{$script:results.Add([pscustomobject]@{Name=$Name;Status='FAIL';Seconds=[math]::Round(((Get-Date)-$at).TotalSeconds,1);Detail=$_.Exception.Message});Write-Report;throw}}
function Write-Report{$lines=@('# HabitFlow v6.12.0 — validação local','',"Gerado (UTC): $((Get-Date).ToUniversalTime().ToString('o'))","Base URL: $BaseUrl",'','| Etapa | Resultado | Segundos | Detalhe |','|---|---:|---:|---|');foreach($r in $results){$lines+="| $($r.Name) | $($r.Status) | $($r.Seconds) | $($r.Detail.Replace('|','\|')) |"};$lines+="`nDuração total: $([math]::Round(((Get-Date)-$started).TotalMinutes,2)) min.";$lines|Set-Content (Join-Path $artifact 'validation-report.md') -Encoding utf8}
Push-Location $root
try{
  Invoke-Step 'dotnet clean' {dotnet clean HabitFlow.sln -c Release}
  Invoke-Step 'dotnet restore' {dotnet restore HabitFlow.sln}
  Invoke-Step 'dotnet build' {dotnet build HabitFlow.sln -c Release --no-restore}
  Invoke-Step 'dotnet test' {dotnet test HabitFlow.sln -c Release --no-build}
  Invoke-Step 'dotnet publish' {dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj -c Release --no-build -o artifacts/v6120/iis-publish}
  if(-not$SkipMigrations){Invoke-Step 'migrations + rerun' {& "$PSScriptRoot/run-migrations.ps1" -ConnectionString $ConnectionString -ValidateRerun}}
  Invoke-Step 'npm ci' {npm ci};Invoke-Step 'npm security:scan' {npm run security:scan};Invoke-Step 'npm test' {npm test};Invoke-Step 'npm audit --omit=dev' {npm audit --omit=dev}
  $js=@('src/HabitFlow.Web/wwwroot/js/header-v4.js','src/HabitFlow.Web/wwwroot/js/feedback-v5.js','src/HabitFlow.Web/wwwroot/js/global-search.js','src/HabitFlow.Web/wwwroot/js/navigation-premium.js','src/HabitFlow.Web/wwwroot/js/guided-tour.js','src/HabitFlow.Web/wwwroot/js/guided-tour-v4.js');foreach($file in $js){Invoke-Step "node --check $file" {node --check $file}}
  if(-not$SkipPlaywright){Invoke-Step 'Playwright real' {& "$PSScriptRoot/run-playwright.ps1" -Suite all -BaseUrl $BaseUrl}}
  Write-Report
}finally{Pop-Location;Write-Report}
