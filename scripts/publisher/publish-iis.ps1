param(
  [Parameter(ValueFromRemainingArguments = $true)]
  [string[]]$PublisherArgs
)

$ErrorActionPreference = "Stop"
$Root = Resolve-Path (Join-Path $PSScriptRoot "..\..")
Push-Location $Root
try {
  node scripts/publisher/publish-iis.js @PublisherArgs
} finally {
  Pop-Location
}
