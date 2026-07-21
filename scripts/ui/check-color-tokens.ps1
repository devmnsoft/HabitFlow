$ErrorActionPreference = 'Stop'
$css = Get-Content "src/HabitFlow.Web/wwwroot/css/site.css" -Raw
$required = @('--hf-bg','--hf-surface','--hf-surface-green','--hf-surface-blue','--hf-surface-dark','--hf-text','--hf-text-muted','--hf-text-on-dark','--hf-primary','--mnsoft-blue','body.hf-contrast-high','body.hf-font-large','body.hf-reduce-motion','.mnsoft-official-logo')
$missing = $required | Where-Object { $css -notmatch [regex]::Escape($_) }
if ($missing) { Write-Error ("Tokens/classes ausentes: " + ($missing -join ', ')) }
$forbidden = @('color:\s*#9CA3AF','\.text-(muted|readable|body)[^{]*\{[^}]*opacity:\s*\.(0|1|2|3|4|5|6)(?!\d)')
foreach ($pattern in $forbidden) { if ($css -match $pattern) { Write-Error "Uso de cor/opacidade proibido detectado: $pattern" } }
Write-Host "Tokens de cor e acessibilidade validados."
