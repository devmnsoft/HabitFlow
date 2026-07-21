@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0validate-db.ps1" %*
exit /b %ERRORLEVEL%
