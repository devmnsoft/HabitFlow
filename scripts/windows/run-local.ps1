[CmdletBinding()] param([int]$Port=5097)
$ErrorActionPreference='Stop'; . "$PSScriptRoot/_common.ps1"; $root=Get-RepoRoot
if(Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue){throw "Porta $Port em uso. Encerre o processo antes de iniciar."}
$env:ASPNETCORE_ENVIRONMENT='Development'; $url="http://localhost:$Port"
Write-Host "HabitFlow em $url (Development). Pressione Ctrl+C para parar."
Push-Location $root; try{dotnet run --project 'src/HabitFlow.Web/HabitFlow.Web.csproj' --urls $url;if($LASTEXITCODE){throw "Aplicação encerrou com código $LASTEXITCODE; consulte o log acima."}}finally{Pop-Location}
