@echo off
set HOSTNAME_ARG=%1
if "%HOSTNAME_ARG%"=="" set HOSTNAME_ARG=localhost
set PORT_ARG=%2
if "%PORT_ARG%"=="" set PORT_ARG=5432
set DB_ARG=%3
if "%DB_ARG%"=="" set DB_ARG=habitflow
set USER_ARG=%4
if "%USER_ARG%"=="" set USER_ARG=postgres
psql -h %HOSTNAME_ARG% -p %PORT_ARG% -U %USER_ARG% -d %DB_ARG% -v ON_ERROR_STOP=1 -c "select 1"
if errorlevel 1 echo ERRO conexao: Nao foi possivel conectar ao PostgreSQL. && exit /b 1
echo OK: Conexao com PostgreSQL validada.
