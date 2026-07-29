#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [string] $AggregatePath = (Join-Path $PSScriptRoot '../../database/script_completo.sql')
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
function Expand-PsqlIncludes([string] $Path, [System.Collections.Generic.HashSet[string]] $Stack) {
    $resolved = (Resolve-Path -LiteralPath $Path).Path
    if (-not $Stack.Add($resolved)) { throw "Recursive psql include: $resolved" }
    $content = Get-Content -LiteralPath $resolved -Raw
    $content = [regex]::Replace($content, '(?m)^\s*\\i(r)?\s+([^\r\n]+)\s*$', {
        param($match)
        $target = $match.Groups[2].Value.Trim(' ', "'", '"')
        $includePath = if ($match.Groups[1].Success) {
            Join-Path (Split-Path $resolved) $target
        } else {
            Join-Path $repoRoot $target
        }
        Expand-PsqlIncludes $includePath $Stack
    })
    [void] $Stack.Remove($resolved)
    return $content
}

$sql = Expand-PsqlIncludes $AggregatePath ([System.Collections.Generic.HashSet[string]]::new())
$sql = [regex]::Replace($sql, '(?m)--.*$', '')

function Split-TopLevel([string] $Text, [char] $Separator = ',') {
    $parts = [System.Collections.Generic.List[string]]::new()
    $depth = 0; $quoted = $false; $start = 0
    for ($i = 0; $i -lt $Text.Length; $i++) {
        if ($Text[$i] -eq "'") {
            if ($quoted -and $i + 1 -lt $Text.Length -and $Text[$i + 1] -eq "'") { $i++; continue }
            $quoted = -not $quoted
        }
        if (-not $quoted) {
            if ($Text[$i] -eq '(') { $depth++ }
            elseif ($Text[$i] -eq ')') { $depth-- }
            elseif ($Text[$i] -eq $Separator -and $depth -eq 0) {
                $parts.Add($Text.Substring($start, $i - $start)); $start = $i + 1
            }
        }
    }
    $parts.Add($Text.Substring($start))
    return $parts
}

$contracts = @{}
$violations = [System.Collections.Generic.List[string]]::new()
$statements = Split-TopLevel $sql ';'
foreach ($raw in $statements) {
    $statement = $raw.Trim()
    if ($statement -match '(?is)^create\s+table\s+(?:if\s+not\s+exists\s+)?habitflow\.([a-z0-9_]+)\s*\((.*)\)') {
        $table = $Matches[1].ToLowerInvariant()
        if (-not $contracts.ContainsKey($table)) { $contracts[$table] = @{} }
        foreach ($definition in (Split-TopLevel $Matches[2])) {
            $definition = $definition.Trim()
            if ($definition -match '^(?i)(constraint|primary|foreign|unique|check|exclude)\b') { continue }
            if ($definition -match '^(?:"([^"]+)"|([a-zA-Z_][a-zA-Z0-9_]*))\s+(.+)$') {
                $column = $(if ($Matches[1]) { $Matches[1] } else { $Matches[2] }).ToLowerInvariant()
                $tail = $Matches[3]
                $contracts[$table][$column] = @{
                    Required = $tail -match '(?i)\bnot\s+null\b' -or $tail -match '(?i)\bprimary\s+key\b'
                    Default = $tail -match '(?i)\bdefault\b' -or $tail -match '(?i)\bgenerated\b'
                }
            }
        }
        continue
    }
    if ($statement -match '(?is)^alter\s+table\s+habitflow\.([a-z0-9_]+)\s+add\s+column\s+(?:if\s+not\s+exists\s+)?([a-z0-9_]+)\s+(.+)$') {
        $table = $Matches[1].ToLowerInvariant(); $column = $Matches[2].ToLowerInvariant(); $tail = $Matches[3]
        if (-not $contracts.ContainsKey($table)) { $contracts[$table] = @{} }
        $contracts[$table][$column] = @{ Required = $tail -match '(?i)\bnot\s+null\b'; Default = $tail -match '(?i)\bdefault\b' }
        continue
    }
    if ($statement -match '(?is)^alter\s+table\s+habitflow\.([a-z0-9_]+)\s+alter\s+column\s+([a-z0-9_]+)\s+set\s+not\s+null') {
        $table = $Matches[1].ToLowerInvariant(); $column = $Matches[2].ToLowerInvariant()
        if ($contracts.ContainsKey($table) -and $contracts[$table].ContainsKey($column)) { $contracts[$table][$column].Required = $true }
        continue
    }
    if ($statement -match '(?is)^insert\s+into\s+habitflow\.([a-z0-9_]+)\s*\(([^)]+)\)') {
        $table = $Matches[1].ToLowerInvariant()
        $inserted = @((Split-TopLevel $Matches[2]) | ForEach-Object { $_.Trim(' ', '"').ToLowerInvariant() })
        if ($contracts.ContainsKey($table)) {
            foreach ($column in $contracts[$table].Keys) {
                $contract = $contracts[$table][$column]
                if ($contract.Required -and -not $contract.Default -and $column -notin $inserted) {
                    $violations.Add("INSERT into habitflow.$table omits NOT NULL column '$column' without a default")
                }
            }
        }
    }
}

if ($violations.Count -gt 0) {
    $violations | Sort-Object -Unique | ForEach-Object { Write-Error $_ }
    exit 1
}
Write-Host "Aggregate NOT NULL insert contract is valid ($AggregatePath)."
