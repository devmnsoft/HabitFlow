# Branding MNSOFT no HabitFlow

O HabitFlow exibe a assinatura institucional da MNSOFT no rodapé e em pontos de confiança da experiência premium.

## Regra para contribuições via Codex

O Codex não deve commitar arquivos binários no repositório. Isso inclui PNG, JPG, JPEG, WEBP, ICO, GIF, ZIP, dumps, backups, artefatos de publicação e arquivos gerados por build.

Enquanto uma logo oficial binária não for adicionada manualmente fora do fluxo do Codex, o sistema deve usar a marca textual em SVG puro localizada em:

- `src/HabitFlow.Web/wwwroot/brand/mnsoft/logo-mnsoft.svg`

Esse SVG é textual, versionável no Git e não contém base64 nem imagem embutida.

## Uso da logo oficial

Se a MNSOFT precisar usar a logo oficial em PNG ou JPG, ela pode ser adicionada manualmente depois por um mantenedor, fora do Codex e conforme a política do repositório. Não embuta imagens em base64 dentro de SVG, HTML, CSS ou JavaScript.

## Boas práticas

- Prefira HTML, CSS, Razor, Bootstrap 5, JavaScript Vanilla e SVG textual.
- Não adicione arquivos pesados ao repositório.
- Não versione `publish/`, `bin/`, `obj/`, `dist/`, `node_modules/` ou `backups/`.
- Preserve a evolução visual premium da v4.8 com componentes reutilizáveis e UX mobile first.

## v4.9 — Confiança institucional

A MNSOFT aparece como assinatura discreta de confiança, com CNPJ 18.160.057/0001-13 e comercial@mnsoft.com.br. O produto principal continua sendo HabitFlow.
