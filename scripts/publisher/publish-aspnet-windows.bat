@echo off
dotnet publish src\HabitFlow.Web\HabitFlow.Web.csproj -c Release -o publish\windows
copy /Y src\HabitFlow.Web\web.config publish\windows\web.config
