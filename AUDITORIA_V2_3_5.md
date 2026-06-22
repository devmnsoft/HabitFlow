# Auditoria v2.3.5-Fix-Callable-Deploy-Cache-Logger

## Buscas obrigatórias
- `rg -n "getPublicSystemSettings|logSystemEvent|getMySupportTickets|healthCheck|onRequest|onCall|https\.onCall|https\.onRequest" functions`
- `rg -n "cloudfunctions.net|FUNCTIONS_BASE_URL|CLOUD_FUNCTIONS_URL|API_BASE_URL" assets dist public . --glob '!**/*.md'`
- `rg -n "getFunctions|httpsCallable" assets --glob '!assets/js/functions-client.js'`
- `rg -n "onRequest|https\.onRequest" functions`

## Correções aplicadas
- Functions críticas locais auditadas como `onCall`; `paymentWebhook` preservado como `onRequest`.
- Script `scripts/verify-functions-shape.js` criado.
- Logger remoto protegido por `bootstrapRemoteLogger()` com `healthCheck`.
- `flushPendingLogs`, `trackUserAction`, fallback de settings e tickets protegidos contra spam de `logSystemEvent`.
- `service-worker.js` versionado para `habitflow-v2-3-5` e bypass de Firebase/Functions.

## Deploy necessário
Executar em ambiente autenticado: `firebase functions:list`, `firebase deploy --only functions` e `firebase deploy --only hosting`.
