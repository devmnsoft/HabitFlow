param([string]$BaseUrl='http://localhost:5097',[switch]$IncludeDevLogin)
. "$PSScriptRoot\_common.ps1"; $log=New-LogPath 'smoke-test'; $paths=@('/','/health/ready','/health/db','/health/version','/login'); $rows=@('# Smoke test')
foreach($p in $paths){ try{$r=Invoke-WebRequest -Uri ($BaseUrl.TrimEnd('/')+$p) -UseBasicParsing -TimeoutSec 15; $ok=$r.StatusCode -eq 200; $rows += "- $p => $($r.StatusCode)"; if(-not $ok){throw "Status inesperado"}; Write-Check OK "$p 200"}catch{Write-Check ERRO "$p falhou: $_"; $rows += "- ERRO $p $_"; $failed=$true}}
if($IncludeDevLogin){$rows += '- Login dev deve ser usado somente em Development; não executado automaticamente.'}; $rows|Set-Content $log; if($failed){exit 1}
