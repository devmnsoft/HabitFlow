[CmdletBinding()] param([string]$BaseUrl='http://localhost:5097')
$ErrorActionPreference='Stop';. "$PSScriptRoot/_common.ps1";$root=Get-RepoRoot;$dir=Join-Path $root 'tests/HabitFlow.Playwright';$auth=Join-Path $root 'playwright/.auth/user.json';New-Item -ItemType Directory -Force (Split-Path $auth)|Out-Null
Write-Host 'Uma janela será aberta. Autentique-se manualmente; este script nunca solicita nem grava a senha.'
$env:HABITFLOW_AUTH_OUTPUT=$auth;$env:HABITFLOW_BASE_URL=$BaseUrl
Push-Location $dir;try{npx playwright test auth-state.setup.js --headed --project=auth-setup;if($LASTEXITCODE){throw 'Geração do auth state falhou.'}}finally{Pop-Location}
Write-Host "Auth state salvo localmente e ignorado pelo Git: $auth";Write-Host "Defina HABITFLOW_AUTH_STORAGE com esse caminho para testar rotas autenticadas."
