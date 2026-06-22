# HabitFlow v2.3.1-Hotfix — Functions e CORS

## onCall x onRequest

- **Callable Functions (`onCall`)** são chamadas pelo frontend com `httpsCallable` via `assets/js/functions-client.js`. Elas carregam automaticamente o contexto do Firebase Auth/App Check e evitam `fetch` direto para `cloudfunctions.net`.
- **HTTP Functions (`onRequest`)** ficam reservadas para integrações externas, como webhooks de pagamento. Quando uma HTTP Function for chamada pelo navegador, ela deve aplicar `functions/cors.js`.

## Por que o CORS ocorria

`getPublicSystemSettings` e `logSystemEvent` estavam sendo acessadas como endpoints HTTP diretos em versões anteriores. Isso fazia o navegador enviar preflight `OPTIONS` e bloquear a resposta sem `Access-Control-Allow-Origin`.

## Correção

- Frontend usa `callFunction(name, payload)`.
- `getPublicSystemSettings`, `logSystemEvent`, `healthCheck`, `sendTestTelegramAlert`, chatbot, suporte e Admin usam callable/onCall.
- `paymentWebhook` continua HTTP/onRequest e usa CORS apenas quando necessário.

## Teste em localhost:5177

1. Rode `npm run dev` ou `npm start` mantendo a porta `5177`.
2. Abra `http://localhost:5177`.
3. No DevTools, confirme que não há erro CORS para `getPublicSystemSettings` ou `logSystemEvent`.

## Deploy

```bash
cd functions
npm install
firebase deploy --only functions:getPublicSystemSettings
firebase deploy --only functions:logSystemEvent
firebase deploy --only functions:healthCheck
firebase deploy --only functions:sendTestTelegramAlert
# ou
firebase deploy --only functions
cd ..
firebase deploy --only hosting
```

Se o frontend usa `httpsCallable`, a Function precisa ser `onCall`. Se ela continuar `onRequest`, o erro de CORS pode continuar.

## v2.3.3-Hotfix-Functions-Callable-Logger-Stability

- Functions internas chamadas pelo navegador devem usar exclusivamente `assets/js/functions-client.js` com `httpsCallable`.
- `onRequest` fica reservado para webhooks externos, como `paymentWebhook`; se uma Function publicada ainda for HTTP, alinhe backend/frontend antes do deploy.
- O logger remoto possui circuit breaker: após 3 falhas consecutivas, pausa por 60 segundos, sanitiza e mantém até 100 logs em `localStorage` (`habitflow_pending_logs`).
- O monitor de erros ignora falhas originadas pelo próprio logger e deduplica erros repetidos para evitar loop, spam no console, Telegram e Functions.
- App Check é opcional em desenvolvimento; com `VITE_APP_CHECK_ENABLED=false`, o app apenas informa localmente que está desativado.
- O fluxo PWA `beforeinstallprompt` só chama `preventDefault()` quando há botão real de instalação e só chama `prompt()` após clique do usuário.
- Admin Geral inclui Diagnóstico Técnico para validar `getPublicSystemSettings`, `logSystemEvent`, `getMySupportTickets`, `healthCheck`, App Check e fila local de logs.
