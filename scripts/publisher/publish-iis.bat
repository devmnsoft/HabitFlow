@echo off
setlocal
cd /d "%~dp0\..\.."
node scripts\publisher\publish-iis.js %*
