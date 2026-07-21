$ErrorActionPreference = 'Stop'
function Get-RepoRoot { (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path }
function New-LogPath([string]$Name){ $root=Get-RepoRoot; $dir=Join-Path $root 'publish\logs'; New-Item -ItemType Directory -Force $dir|Out-Null; Join-Path $dir ("$Name-{0}.md" -f (Get-Date -Format 'yyyyMMdd-HHmmss')) }
function Write-Check($Level,$Message){ $prefix=@{OK='[OK]';AVISO='[AVISO]';ERRO='[ERRO]'}[$Level]; Write-Host "$prefix $Message" }
function Require-Command($Name,$Hint){ if(-not (Get-Command $Name -ErrorAction SilentlyContinue)){ throw "$Name não encontrado. $Hint" } }
