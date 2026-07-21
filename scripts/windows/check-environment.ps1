param([string]$PublishPath='C:\inetpub\wwwroot\habitflow',[string]$HttpsBindingHost)
. "$PSScriptRoot\_common.ps1"; $log=New-LogPath 'windows-environment-check'; $rows=@('# Windows environment check')
function Add($l,$m){ Write-Check $l $m; $script:rows += "- **$l** $m" }
if($IsWindows -or $env:OS -like '*Windows*'){Add OK 'Windows OS detectado'}else{Add AVISO 'Sistema atual não é Windows; script validará parcialmente'}
Add OK "PowerShell $($PSVersionTable.PSVersion)"
try{ $dv=& dotnet --version; Add OK "dotnet disponível: $dv" }catch{ Add ERRO 'dotnet/ASP.NET Core Runtime não encontrado; instale o .NET Hosting Bundle' }
try{ if(Get-WindowsFeature Web-Server -ErrorAction SilentlyContinue){Add OK 'IIS consultável'} else {Add AVISO 'Get-WindowsFeature indisponível fora do Windows Server'} }catch{ Add AVISO 'Não foi possível consultar IIS via Get-WindowsFeature' }
if(Get-Module -ListAvailable WebAdministration){Add OK 'IIS Management Scripts/WebAdministration disponível'}else{Add AVISO 'WebAdministration não encontrado'}
$anc=Join-Path $env:ProgramFiles 'IIS\Asp.Net Core Module\V2\aspnetcorev2.dll'; if(Test-Path $anc){Add OK 'AspNetCoreModuleV2 instalado'}else{Add AVISO 'AspNetCoreModuleV2 não localizado; confirme Hosting Bundle'}
if(Get-Command psql -ErrorAction SilentlyContinue){Add OK 'psql disponível'}else{Add AVISO 'psql não está no PATH'}
$busy = Get-NetTCPConnection -LocalPort 5097 -ErrorAction SilentlyContinue; if($busy){Add AVISO 'Porta 5097 em uso'}else{Add OK 'Porta 5097 livre'}
try{New-Item -ItemType Directory -Force $PublishPath|Out-Null; $t=Join-Path $PublishPath '.write-test'; Set-Content $t 'ok'; Remove-Item $t; Add OK "Escrita permitida em $PublishPath"}catch{Add ERRO "Sem permissão de escrita em $PublishPath"}
if($HttpsBindingHost){ try{ Get-ChildItem Cert:\LocalMachine\My | Where-Object Subject -like "*$HttpsBindingHost*" | Out-Null; Add OK "Certificados consultados para $HttpsBindingHost" }catch{Add AVISO 'Não foi possível validar certificado SSL'} }
Add AVISO 'URL Rewrite não é obrigatório para ASP.NET Core reverse proxy/IIS direto'
$rows | Set-Content $log -Encoding UTF8; Write-Host "Relatório: $log"
