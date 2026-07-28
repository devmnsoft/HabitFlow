$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '../..')
$required = @(
 'src/HabitFlow.Application/DTOs/Progress/ProgressCalendarModels.cs',
 'src/HabitFlow.Application/DTOs/Progress/ProgressCalendarRows.cs',
 'src/HabitFlow.Application/Abstractions/Progress/IProgressCalendarRepository.cs',
 'src/HabitFlow.Application/Services/ProgressCalendarService.cs',
 'src/HabitFlow.Application/Services/UserTimeZoneService.cs')
foreach ($path in $required) { if (-not (Test-Path (Join-Path $root $path))) { throw "Missing contract: $path" } }
$tracked = git -C $root ls-files
if ($tracked -match '(^|/)(bin|obj)/') { throw 'Generated bin/obj output is tracked.' }
$interfaceCount = (rg -l 'interface IProgressCalendarRepository' (Join-Path $root 'src')).Count
if ($interfaceCount -ne 1) { throw "Expected one repository interface, found $interfaceCount." }
if (rg -l 'class ProgressCalendarViewModel|record ProgressCalendarViewModel' (Join-Path $root 'src/HabitFlow.Web')) { throw 'Web contains a progress ViewModel copy.' }
$files = $required | ForEach-Object { Get-Content (Join-Path $root $_) -Raw }
if ($files | Where-Object { $_ -notmatch 'namespace HabitFlow.Application;' }) { throw 'Progress contract has an unexpected namespace.' }
$infraProject = Get-Content (Join-Path $root 'src/HabitFlow.Infrastructure/HabitFlow.Infrastructure.csproj') -Raw
if ($infraProject -notmatch 'HabitFlow.Application.csproj') { throw 'Infrastructure does not reference Application.' }
Write-Host 'Progress compile contract is consistent.'
