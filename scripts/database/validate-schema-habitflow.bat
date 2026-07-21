@echo off
set DB=%1
if "%DB%"=="" set DB=habitflow
psql -U postgres -d %DB% -f database\validate_schema_habitflow.sql
