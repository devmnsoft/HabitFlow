param([string]$Output="publish/windows",[string]$Target="")
$ErrorActionPreference="Stop"
dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj -c Release -o $Output
Copy-Item src/HabitFlow.Web/web.config $Output -Force
if($Target){New-Item -ItemType Directory -Force -Path $Target | Out-Null; Copy-Item "$Output\*" $Target -Recurse -Force}
"HabitFlow ASP.NET Core publicado em $Output" | Out-File "$Output\publish-report.txt"
