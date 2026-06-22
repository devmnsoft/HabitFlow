# Firebase PRD Deploy

## Ordem segura
```bash
npm run security:scan
npm run verify:functions
node --check functions/index.js
npm run build
npm run security:dist
firebase deploy --only functions
firebase deploy --only firestore:rules
firebase deploy --only hosting
```

## Hosting
`firebase.json` publica `dist`, ignora `functions`, `node_modules`, `.env`, source maps e arquivos sensíveis, aplica headers de segurança e rewrite SPA para `index.html`.

## Pós-deploy
- Abrir `https://habitflow-5f945.web.app` e `https://habitflow-5f945.firebaseapp.com`.
- Validar login, getPublicSystemSettings, healthCheck, logSystemEvent, suporte, Admin Geral, Telegram teste e PWA.
- Limpar cache PWA pelo Admin Geral se detectar versão antiga.
