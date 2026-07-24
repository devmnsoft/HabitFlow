$ErrorActionPreference='Stop'
$root=(Resolve-Path "$PSScriptRoot/../..").Path
$migrate=Get-Content "$root/database/migrate.sql" -Raw
1..29 | ForEach-Object { $id='{0:D3}' -f $_; if($migrate -notmatch "database/migrations/$id") { throw "Migration $id ausente do migrate.sql" } }
if($migrate -match '\\i\s+migrations/') { throw 'migrate.sql usa caminho quebrado migrations/' }
foreach($name in 'client_onboarding','client_communications','job_execution_logs','user_invites','client_invoices','client_subscriptions','schema_migrations'){ if((Get-Content "$root/database/script_completo.sql" -Raw) -notmatch $name -and (Get-Content "$root/database/migrations/029_operational_completeness_v61.sql" -Raw) -notmatch $name){ throw "Tabela $name não encontrada nos scripts" } }
Write-Host 'Database scripts v6.1 OK'
