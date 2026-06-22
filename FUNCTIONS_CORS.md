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

## v2.3.2 Hotfix callable
- O frontend deve chamar Functions internas exclusivamente por `callFunction`/`httpsCallable`; não use URLs `cloudfunctions.net` nem `fetch` direto.
- `onCall` inclui protocolo callable, contexto de Auth/App Check e evita o preflight CORS manual que bloqueou localhost:5177.
- `onRequest` fica reservado para webhooks externos, como pagamento, onde CORS/assinatura são tratados explicitamente.
- Teste em `http://localhost:5177` validando `getPublicSystemSettings`, `logSystemEvent`, `getMySupportTickets` e `healthCheck` sem erros CORS no console.

## v2.3.4-Audit-Fix-Callable-Cache-Layout
- Frontend interno deve usar `callFunction`/`httpsCallable`; não use `fetch` direto para Functions internas.
- Publique Functions callable e Hosting juntos para evitar CORS por desalinhamento entre frontend e backend.
- Service worker usa cache `habitflow-v2-3-4`; em validação, desregistre o service worker, limpe site data e faça hard reload.
- Admin Geral > Diagnóstico Técnico inclui ações para limpar cache PWA, desregistrar service worker e recarregar a aplicação.
