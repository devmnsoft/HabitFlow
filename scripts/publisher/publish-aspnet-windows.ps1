$ErrorActionPreference='Stop'
dotnet publish src/HabitFlow.Web/HabitFlow.Web.csproj -c Release -o publish/windows
Copy-Item src/HabitFlow.Web/web.config publish/windows/web.config -Force
Write-Host 'Publicado em publish/windows sem ZIP.'
