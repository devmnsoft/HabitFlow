[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ConnectionString,
    [string[]] $SeedFiles = @(
        "database/seed_dev.sql",
        "database/seed_production_minimal.sql",
        "database/script_completo.sql",
        "database/script_completo_dev.sql"
    )
)

$ErrorActionPreference = "Stop"
if (-not (Get-Command psql -ErrorAction SilentlyContinue)) {
    throw "psql is required: this check reads PostgreSQL metadata and is not a textual substitute for a database test."
}

$metadataSql = @"
select n.nspname || '.' || c.relname
from pg_class c
join pg_namespace n on n.oid = c.relnamespace
join pg_index i on i.indrelid = c.oid and i.indisprimary
join pg_attribute a on a.attrelid = c.oid and a.attnum = any(i.indkey)
join pg_type t on t.oid = a.atttypid
where n.nspname = 'habitflow'
  and t.typname = 'uuid'
  and a.attnotnull
  and not a.atthasdef
  and not a.attisdropped;
"@

$requiredIdTables = @(& psql $ConnectionString -X -qAt -v ON_ERROR_STOP=1 -c $metadataSql)
if ($LASTEXITCODE -ne 0) { throw "Unable to inspect PostgreSQL seed metadata." }

$failures = [System.Collections.Generic.List[string]]::new()
foreach ($file in $SeedFiles) {
    if (-not (Test-Path $file)) { continue }
    $sql = Get-Content $file -Raw
    foreach ($table in $requiredIdTables) {
        $escaped = [regex]::Escape($table)
        $matches = [regex]::Matches($sql, "(?is)insert\s+into\s+$escaped\s*\((?<columns>[^)]*)\)")
        foreach ($match in $matches) {
            $columns = $match.Groups['columns'].Value -split ',' | ForEach-Object { $_.Trim(' ', '"', "`r", "`n", "`t").ToLowerInvariant() }
            if ($columns -notcontains 'id') { $failures.Add("${file}: INSERT into ${table} omits required UUID id") }
        }
    }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Error $_ -ErrorAction Continue }
    exit 1
}
Write-Host "Seed UUID audit passed for $($requiredIdTables.Count) table(s) without UUID defaults."
