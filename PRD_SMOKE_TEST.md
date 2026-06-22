# PRD Smoke Test

## Pré-deploy
- `npm run security:scan`
- `npm run verify:functions`
- `node --check functions/index.js`
- `npm run build`
- `npm run security:dist`

## Deploy
- `firebase deploy --only functions`
- `firebase deploy --only firestore:rules`
- `firebase deploy --only hosting`

## IIS opcional
- Copiar `dist` para `C:\inetpub\wwwroot\habitflow`.
- Confirmar `web.config`, reciclar site/app pool e limpar cache do navegador.

## Funcional
Login Google, login e-mail/senha, criar hábito `[SMOKE]`, marcar hábito, progresso, perfil, suporte, chatbot, `getPublicSystemSettings`, `logSystemEvent`, `getMySupportTickets`, Admin Geral, Diagnóstico Técnico, Telegram teste, PWA sem erro e console sem CORS.

## Pós-teste
Rodar cleanup smoke data e registrar resultado em `PRD_DEPLOY_LOG.md`.
