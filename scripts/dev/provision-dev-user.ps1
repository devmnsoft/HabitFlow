# Usage (Development only, PowerShell 7+): ./scripts/dev/provision-dev-user.ps1
#   [-BaseUrl http://localhost:5097] [-Email demo@habitflow.local] [-Password (Read-Host -AsSecureString)]
[CmdletBinding()] param([string]$BaseUrl='http://localhost:5097',[string]$Email='demo@habitflow.local',[Security.SecureString]$Password)
$ErrorActionPreference='Stop'
if($env:ASPNETCORE_ENVIRONMENT -ne 'Development'){throw 'This helper only runs when ASPNETCORE_ENVIRONMENT=Development.'}
$generated=$false
if(-not $Password){$bytes=New-Object byte[] 24; [Security.Cryptography.RandomNumberGenerator]::Fill($bytes); $Password=ConvertTo-SecureString (([Convert]::ToBase64String($bytes))+'!aA1') -AsPlainText -Force; $generated=$true}
$plain=[Net.NetworkCredential]::new('', $Password).Password; $session=New-Object Microsoft.PowerShell.Commands.WebRequestSession
try {
  $page=Invoke-WebRequest "$BaseUrl/register" -WebSession $session -UseBasicParsing
  $token=[regex]::Match($page.Content,'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
  if(-not $token){throw 'Antiforgery token not found. Is HabitFlow running?'}
  $body=@{__RequestVerificationToken=$token;ClientPersonType='NaturalPerson';DocumentType='CPF';Document='52998224725';Name='Usuário Demo';Email=$Email;Password=$plain;ConfirmPassword=$plain;AcceptedTerms='true';AcceptedPrivacy='true'}
  $result=Invoke-WebRequest "$BaseUrl/register" -Method Post -WebSession $session -Body $body -UseBasicParsing -MaximumRedirection 5
  if($result.Content -match 'CPF/CNPJ|já existe'){Write-Host 'Development identity already exists; no credential was changed.'} else {Write-Host "Development user created: $Email"; if($generated){Write-Host 'A random password was generated in memory and was not displayed.'}}
  Write-Host "Run seed-demo-data.ps1 -Email '$Email' next. The user is tenant-bound and is not SuperAdmin."
} finally {$plain=$null}
