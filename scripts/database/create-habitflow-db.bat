@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0create-habitflow-db.ps1" %*
exit /b %ERRORLEVEL%
