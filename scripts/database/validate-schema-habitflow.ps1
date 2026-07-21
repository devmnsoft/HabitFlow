param([string]$Database = "habitflow", [string]$User = "postgres")
$ErrorActionPreference = "Stop"
psql -U $User -d $Database -f "database/validate_schema_habitflow.sql"
