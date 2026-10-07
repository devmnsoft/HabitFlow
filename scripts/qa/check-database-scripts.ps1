$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$migrate=Get-Content "$root/database/migrate.sql" -Raw
if($migrate -match '\\i\s+migrations/') { throw 'migrate.sql usa caminho quebrado migrations/' }
$aggregate=Get-Content "$root/database/script_completo.sql" -Raw
Get-ChildItem "$root/database/migrations" -Filter '*.sql' | ForEach-Object {
    if($migrate -notmatch [regex]::Escape("database/migrations/$($_.Name)")) {
        throw "Migration $($_.Name) ausente do migrate.sql"
    }
    if($aggregate -notmatch [regex]::Escape("-- BEGIN include database/migrations/$($_.Name)")) {
        throw "Migration $($_.Name) ausente do script_completo.sql"
    }
}
Write-Host 'Database scripts valid: every migration is included in both runners'
