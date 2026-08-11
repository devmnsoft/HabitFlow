[CmdletBinding()] param([int]$Port = 5097)
$ErrorActionPreference = 'Stop'; . "$PSScriptRoot/_common.ps1"; $root = Get-RepoRoot
$problems = [System.Collections.Generic.List[string]]::new()
function Test-Tool($Name,$Hint) { if(-not(Get-Command $Name -ErrorAction SilentlyContinue)){$problems.Add("$Name ausente. $Hint")}else{Write-Check OK "$Name disponível"} }
Test-Tool dotnet 'Instale: winget install Microsoft.DotNet.SDK.10 — https://dotnet.microsoft.com/download/dotnet/10.0'
Test-Tool node 'Instale: winget install OpenJS.NodeJS.LTS — https://nodejs.org/'
Test-Tool npm 'É instalado com o Node.js.'
Test-Tool psql 'Instale o cliente: winget install PostgreSQL.PostgreSQL — https://www.postgresql.org/download/windows/'
if(Get-Command dotnet -ErrorAction SilentlyContinue){if(-not((dotnet --list-sdks)-match '^10\.')){$problems.Add('.NET SDK 10 ausente. Execute: winget install Microsoft.DotNet.SDK.10')}}
if(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue){$problems.Add("Porta $Port ocupada. Encerre o processo que está escutando nela.")}else{Write-Check OK "Porta $Port livre"}
$settings=Join-Path $root 'src/HabitFlow.Web/appsettings.Development.json'
if(-not(Test-Path $settings)){$problems.Add("Arquivo ausente: $settings")}else{$json=Get-Content $settings -Raw|ConvertFrom-Json;if([string]::IsNullOrWhiteSpace($json.ConnectionStrings.DefaultConnection)){$problems.Add('ConnectionStrings:DefaultConnection ausente. Configure-a por variável de ambiente ou arquivo .local ignorado.')}else{Write-Check OK 'ConnectionStrings configurada (valor não exibido).'}}
$pw=Join-Path $root 'tests/HabitFlow.Playwright/node_modules/@playwright/test'
if(-not(Test-Path $pw)){$problems.Add('Playwright ausente. Execute: cd tests/HabitFlow.Playwright; npm ci; npx playwright install')}else{Write-Check OK 'Dependências Playwright presentes; execute npx playwright install para garantir os browsers.'}
if($problems.Count){$problems|ForEach-Object{Write-Check ERRO $_};throw "Ambiente incompleto: $($problems.Count) item(ns). Nenhuma configuração ou segredo foi alterado."}
Write-Check OK 'Ambiente pronto; nenhum segredo foi criado ou exibido.'
