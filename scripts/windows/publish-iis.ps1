[CmdletBinding()] param([string]$Output='artifacts/v6120/iis-publish',[string]$Configuration='Release')
$ErrorActionPreference='Stop';. "$PSScriptRoot/_common.ps1";$root=Get-RepoRoot;$target=Join-Path $root $Output
if(git -C $root ls-files 'src/HabitFlow.Web/appsettings.Production.json'){throw 'appsettings.Production.json real não pode estar versionado.'}
New-Item -ItemType Directory -Force $target|Out-Null
dotnet publish (Join-Path $root 'src/HabitFlow.Web/HabitFlow.Web.csproj') --configuration $Configuration --output $target
if($LASTEXITCODE){throw 'dotnet publish para IIS falhou.'}
@'
# Checklist do pacote IIS
- [ ] App Pool x64, **No Managed Code**, identidade dedicada e `Load User Profile=true`.
- [ ] .NET 10 Hosting Bundle instalado; IIS reiniciado depois da instalação.
- [ ] `ASPNETCORE_ENVIRONMENT` e `ConnectionStrings__DefaultConnection` fora do pacote/Git.
- [ ] Data Protection keys persistentes, criptografadas e fora do Git.
- [ ] identidade do App Pool com leitura/execução; escrita somente nos diretórios operacionais.
- [ ] stdout log habilitado apenas para diagnóstico e com rotação.
- [ ] migrations canônicas aplicadas uma única vez antes do switch.
- [ ] `GET /health` validado após o deploy.
'@|Set-Content (Join-Path $target 'IIS-CHECKLIST.md') -Encoding utf8
Write-Check OK "Pacote IIS criado: $target"
