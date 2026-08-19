# Provisions a tenant-bound Free user through the same HTTP registration path used by the product.
# The caller keeps the ephemeral password in HABITFLOW_CI_PASSWORD; this script never prints it.
[CmdletBinding()] param(
  [string]$BaseUrl = 'http://127.0.0.1:5097',
  [string]$Email = 'release-gate@habitflow.local'
)
$ErrorActionPreference = 'Stop'

if ($env:CI -ne 'true' -and $env:ASPNETCORE_ENVIRONMENT -ne 'Development') {
  throw 'CI user provisioning is allowed only in CI or Development.'
}
if ([string]::IsNullOrWhiteSpace($env:HABITFLOW_CI_PASSWORD)) {
  throw 'HABITFLOW_CI_PASSWORD must contain an ephemeral password.'
}

$session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()
$password = $env:HABITFLOW_CI_PASSWORD
try {
  $page = Invoke-WebRequest "$BaseUrl/register" -WebSession $session -UseBasicParsing
  $token = [regex]::Match($page.Content, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
  if (-not $token) { throw 'Antiforgery token was not found on the registration page.' }

  # Valid test CPF; it is synthetic data used only in the disposable CI database.
  $body = @{
    __RequestVerificationToken = $token
    ClientPersonType = 'NaturalPerson'; DocumentType = 'CPF'; Document = '52998224725'
    Name = 'HabitFlow CI'; Email = $Email; Password = $password; ConfirmPassword = $password
    AcceptedTerms = 'true'; AcceptedPrivacy = 'true'
  }
  $result = Invoke-WebRequest "$BaseUrl/register" -Method Post -WebSession $session -Body $body -UseBasicParsing -MaximumRedirection 5
  if ($result.StatusCode -ge 500 -or $result.Content -match '(Internal Server Error|NpgsqlException|PostgresException)') {
    throw 'CI user registration returned a server error.'
  }
  Write-Host "CI Free user provisioned for authenticated smoke: $Email"
} finally {
  $password = $null
  $body = $null
}
