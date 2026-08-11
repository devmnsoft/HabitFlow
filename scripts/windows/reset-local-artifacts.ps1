[CmdletBinding(SupportsShouldProcess)] param()
. "$PSScriptRoot/_common.ps1";$root=Get-RepoRoot
@('artifacts/v6120','tests/HabitFlow.Playwright/test-results','tests/HabitFlow.Playwright/playwright-report')|ForEach-Object{$p=Join-Path $root $_;if((Test-Path $p)-and$PSCmdlet.ShouldProcess($p,'Remover')){Remove-Item $p -Recurse -Force}}
Write-Host 'Artefatos removidos; playwright/.auth foi preservado.'
