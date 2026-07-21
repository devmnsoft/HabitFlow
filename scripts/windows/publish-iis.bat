@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0publish-iis.ps1" %*
