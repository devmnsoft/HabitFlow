#requires -Version 7.0
<#
.SYNOPSIS
Runs the HabitFlow v6.14.6 release gate on a Windows development workstation.
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
$artifactDir = Join-Path $root 'artifacts/v6146'
$publishDir = Join-Path $root 'artifacts/v6146-publish'
$runtimeLog = Join-Path $artifactDir 'habitflow-runtime.log'
$runtimeErrorLog = Join-Path $artifactDir 'habitflow-runtime-error.log'
$initialSha = (git rev-parse HEAD).Trim()
$results = [ordered]@{}
$app = $null
$startedAt = Get-Date
$currentPhase = 'pré-check do Windows real'
New-Item -ItemType Directory -Force $artifactDir | Out-Null

function Write-Report([string]$Name, [string[]]$Content) {
  $Content | Set-Content (Join-Path $artifactDir $Name) -Encoding utf8
}
function Invoke-Step([string]$Name, [scriptblock]$Action) {
  $script:currentPhase=$Name
  Write-Host "`n==> $Name" -ForegroundColor Cyan
  & $Action
  if ($LASTEXITCODE) { throw "$Name failed with exit code $LASTEXITCODE." }
  $results[$Name] = 'Aprovado'
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
  $bad='Unhandled exception|InvalidOperationException|PostgresException|NpgsqlException|\bDapper\b|partial view was not found|StackTrace'
  $content=(Get-Content $runtimeLog,$runtimeErrorLog -Raw -ErrorAction SilentlyContinue) -join "`n"
  if($content -match $bad){throw 'P0: runtime log contains a forbidden exception signature.'}
}
function New-MobileChecklist {
  if($SkipMobileChecklist){Write-Report 'mobile-minimum-validation.md' @('# Checklist mobile v6.14.6','','Não executado: `-SkipMobileChecklist` foi informado.','', '**P0 pendente: validação visual não realizada.**');return}
  $viewports='1440x900','1366x768','1280x720','1024x768','768x1024','430x932','390x844','360x800','320x568'
  $screens='Dashboard','Meu Dia','Biblioteca','Hábitos','Detalhe do hábito','Objetivos','Lembretes','Revisão semanal','Relatórios','Uso do plano'
  $lines=@('# Checklist mobile v6.14.6','',"Gerado em: $(Get-Date -Format o)",'','Preencher manualmente em navegador real. Nenhuma captura foi fabricada.','','## Viewports') + ($viewports|ForEach-Object{"- [ ] $_"}) + @('','## Telas') + ($screens|ForEach-Object{"- [ ] $_ — sem overflow, CTA coberto, header quebrado, modal fora da tela ou contraste crítico"}) + @('','**P0 pendente até que todos os itens sejam conferidos.**')
  Write-Report 'mobile-minimum-validation.md' $lines
}

$failure=$null
try {
  $toolRows=[Collections.Generic.List[string]]::new()
  $missingTools=[Collections.Generic.List[string]]::new()
  if (-not $IsWindows) {
    throw 'P0: este release gate exige execução em Windows real. Execute-o em C:\MNSOFT\HabitFlow.'
  }
  foreach($tool in @(
    @('dotnet','build e execução','winget install Microsoft.DotNet.SDK.10'),
    @('pwsh','orquestração','winget install Microsoft.PowerShell'),
    @('psql','migrations e sanidades','winget install PostgreSQL.PostgreSQL'),
    @('node','checks JavaScript','winget install OpenJS.NodeJS.LTS'),
    @('npm','testes e auditoria','instalado com Node.js'),
    @('git','inventário do release','winget install Git.Git'),
    @('bash','migration runner','winget install Git.Git'))) {
    $command=Get-Command $tool[0] -ErrorAction SilentlyContinue
    if(-not $command){
      $missingTools.Add($tool[0])
      $toolRows.Add("| $($tool[0]) | Ausente | `$($tool[2])` |")
      continue
    }
    $version = switch($tool[0]){'dotnet'{(dotnet --version)}'pwsh'{(pwsh --version)}default{(& $tool[0] --version | Select-Object -First 1)}}
    $toolRows.Add("| $($tool[0]) | Aprovado | $version |")
  }
  $precheck=@('# Pré-check Windows v6.14.6','',"Executado em: $(Get-Date -Format o)",'- Sistema operacional: Windows real','- A senha da conexão não é registrada.','', '| Ferramenta | Status | Versão / instalação |','|---|---|---|')+$toolRows
  if($missingTools.Count){
    $precheck+=@('','## Instalação necessária','','```powershell','winget install Microsoft.DotNet.SDK.10','winget install Microsoft.PowerShell','winget install PostgreSQL.PostgreSQL','```','','Feche e abra um novo PowerShell e confirme:','','```powershell','where.exe dotnet','where.exe pwsh','where.exe psql','```','','Depois repita o runner. Nenhuma ferramenta foi instalada automaticamente.')
  }
  Write-Report 'windows-precheck.md' $precheck
  if($missingTools.Count){throw "P0: ferramentas ausentes: $($missingTools -join ', '). Consulte windows-precheck.md."}
  $results['Ambiente']='Aprovado'

  $currentPhase='build/publish'
  Invoke-Step 'clean' {dotnet clean HabitFlow.sln}
  Invoke-Step 'restore' {dotnet restore HabitFlow.sln}
  Invoke-Step 'build' {dotnet build HabitFlow.sln --configuration $Configuration --no-restore}
  if(Test-Path $publishDir){Remove-Item $publishDir -Recurse -Force}
  Invoke-Step 'publish' {dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj --configuration $Configuration --no-build --output $publishDir}
  Write-Report 'build-publish-validation.md' @('# Build e publish v6.14.6','',"Executado em: $(Get-Date -Format o)",'','- clean: aprovado','- restore: aprovado','- build: aprovado',"- publish: aprovado em diretório ignorado `$publishDir`")

  $currentPhase='migrations e EXPLAIN'
  Invoke-Step 'migrations' {& (Join-Path $root 'scripts/validation/validate-postgres-migrations.ps1') -ConnectionString $ConnectionString -TemporaryDatabase habitflow_v6146_fresh -ReportPath 'artifacts/v6146/postgres-migrations-validation.md'}
  $columnTypes=Invoke-Db "select column_name||'|'||data_type||'|'||udt_name from information_schema.columns where table_schema='habitflow' and table_name='habit_templates' and column_name in ('suggested_days','tags','difficulty','suggested_reminder_time','published_at') order by column_name"
  $templateExplain=Invoke-Db @"
explain select t.id as "Id", coalesce((select sum(1 << d)::int from unnest(t.suggested_days) d),127)::int as "SuggestedDays"
from habitflow.habit_templates t
join habitflow.habit_template_favorites f
  on f.template_id=t.id
where t.is_active=true and t.published_at is not null
limit 1
"@
  Write-Report 'habit-template-explain-validation.md' @('# EXPLAIN da consulta de templates favoritos v6.14.6','',"Executado em: $(Get-Date -Format o)",'','## Tipos observados','```text',$columnTypes,'```','','## Plano','```text',$templateExplain,'```','','A consulta foi aceita pelo PostgreSQL com `ON_ERROR_STOP=1`; nenhum identificador ou valor de usuário foi persistido.')
  $results['SQL templates/favoritos']='Aprovado'
  $currentPhase='startup e smoke público'
  Stop-PreviousHabitFlow
  $env:ASPNETCORE_ENVIRONMENT='Development';$env:ASPNETCORE_URLS=$BaseUrl;$env:ConnectionStrings__DefaultConnection=$ConnectionString
  $app=Start-Process dotnet -ArgumentList @((Join-Path $publishDir 'HabitFlow.Web.dll'),'--urls',$BaseUrl) -PassThru -NoNewWindow -RedirectStandardOutput $runtimeLog -RedirectStandardError $runtimeErrorLog
  $ready=$false; for($i=0;$i-lt 120;$i++){if($app.HasExited){throw "P0: HabitFlow exited during startup (code $($app.ExitCode))."};try{$r=Invoke-WebRequest "$BaseUrl/" -UseBasicParsing -MaximumRedirection 0 -ErrorAction Stop;if($r.StatusCode-lt500){$ready=$true;break}}catch{};Start-Sleep 1}
  if(-not $ready){throw 'P0: startup timeout after 120 seconds.'};Assert-RuntimeLog;$results['startup']='Aprovado'
  Write-Report 'startup-runtime-log-validation.md' @('# Startup v6.14.6','',"Executado em: $(Get-Date -Format o)","- URL: $BaseUrl",'- Aplicação publicada respondeu em até 120 segundos.','- Log verificado contra assinaturas técnicas proibidas.')

  $publicRows=[Collections.Generic.List[string]]::new();foreach($route in '/','/login','/register','/plans','/service-worker.js','/favicon.ico'){$r=Invoke-WebRequest "$BaseUrl$route" -UseBasicParsing -MaximumRedirection 5;if($r.StatusCode-ge500){throw "P0: GET $route returned $($r.StatusCode)."};$publicRows.Add("| `$route` | $($r.StatusCode) | Aprovado |")};Assert-RuntimeLog;$results['smoke público']='Aprovado'
  Write-Report 'public-smoke-validation.md' (@('# Smoke público v6.14.6','',"Executado em: $(Get-Date -Format o)",'','| Rota | HTTP | Status |','|---|---:|---|')+$publicRows)

  $currentPhase='provisionamento, seed e smoke autenticado'
  $bytes=New-Object byte[] 24;[Security.Cryptography.RandomNumberGenerator]::Fill($bytes);$securePassword=ConvertTo-SecureString (([Convert]::ToBase64String($bytes))+'!aA1') -AsPlainText -Force
  & scripts/dev/provision-dev-user.ps1 -BaseUrl $BaseUrl -Email $DevEmail -Password $securePassword
  & scripts/dev/seed-demo-data.ps1 -Email $DevEmail -ConnectionString $ConnectionString
  $ids=(Invoke-Db "select h.id,(select id from habitflow.habit_templates where is_active=true and published_at is not null order by sort_order nulls last limit 1),(select id from habitflow.user_goals where user_id=u.id order by title limit 1) from habitflow.users u join habitflow.habits h on h.user_id=u.id where lower(u.email)=lower('$($DevEmail.Replace("'","''"))') order by h.sort_order limit 1") -split '\|'
  if($ids.Count-ne3 -or $ids -contains ''){throw 'P0: seed IDs could not be resolved.'}
  Write-Report 'dev-user-seed-validation.md' @('# Usuário e seed v6.14.6','',"Executado em: $(Get-Date -Format o)","- Usuário: $DevEmail",'- Senha aleatória permaneceu somente em memória como SecureString.','- Seed idempotente aplicado e IDs resolvidos.')
  & scripts/validation/smoke-authenticated-routes.ps1 -BaseUrl $BaseUrl -Email $DevEmail -Password $securePassword -HabitId $ids[0] -TemplateId $ids[1] -GoalId $ids[2] -ReportPath 'artifacts/v6146/authenticated-smoke-validation.md';$results['smoke autenticado']='Aprovado';Assert-RuntimeLog
  Write-Report 'habit-library-manual-validation.md' @('# Biblioteca de hábitos v6.14.6','',"Gerado em: $(Get-Date -Format o)",'- Rotas de biblioteca, favoritos, detalhe e customização: aprovadas pelo smoke autenticado.','- Favoritar/desfavoritar, filtros, criação por template e reload: pendentes de conferência manual em navegador real.','- Nenhuma aprovação manual ou screenshot foi fabricada.','','**P0 pendente até a conferência manual.**')

  $counts=(Invoke-Db "select count(*) filter(where not h.is_archived),count(*) filter(where hc.completed_date=current_date),count(distinct g.id),count(distinct r.id),count(distinct n.id) from habitflow.users u left join habitflow.habits h on h.user_id=u.id left join habitflow.habit_completions hc on hc.habit_id=h.id left join habitflow.user_goals g on g.user_id=u.id left join habitflow.habit_reminders r on r.user_id=u.id left join habitflow.notifications n on n.user_id=u.id where lower(u.email)=lower('$($DevEmail.Replace("'","''"))')")
  Write-Report 'mvp-essential-journey-validation.md' @('# Jornada MVP v6.14.6','',"Executado em: $(Get-Date -Format o)",'','Validação automatizada: login, rotas, reload e persistência do seed.','',"Contagens (hábitos ativos|conclusões hoje|objetivos|lembretes|notificações): `$counts`",'','Interações completas de criação/customização devem ser conferidas manualmente; não são declaradas aprovadas sem navegador real.')
  Write-Report 'mvp-plan-rules-validation.md' @('# Regras do MVP v6.14.6','',"Executado em: $(Get-Date -Format o)",'','- Uso do plano e páginas comerciais: cobertos pelo smoke autenticado/público.','- Limite Free, sexto hábito, template e duplicação: **pendentes de execução transacional guiada**.','- Ritmo e Evolução: nenhuma alteração comercial foi feita pelo runner.','','**P0 pendente: regras mutacionais não foram comprovadas automaticamente.**')
  New-MobileChecklist
  Write-Report 'first-runtime-failure-fix.md' @('# Bugs observados v6.14.6','',"Executado em: $(Get-Date -Format o)",'','Nenhum bug de runtime pode ser registrado sem uma falha observada. Consulte o relatório final e o log bruto.')
} catch {
  $failure=$_.Exception.Message
  $results['falha']='Não aprovado'
  if(-not(Test-Path (Join-Path $artifactDir 'windows-precheck.md'))){Write-Report 'windows-precheck.md' @('# Ambiente v6.14.6','',"Executado em: $(Get-Date -Format o)","**P0:** $failure",'','Nenhuma ferramenta foi instalada automaticamente. Nenhuma senha foi registrada.')}
  Write-Report 'first-runtime-failure-fix.md' @('# Primeira falha real v6.14.6','',"Executado em: $(Get-Date -Format o)","- Fase: $currentPhase",'- Status: falha preservada; nenhuma exceção foi mascarada.','','## Erro bruto','```text',$failure,'```','','## Causa e correção','A causa deve ser diagnosticada no Windows real a partir deste erro. Nenhuma correção de aplicação é declarada sem reprodução e reexecução.')
  throw
} finally {
  if($app -and -not $app.HasExited){Stop-Process -Id $app.Id -Force -ErrorAction SilentlyContinue;Wait-Process -Id $app.Id -ErrorAction SilentlyContinue}
  $decision=if($failure){'Release não aprovada — P0 pendente'}else{'Release não aprovada — P0 pendente'}
  $pending=if($failure){$failure}else{'Validação manual da jornada mutacional, regras Free e checklist mobile.'}
  $rows=$results.GetEnumerator()|ForEach-Object{"| $($_.Key) | $($_.Value) |"}
  $runnerStatus=if($failure){'Interrompido'}else{'Concluído com P0s manuais pendentes'}
  Write-Report 'windows-runner-execution.md' @('# Execução do runner Windows v6.14.6','',"- Início: $($startedAt.ToString('o'))","- Fim: $((Get-Date).ToString('o'))",'- Ambiente: Windows real / PowerShell 7',"- Status: $runnerStatus","- Fase final: $currentPhase",'- Connection string: fornecida somente por `HABITFLOW_LOCAL_CONNECTION`; valor não persistido.','','## Comando','```powershell','pwsh .\scripts\validation\run-release-candidate-local-windows.ps1 `','  -BaseUrl "http://localhost:5097" `','  -ConnectionString $env:HABITFLOW_LOCAL_CONNECTION `','  -DevEmail "release-gate@habitflow.local"','```','','## Primeiro erro',$(if($failure){"```text`n$failure`n```"}else{'Nenhum erro técnico observado pelo runner; permanecem validações manuais.'}),'','## Próximo passo',$(if($failure){'Corrigir a causa raiz indicada pelo primeiro erro e repetir o mesmo comando.'}else{'Concluir jornada mutacional, regras de planos e inspeção mobile em navegador real.'}))
  Write-Report 'final-release-candidate-report.md' (@('# Release candidate v6.14.6','',"Executado em: $(Get-Date -Format o)","- SHA inicial: `$initialSha`",'- SHA final: consultar o commit que incorpora esta evidência.','- Ambiente alvo: Windows / PowerShell 7',"- Base URL: $BaseUrl",'- Connection string: fornecida em memória; senha mascarada e não persistida.','', '| Etapa | Status |','|---|---|')+$rows+@('','## P0s pendentes',"- $pending",'','## Decisão',"**$decision**"))
}
