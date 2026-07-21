@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0seed-dev.ps1" %*
exit /b %ERRORLEVEL%
