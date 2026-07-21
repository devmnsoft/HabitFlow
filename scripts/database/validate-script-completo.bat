@echo off
set DB=habitflow_test
set USER=postgres
createdb -U %USER% %DB%
psql -U %USER% -d %DB% -f database/script_completo.sql
psql -U %USER% -d %DB% -c "select count(*) from habitflow.system_settings;"
