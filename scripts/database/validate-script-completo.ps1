param(
    [string]$Database = "habitflow_test",
    [string]$User = "postgres"
)

$ErrorActionPreference = "Stop"
createdb -U $User $Database 2>$null
psql -U $User -d $Database -f "database/script_completo.sql"
psql -U $User -d $Database -c "select count(*) from habitflow.system_settings;"
