#requires -Version 7.0
<#
.SYNOPSIS
Runs the HabitFlow v6.14.3 release gate on a Windows development workstation.
.EXAMPLE
./scripts/validation/run-release-candidate-local-windows.ps1 -ConnectionString $env:HABITFLOW_LOCAL_CONNECTION
#>
[CmdletBinding()] param(
  [string]$BaseUrl = 'http://localhost:5097',
  [Parameter(Mandatory=$true)][string]$ConnectionString,
  [string]$DevEmail = 'release-gate@habitflow.local',
  [string]$Configuration = 'Release',
  [switch]$SkipMobileChecklist
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location $root
$artifactDir = Join-Path $root 'artifacts/v6143'
$publishDir = Join-Path $root 'artifacts/v6143-publish'
$runtimeLog = Join-Path $artifactDir 'habitflow-runtime.log'
$runtimeErrorLog = Join-Path $artifactDir 'habitflow-runtime-error.log'
$initialSha = (git rev-parse HEAD).Trim()
$results = [ordered]@{}
$app = $null
New-Item -ItemType Directory -Force $artifactDir | Out-Null

function Write-Report([string]$Name, [string[]]$Content) {
  $Content | Set-Content (Join-Path $artifactDir $Name) -Encoding utf8
}
function Invoke-Step([string]$Name, [scriptblock]$Action) {
  Write-Host "`n==> $Name" -ForegroundColor Cyan
  & $Action
  if ($LASTEXITCODE) { throw "$Name failed with exit code $LASTEXITCODE." }
  $results[$Name] = 'Aprovado'
}
function Require-Tool([string]$Name, [string]$Use, [string]$InstallHint) {
  if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
    throw "P0: ferramenta ausente: $Name. Uso: $Use. Instalação sugerida: $InstallHint"
  }
}
function Get-DbSettings {
  $parts=@{}; foreach($item in $ConnectionString -split ';') { if($item -match '^\s*([^=]+)=(.*)$'){$parts[$matches[1].Trim().ToLowerInvariant()]=$matches[2].Trim()} }
  return @{Host=$parts['host'];Port=$(if($parts['port']){$parts['port']}else{'5432'});User=$(if($parts['username']){$parts['username']}else{$parts['user id']});Password=$parts['password'];Database=$parts['database']}
}
function Invoke-Db([string]$Sql) {
  $db=Get-DbSettings; $old=@($env:PGHOST,$env:PGPORT,$env:PGUSER,$env:PGPASSWORD,$env:PGDATABASE)
  try {$env:PGHOST=$db.Host;$env:PGPORT=$db.Port;$env:PGUSER=$db.User;$env:PGPASSWORD=$db.Password;$env:PGDATABASE=$db.Database; $value=& psql -X -v ON_ERROR_STOP=1 -Atqc $Sql; if($LASTEXITCODE){throw 'Database query failed.'}; return ($value|Out-String).Trim()}
  finally {$env:PGHOST=$old[0];$env:PGPORT=$old[1];$env:PGUSER=$old[2];$env:PGPASSWORD=$old[3];$env:PGDATABASE=$old[4]}
}
function Stop-PreviousHabitFlow {
  if (-not $IsWindows) { return }
  $port=([uri]$BaseUrl).Port
  $owners=Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue | Select-Object -ExpandProperty OwningProcess -Unique
  foreach($owner in $owners) {
    $process=Get-CimInstance Win32_Process -Filter "ProcessId=$owner"
    if($process.CommandLine -match 'HabitFlow\.Web(\.dll|\.exe)'){Stop-Process -Id $owner -Force; Write-Host "Stopped previous HabitFlow process $owner on port $port."}
    else {throw "P0: port $port is occupied by a process that is not HabitFlow (PID $owner)."}
  }
}
function Assert-RuntimeLog {
  $bad='Unhandled exception|InvalidOperationException|NpgsqlException|\bDapper\b|partial view was not found|StackTrace'
  $content=(Get-Content $runtimeLog,$runtimeErrorLog -Raw -ErrorAction SilentlyContinue) -join "`n"
  if($content -match $bad){throw 'P0: runtime log contains a forbidden exception signature.'}
}
function New-MobileChecklist {
  if($SkipMobileChecklist){Write-Report 'mobile-checklist.md' @('# Checklist mobile v6.14.3','','Não executado: `-SkipMobileChecklist` foi informado.','', '**P0 pendente: validação visual não realizada.**');return}
  $viewports='1440x900','1366x768','1280x720','1024x768','768x1024','430x932','390x844','360x800','320x568'
  $screens='Dashboard','Meu Dia','Biblioteca','Hábitos','Detalhe do hábito','Objetivos','Lembretes','Revisão semanal','Relatórios','Uso do plano'
  $lines=@('# Checklist mobile v6.14.3','',"Gerado em: $(Get-Date -Format o)",'','Preencher manualmente em navegador real. Nenhuma captura foi fabricada.','','## Viewports') + ($viewports|ForEach-Object{"- [ ] $_"}) + @('','## Telas') + ($screens|ForEach-Object{"- [ ] $_ — sem overflow, CTA coberto, header quebrado, modal fora da tela ou contraste crítico"}) + @('','**P0 pendente até que todos os itens sejam conferidos.**')
  Write-Report 'mobile-checklist.md' $lines
}

$failure=$null
try {
  $toolRows=[Collections.Generic.List[string]]::new()
  foreach($tool in @(
    @('dotnet','build e execução','winget install Microsoft.DotNet.SDK.10'),
    @('pwsh','orquestração','winget install Microsoft.PowerShell'),
    @('psql','migrations e sanidades','winget install PostgreSQL.PostgreSQL'),
    @('node','checks JavaScript','winget install OpenJS.NodeJS.LTS'),
    @('npm','testes e auditoria','instalado com Node.js'),
    @('git','inventário do release','winget install Git.Git'),
    @('bash','migration runner','winget install Git.Git'))) {
    Require-Tool $tool[0] $tool[1] $tool[2]
    $version = switch($tool[0]){'dotnet'{(dotnet --version)}'pwsh'{(pwsh --version)}default{(& $tool[0] --version | Select-Object -First 1)}}
    $toolRows.Add("| $($tool[0]) | Aprovado | $version |")
  }
  Write-Report 'environment-validation.md' (@('# Ambiente v6.14.3','',"Executado em: $(Get-Date -Format o)",'','A senha da conexão não é registrada.','', '| Ferramenta | Status | Versão |','|---|---|---|')+$toolRows)
  $results['Ambiente']='Aprovado'

  Invoke-Step 'clean' {dotnet clean HabitFlow.sln}
  Invoke-Step 'restore' {dotnet restore HabitFlow.sln}
  Invoke-Step 'build' {dotnet build HabitFlow.sln --configuration $Configuration --no-restore}
  if(Test-Path $publishDir){Remove-Item $publishDir -Recurse -Force}
  Invoke-Step 'publish' {dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj --configuration $Configuration --no-build --output $publishDir}
  Write-Report 'build-publish-validation.md' @('# Build e publish v6.14.3','',"Executado em: $(Get-Date -Format o)",'','- clean: aprovado','- restore: aprovado','- build: aprovado',"- publish: aprovado em diretório ignorado `$publishDir`")

  Invoke-Step 'migrations' {& (Join-Path $root 'scripts/validation/validate-postgres-migrations.ps1') -ConnectionString $ConnectionString -TemporaryDatabase habitflow_v6143_fresh -ReportPath 'artifacts/v6143/postgres-migrations-validation.md'}
  Stop-PreviousHabitFlow
  $env:ASPNETCORE_ENVIRONMENT='Development';$env:ASPNETCORE_URLS=$BaseUrl;$env:ConnectionStrings__DefaultConnection=$ConnectionString
  $app=Start-Process dotnet -ArgumentList @((Join-Path $publishDir 'HabitFlow.Web.dll'),'--urls',$BaseUrl) -PassThru -NoNewWindow -RedirectStandardOutput $runtimeLog -RedirectStandardError $runtimeErrorLog
  $ready=$false; for($i=0;$i-lt 120;$i++){if($app.HasExited){throw "P0: HabitFlow exited during startup (code $($app.ExitCode))."};try{$r=Invoke-WebRequest "$BaseUrl/" -UseBasicParsing -MaximumRedirection 0 -ErrorAction Stop;if($r.StatusCode-lt500){$ready=$true;break}}catch{};Start-Sleep 1}
  if(-not $ready){throw 'P0: startup timeout after 120 seconds.'};Assert-RuntimeLog;$results['startup']='Aprovado'
  Write-Report 'startup-validation.md' @('# Startup v6.14.3','',"Executado em: $(Get-Date -Format o)","- URL: $BaseUrl",'- Aplicação publicada respondeu em até 120 segundos.','- Log verificado contra assinaturas técnicas proibidas.')

  $publicRows=[Collections.Generic.List[string]]::new();foreach($route in '/','/login','/register','/plans','/service-worker.js','/favicon.ico'){$r=Invoke-WebRequest "$BaseUrl$route" -UseBasicParsing -MaximumRedirection 5;if($r.StatusCode-ge500){throw "P0: GET $route returned $($r.StatusCode)."};$publicRows.Add("| `$route` | $($r.StatusCode) | Aprovado |")};Assert-RuntimeLog;$results['smoke público']='Aprovado'
  Write-Report 'public-smoke-validation.md' (@('# Smoke público v6.14.3','',"Executado em: $(Get-Date -Format o)",'','| Rota | HTTP | Status |','|---|---:|---|')+$publicRows)

  $bytes=New-Object byte[] 24;[Security.Cryptography.RandomNumberGenerator]::Fill($bytes);$securePassword=ConvertTo-SecureString (([Convert]::ToBase64String($bytes))+'!aA1') -AsPlainText -Force
  & scripts/dev/provision-dev-user.ps1 -BaseUrl $BaseUrl -Email $DevEmail -Password $securePassword
  & scripts/dev/seed-demo-data.ps1 -Email $DevEmail -ConnectionString $ConnectionString
  $ids=(Invoke-Db "select h.id,(select id from habitflow.habit_templates where status='Published' order by sort_order nulls last limit 1),(select id from habitflow.user_goals where user_id=u.id order by title limit 1) from habitflow.users u join habitflow.habits h on h.user_id=u.id where lower(u.email)=lower('$($DevEmail.Replace("'","''"))') order by h.sort_order limit 1") -split '\|'
  if($ids.Count-ne3 -or $ids -contains ''){throw 'P0: seed IDs could not be resolved.'}
  Write-Report 'dev-user-seed-validation.md' @('# Usuário e seed v6.14.3','',"Executado em: $(Get-Date -Format o)","- Usuário: $DevEmail",'- Senha aleatória permaneceu somente em memória como SecureString.','- Seed idempotente aplicado e IDs resolvidos.')
  & scripts/validation/smoke-authenticated-routes.ps1 -BaseUrl $BaseUrl -Email $DevEmail -Password $securePassword -HabitId $ids[0] -TemplateId $ids[1] -GoalId $ids[2] -ReportPath 'artifacts/v6143/authenticated-smoke-validation.md';$results['smoke autenticado']='Aprovado';Assert-RuntimeLog

  $counts=(Invoke-Db "select count(*) filter(where not h.is_archived),count(*) filter(where hc.completed_date=current_date),count(distinct g.id),count(distinct r.id),count(distinct n.id) from habitflow.users u left join habitflow.habits h on h.user_id=u.id left join habitflow.habit_completions hc on hc.habit_id=h.id left join habitflow.user_goals g on g.user_id=u.id left join habitflow.habit_reminders r on r.user_id=u.id left join habitflow.notifications n on n.user_id=u.id where lower(u.email)=lower('$($DevEmail.Replace("'","''"))')")
  Write-Report 'mvp-journey-validation.md' @('# Jornada MVP v6.14.3','',"Executado em: $(Get-Date -Format o)",'','Validação automatizada: login, rotas, reload e persistência do seed.','',"Contagens (hábitos ativos|conclusões hoje|objetivos|lembretes|notificações): `$counts`",'','Interações completas de criação/customização devem ser conferidas manualmente; não são declaradas aprovadas sem navegador real.')
  Write-Report 'mvp-business-rules-validation.md' @('# Regras do MVP v6.14.3','',"Executado em: $(Get-Date -Format o)",'','- Uso do plano e páginas comerciais: cobertos pelo smoke autenticado/público.','- Limite Free, sexto hábito, template e duplicação: **pendentes de execução transacional guiada**.','- Ritmo e Evolução: nenhuma alteração comercial foi feita pelo runner.','','**P0 pendente: regras mutacionais não foram comprovadas automaticamente.**')
  New-MobileChecklist
  Write-Report 'runtime-bugfixes.md' @('# Bugs observados v6.14.3','',"Executado em: $(Get-Date -Format o)",'','Nenhum bug de runtime pode ser registrado sem uma falha observada. Consulte o relatório final e o log bruto.')
} catch {
  $failure=$_.Exception.Message
  $results['falha']='Não aprovado'
  if(-not(Test-Path (Join-Path $artifactDir 'environment-validation.md'))){Write-Report 'environment-validation.md' @('# Ambiente v6.14.3','',"Executado em: $(Get-Date -Format o)","**P0:** $failure",'','Nenhuma ferramenta foi instalada automaticamente. Nenhuma senha foi registrada.')}
  throw
} finally {
  if($app -and -not $app.HasExited){Stop-Process -Id $app.Id -Force -ErrorAction SilentlyContinue;Wait-Process -Id $app.Id -ErrorAction SilentlyContinue}
  $decision=if($failure){'Release não aprovada — P0 pendente'}else{'Release não aprovada — P0 pendente'}
  $pending=if($failure){$failure}else{'Validação manual da jornada mutacional, regras Free e checklist mobile.'}
  $rows=$results.GetEnumerator()|ForEach-Object{"| $($_.Key) | $($_.Value) |"}
  Write-Report 'final-release-candidate-report.md' (@('# Release candidate v6.14.3','',"Executado em: $(Get-Date -Format o)","- SHA inicial: `$initialSha`",'- SHA final: consultar o commit que incorpora esta evidência.','- Ambiente alvo: Windows / PowerShell 7',"- Base URL: $BaseUrl",'- Connection string: fornecida em memória; senha mascarada e não persistida.','', '| Etapa | Status |','|---|---|')+$rows+@('','## P0s pendentes',"- $pending",'','## Decisão',"**$decision**"))
}
