[CmdletBinding()] param([string]$BaseUrl='http://localhost:5097',[Parameter(Mandatory)][string]$Email,[Security.SecureString]$Password)
$ErrorActionPreference='Stop'; $root=(Resolve-Path (Join-Path $PSScriptRoot '../..')).Path; $report=Join-Path $root 'artifacts/v6131/smoke-routes-report.md'; New-Item -ItemType Directory -Force (Split-Path $report)|Out-Null
if(-not $Password){$Password=Read-Host 'Development user password' -AsSecureString}
$plain=[Net.NetworkCredential]::new('', $Password).Password; $session=New-Object Microsoft.PowerShell.Commands.WebRequestSession; $rows=[Collections.Generic.List[string]]::new()
try {
  $login=Invoke-WebRequest "$BaseUrl/login" -WebSession $session -UseBasicParsing
  $token=[regex]::Match($login.Content,'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
  if(-not $token){throw 'Antiforgery token not found on login page.'}
  $response=Invoke-WebRequest "$BaseUrl/login" -Method Post -WebSession $session -Body @{Email=$Email;Password=$plain;__RequestVerificationToken=$token} -UseBasicParsing -MaximumRedirection 5
  if($response.BaseResponse.ResponseUri.AbsolutePath -eq '/login'){throw 'Login did not create an authenticated session.'}
  foreach($route in '/dashboard','/my-day','/habits','/habits/create','/goals','/habit-library','/onboarding','/reminders','/notifications','/reports','/plans','/account/plan/usage','/account/privacy','/profile','/profile/accessibility'){
    try{$r=Invoke-WebRequest "$BaseUrl$route" -WebSession $session -UseBasicParsing -MaximumRedirection 5;if($r.StatusCode -ge 500 -or $r.Content -match '(Internal Server Error|Erro 500)'){throw 'server error content'};$rows.Add("| `$route` | Aprovado | HTTP $($r.StatusCode), conteúdo recebido |")}
    catch{$rows.Add("| `$route` | Falhou | $($_.Exception.Message -replace '\|','/') |")}
  }
  @('# Smoke de rotas autenticadas v6.13.1','',"Executado em: $(Get-Date -Format o)",'','| Rota | Status | Evidência |','|---|---|---|')+$rows|Set-Content $report -Encoding utf8
  if($rows -match 'Falhou'){throw 'One or more authenticated routes failed.'}
} finally {$plain=$null}
