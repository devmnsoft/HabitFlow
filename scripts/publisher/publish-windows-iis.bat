@echo off
set OUTPUT=publish\windows
dotnet publish src\HabitFlow.Web\HabitFlow.Web.csproj -c Release -o %OUTPUT%
copy src\HabitFlow.Web\web.config %OUTPUT%\web.config /Y
echo HabitFlow ASP.NET Core publicado em %OUTPUT% > %OUTPUT%\publish-report.txt
