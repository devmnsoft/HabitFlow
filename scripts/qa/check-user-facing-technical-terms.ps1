$ErrorActionPreference = 'Stop'
$roots = @('src/HabitFlow.Web/Views', 'src/HabitFlow.Web/wwwroot/js')
$excluded = @('Admin', 'SuperAdmin', 'Health', 'Help/DatabaseSetup.cshtml')
$terms = '(?i)\b(SaaS|tenant|multi-tenant|entitlement|webhook|checkout provider|payload|schema|PostgreSQL|stack trace)\b'
$violations = Get-ChildItem $roots -Recurse -File | Where-Object {
  $path = $_.FullName.Replace('\','/')
  -not ($excluded | Where-Object { $path -like "*/$_/*" -or $path -like "*/$_" })
} | Select-String -Pattern $terms
if ($violations) { $violations | ForEach-Object { Write-Error "$($_.Path):$($_.LineNumber): termo técnico visível" }; exit 1 }
Write-Host 'Linguagem pública validada.'
