$ErrorActionPreference = "Stop"
$patterns = @('from users','from habits','insert into users','insert into habits','update users','delete from users','create table users','create table public.users')
$found = $false
foreach ($pattern in $patterns) {
  $result = rg -n -i --glob '!bin/**' --glob '!obj/**' --glob '!publish/**' --glob '!node_modules/**' --glob '!scripts/database/check-sql-schema-prefix.*' --glob '!docs/AUDITORIA_SCHEMA_HABITFLOW_V4_5.md' $pattern src database tests
  if ($LASTEXITCODE -eq 0) { $found = $true; $result }
}
if ($found) { Write-Error "SQL sem schema habitflow encontrado." }
Write-Host "OK: nenhum padrão SQL proibido encontrado."
