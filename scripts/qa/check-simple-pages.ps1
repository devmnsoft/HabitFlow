$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$controller=Get-Content "$root/src/HabitFlow.Web/Controllers/SuperAdminController.cs" -Raw
if($controller -match 'Payments\(.*Simple\.cshtml|Overdue\(.*Simple\.cshtml|Subscriptions\(.*Simple\.cshtml|Plans\(.*Simple\.cshtml|Audit\(.*Simple\.cshtml|System\(.*Simple\.cshtml'){ throw 'Rota operacional SuperAdmin usando Simple.cshtml' }
Write-Host 'Simple pages OK'
