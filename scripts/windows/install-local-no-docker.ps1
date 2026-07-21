param([string]$DatabaseName='habitflow',[string]$Environment='Development')
& "$PSScriptRoot\check-environment.ps1"; $hostName=Read-Host 'PostgreSQL Host (localhost)'; if(-not $hostName){$hostName='localhost'}; $user=Read-Host 'PostgreSQL User (postgres)'; if(-not $user){$user='postgres'}
& "$PSScriptRoot\setup-postgres-database.ps1" -DatabaseName $DatabaseName -Host $hostName -User $user
if($Environment -eq 'Development'){& "$PSScriptRoot\apply-database-script.ps1" -DatabaseName $DatabaseName -Host $hostName -User $user -Environment $Environment -DevSeed}else{& "$PSScriptRoot\apply-database-script.ps1" -DatabaseName $DatabaseName -Host $hostName -User $user -Environment $Environment}
dotnet build; Write-Host 'Execute: dotnet run --project src/HabitFlow.Web/HabitFlow.Web.csproj --urls http://localhost:5097'
