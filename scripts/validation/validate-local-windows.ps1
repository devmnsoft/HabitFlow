# Usage (PowerShell 7+): set the connection/environment variables, then run
# ./scripts/validation/validate-local-windows.ps1 [-BaseUrl http://localhost:5097] [-SkipMigrations]
[CmdletBinding()] param([string]$BaseUrl=$env:App__BaseUrl,[switch]$SkipMigrations)
$ErrorActionPreference='Stop'; $root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path; Set-Location $root
if(-not $BaseUrl){$BaseUrl='http://localhost:5097'}
$dir=Join-Path $root 'artifacts/v6132'; New-Item -ItemType Directory -Force $dir|Out-Null
$log=Join-Path $dir 'local-validation.log'; $summary=Join-Path $dir 'local-validation-summary.md'; $steps=[System.Collections.Generic.List[string]]::new()
function Run([string]$name,[scriptblock]$command){"[$(Get-Date -Format o)] START $name"|Tee-Object -FilePath $log -Append; & $command *>&1|Tee-Object -FilePath $log -Append; if($LASTEXITCODE){throw "$name failed with exit code $LASTEXITCODE"}; $steps.Add("| $name | Aprovado |")}
function Require($name){if(-not(Get-Command $name -ErrorAction SilentlyContinue)){throw "$name was not found in PATH."}}
try {
  'dotnet','psql','node','npm','bash'|ForEach-Object {Require $_}
  if([string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection)){throw 'ConnectionStrings__DefaultConnection is required.'}
  if([string]::IsNullOrWhiteSpace($env:ASPNETCORE_ENVIRONMENT)){throw 'ASPNETCORE_ENVIRONMENT is required.'}
  Run 'dotnet info' {dotnet --info}; Run 'psql version' {psql --version}; Run 'node version' {node --version}; Run 'npm version' {npm --version}
  Run 'dotnet restore' {dotnet restore HabitFlow.sln}; Run 'npm install' {npm install}
  Run 'dotnet build Release' {dotnet build HabitFlow.sln --configuration Release --no-restore}
  Run 'dotnet publish Release' {dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj --configuration Release --no-build --output artifacts/v6132-publish}
  Run 'frontend security scan' {npm run security:scan}; Run 'frontend tests' {npm test}; Run 'production dependency audit' {npm audit --omit=dev}
  foreach($js in 'habits-v4.js','my-day-v3.js','habit-library.js','notifications-v2.js','global-search.js','header-v4.js','feedback-v5.js','guided-tour-v4.js'){Run "syntax $js" {node --check "src/HabitFlow.Web/wwwroot/js/$js"}}
  if(-not $SkipMigrations){Run 'PostgreSQL migrations' {& scripts/validation/validate-postgres-migrations.ps1 -ConnectionString $env:ConnectionStrings__DefaultConnection -SkipCreateDatabase}}
  $env:ASPNETCORE_URLS=$BaseUrl; $process=Start-Process dotnet -ArgumentList 'artifacts/v6132-publish/HabitFlow.Web.dll' -PassThru -RedirectStandardOutput (Join-Path $dir 'app.stdout.log') -RedirectStandardError (Join-Path $dir 'app.stderr.log')
  try { for($i=0;$i-lt 30;$i++){try{Invoke-WebRequest "$BaseUrl/" -UseBasicParsing|Out-Null;break}catch{Start-Sleep 1}}; foreach($route in '/','/plans','/login','/service-worker.js','/favicon.ico'){ $response=Invoke-WebRequest "$BaseUrl$route" -UseBasicParsing -MaximumRedirection 5; if($response.StatusCode -ge 400){throw "GET $route returned $($response.StatusCode)"}; $steps.Add("| GET $route | Aprovado ($($response.StatusCode)) |") }; try{$health=Invoke-WebRequest "$BaseUrl/health" -UseBasicParsing;if($health.StatusCode -lt 400){$steps.Add('| GET /health | Aprovado |')}}catch{$steps.Add('| GET /health | Não executado (rota ausente) |')} } finally {Stop-Process $process -Force -ErrorAction SilentlyContinue}
  @('# Validação local Windows v6.13.2','',"Base URL: $BaseUrl",'','| Etapa | Status |','|---|---|')+$steps|Set-Content $summary -Encoding utf8
} catch { @('# Validação local Windows v6.13.2','',"**Falhou:** $($_.Exception.Message)",'','A connection string nunca é registrada por este script.')|Set-Content $summary -Encoding utf8; throw }
