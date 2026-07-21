param([string]$TargetPath='C:\inetpub\wwwroot\habitflow',[string]$Configuration='Release',[switch]$SkipTests,[switch]$NoBackup,[switch]$DryRun,[switch]$OpenFolder,[string]$Confirm)
. "$PSScriptRoot\_common.ps1"; $root=Get-RepoRoot; Set-Location $root; $log=New-LogPath 'iis-publish'
if((git ls-files 'src/HabitFlow.Web/appsettings.Production.json')){throw 'appsettings.Production.json real está versionado'}
& dotnet restore; if($LASTEXITCODE -ne 0){throw 'restore falhou'}; & dotnet build -c $Configuration --no-restore; if($LASTEXITCODE -ne 0){throw 'build falhou'}; if(-not $SkipTests){& dotnet test -c $Configuration --no-build; if($LASTEXITCODE -ne 0){throw 'testes falharam'}}
$out=Join-Path $root 'publish\windows'; & dotnet publish 'src/HabitFlow.Web/HabitFlow.Web.csproj' -c $Configuration -o $out; if($LASTEXITCODE -ne 0){throw 'publish falhou'}
'HabitFlow.Web.dll','web.config','appsettings.json','wwwroot'|%{if(-not(Test-Path (Join-Path $out $_))){throw "Saída inválida: $_ ausente"}}
if(Get-ChildItem $out -Recurse -Directory -Include bin,obj){throw 'bin/obj encontrados no publish'}
if($Confirm -ne 'PUBLICAR_HABITFLOW_IIS' -and -not $DryRun){throw 'Confirmação obrigatória: -Confirm PUBLICAR_HABITFLOW_IIS'}
if(-not $DryRun){ if((Test-Path $TargetPath) -and -not $NoBackup){$b=Join-Path $root ("backups\iis\habitflow-{0}" -f (Get-Date -Format 'yyyyMMdd-HHmmss')); New-Item -ItemType Directory -Force $b|Out-Null; Copy-Item "$TargetPath\*" $b -Recurse -Force -ErrorAction SilentlyContinue}; New-Item -ItemType Directory -Force $TargetPath|Out-Null; Copy-Item "$out\*" $TargetPath -Recurse -Force }
"# IIS publish`nTarget: $TargetPath`nDryRun: $DryRun"|Set-Content $log; if($OpenFolder){Invoke-Item $TargetPath}
