# Branding MNSOFT

A MNSOFT aparece no HabitFlow como marca de confiança, não como nome principal do produto.

## Logo oficial

O caminho esperado para o arquivo oficial é:

`src/HabitFlow.Web/wwwroot/brand/mnsoft/logo-mnsoft-oficial.png`

O Codex não deve criar, redesenhar ou commitar PNG/JPG/ICO/ZIP/PDF/DOCX da logo. O arquivo deve ser adicionado manualmente pelo cliente quando o binário oficial estiver disponível.

O template já possui o partial `Views/Shared/Partials/_MNSOFTOfficialLogo.cshtml`, que renderiza a imagem oficial com proporção preservada. A classe `.mnsoft-official-logo` usa `max-width`, `width: 100%`, `height: auto` e `object-fit: contain`; não usa filtros, recortes ou distorções.
