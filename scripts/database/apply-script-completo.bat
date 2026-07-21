@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0apply-script-completo.ps1" %*
exit /b %ERRORLEVEL%
