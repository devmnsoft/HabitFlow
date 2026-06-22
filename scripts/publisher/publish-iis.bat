@echo off
setlocal
cd /d "%~dp0\..\.."
echo ========================================
echo HabitFlow - Publicador IIS
echo ========================================
call npm run publish:iis:nozip
echo.
echo Pacote final: %CD%\publish\iis\HabitFlow-IIS
echo.
pause
