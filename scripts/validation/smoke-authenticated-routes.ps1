# Usage (PowerShell 7+): ./scripts/validation/smoke-authenticated-routes.ps1
#   -Email user@example.test [-BaseUrl http://localhost:5097] [-Password (Read-Host -AsSecureString)]
#   [-HabitId <guid>] [-TemplateId <guid>] [-GoalId <guid>]
[CmdletBinding()] param(
  [string]$BaseUrl='http://localhost:5097',
  [Parameter(Mandatory)][string]$Email,
  [Security.SecureString]$Password,
  [guid]$HabitId,
  [guid]$TemplateId,
  [guid]$GoalId,
  [string]$ReportPath='artifacts/v6137/authenticated-smoke-validation.md'
)
$ErrorActionPreference='Stop'; $root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path; $report=if([IO.Path]::IsPathRooted($ReportPath)){$ReportPath}else{Join-Path $root $ReportPath}; New-Item -ItemType Directory -Force (Split-Path $report)|Out-Null
if(-not $Password -and $env:HABITFLOW_SMOKE_PASSWORD){$Password=ConvertTo-SecureString $env:HABITFLOW_SMOKE_PASSWORD -AsPlainText -Force}
if(-not $Password){
  if(-not [Environment]::UserInteractive){throw 'Password is required in non-interactive environments. Pass -Password or set HABITFLOW_SMOKE_PASSWORD.'}
  $Password=Read-Host 'Development user password' -AsSecureString
}
$plain=[Net.NetworkCredential]::new('', $Password).Password; $session=New-Object Microsoft.PowerShell.Commands.WebRequestSession; $rows=[Collections.Generic.List[string]]::new()
try {
  $login=Invoke-WebRequest "$BaseUrl/login" -WebSession $session -UseBasicParsing
  $token=[regex]::Match($login.Content,'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
  if(-not $token){throw 'Antiforgery token not found on login page.'}
  $response=Invoke-WebRequest "$BaseUrl/login" -Method Post -WebSession $session -Body @{Email=$Email;Password=$plain;__RequestVerificationToken=$token} -UseBasicParsing -MaximumRedirection 5
  if($response.BaseResponse.ResponseUri.AbsolutePath -eq '/login'){throw 'Login did not create an authenticated session.'}
  $routes=[Collections.Generic.List[string]]::new()
  @('/dashboard','/my-day','/habits','/habits/create','/habit-library','/habit-library?favoritesOnly=true','/onboarding','/goals','/goals/create','/reminders','/notifications','/weekly-review','/reports','/account/plan/usage','/account/privacy','/profile','/profile/accessibility') | ForEach-Object {$routes.Add($_)}
  if($HabitId -ne [guid]::Empty){$routes.Add("/habits/$HabitId");$routes.Add("/habits/$HabitId/edit")}
  if($TemplateId -ne [guid]::Empty){$routes.Add("/habit-library/templates/$TemplateId");$routes.Add("/habit-library/templates/$TemplateId/customize")}
  if($GoalId -ne [guid]::Empty){$routes.Add("/goals/$GoalId")}
  foreach($route in $routes){
    try{$r=Invoke-WebRequest "$BaseUrl$route" -WebSession $session -UseBasicParsing -MaximumRedirection 5;if($r.StatusCode -ge 500 -or $r.Content -match '(Internal Server Error|Erro 500|System\.[A-Za-z]+Exception|NpgsqlException)'){throw 'server error or technical exception content'};if($r.BaseResponse.ResponseUri.AbsolutePath -eq '/login'){throw 'route redirected to login'};$reload=Invoke-WebRequest "$BaseUrl$route" -WebSession $session -UseBasicParsing -MaximumRedirection 5;if($reload.StatusCode -ne 200){throw "reload returned HTTP $($reload.StatusCode)"};$rows.Add("| `$route` | Aprovado | HTTP $($r.StatusCode), reload HTTP $($reload.StatusCode) |")}
    catch{$rows.Add("| `$route` | Falhou | $($_.Exception.Message -replace '\|','/') |")}
  }
  @('# Smoke de rotas autenticadas','',"Executado em: $(Get-Date -Format o)",'','| Rota | Status | Evidência |','|---|---|---|')+$rows|Set-Content $report -Encoding utf8
  if($rows -match 'Falhou'){throw 'One or more authenticated routes failed.'}
} finally {$plain=$null}
