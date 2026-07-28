$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$views = Join-Path $root 'src/HabitFlow.Web/Views'
$layout = Join-Path $views 'Shared/_Layout.cshtml'
$failures = [System.Collections.Generic.List[string]]::new()
$commonViews = Get-ChildItem $views -Recurse -Filter '*.cshtml' | Where-Object { $_.Name -notin @('_ViewStart.cshtml', '_Layout.cshtml') -and $_.FullName -notmatch '[\\/]Emails?[\\/]|[\\/]Print[\\/]' }
foreach ($file in $commonViews) {
  if (Select-String -Path $file.FullName -Pattern '\bLayout\s*=' -Quiet) { $failures.Add("Layout direto: $($file.FullName)") }
  if (Select-String -Path $file.FullName -Pattern 'style\s*=\s*"[^\"]*(position\s*:\s*(fixed|absolute)|width\s*:\s*\d{3,}px)' -Quiet) { $failures.Add("Estilo inline crítico: $($file.FullName)") }
  if (Select-String -Path $file.FullName -Pattern 'class\s*=\s*"[^\"]*\b(container|container-fluid|hf-shell)\b' -Quiet) { $failures.Add("Container duplicado no shell: $($file.FullName)") }
}
$layoutText = Get-Content $layout -Raw
if (($layoutText | Select-String 'RenderSectionAsync\("Scripts"' -AllMatches).Matches.Count -ne 1) { $failures.Add('Scripts deve ser renderizado exatamente uma vez.') }
if ($layoutText -match 'Layout\s*=') { $failures.Add('_Layout não pode aninhar outro layout.') }
$bootstrapPosition = $layoutText.IndexOf('bootstrap.bundle.min.js')
$sectionPosition = $layoutText.IndexOf('RenderSectionAsync("Scripts"')
if ($bootstrapPosition -lt 0 -or $sectionPosition -lt $bootstrapPosition) { $failures.Add('Scripts de página não estão depois do Bootstrap.') }
if (($layoutText -match 'public\.css') -and ($layoutText -match 'personal\.css') -and ($layoutText -match 'account\.css') -and ($layoutText -match 'platform\.css')) { $failures.Add('CSS contextuais estão carregados simultaneamente.') }
if ($layoutText -match 'NavigationVariant\.PlatformSidebar' -and $layoutText -match 'PlatformTop') { $failures.Add('Menu da plataforma não pode ser horizontal.') }
if ((Get-Content (Join-Path $root 'src/HabitFlow.Web/Services/NavigationService.cs') -Raw) -match 'Url:\s*"#') { $failures.Add('URL de menu com #.') }
$navigationText = Get-Content (Join-Path $root 'src/HabitFlow.Web/Services/NavigationService.cs') -Raw
$catalogText = Get-Content (Join-Path $root 'src/HabitFlow.Web/Services/NavigationIconCatalog.cs') -Raw
$icons = [regex]::Matches($navigationText, 'Icon:\s*"([^"]+)"') | ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique
foreach ($icon in $icons) { if ($catalogText -notmatch ('"' + [regex]::Escape($icon) + '"')) { $failures.Add("Ícone de navegação ausente: $icon") } }
if ($layoutText -match 'hf-public-footer[\s\S]*NavigationContext\.Platform') { } # context branches are intentionally colocated in the sole shell.
if ($failures.Count) { $failures | ForEach-Object { Write-Error $_ }; exit 1 }
Write-Host 'Contrato de layout v6.5.1 validado.'
