# Auditoria v2.3.4-Audit-Fix-Callable-Cache-Layout

Comando-base executado:

```bash
rg -n "cloudfunctions.net|FUNCTIONS_BASE_URL|CLOUD_FUNCTIONS_URL|API_BASE_URL|fetch\(|XMLHttpRequest|getFunctions|httpsCallable|getPublicSystemSettings|logSystemEvent|getMySupportTickets|beforeinstallprompt|preventDefault|CONFLICT_MARKERS" .
```

## Relatório de achados

| Arquivo | Linha/termo | Risco | Correção aplicada | Status |
|---|---:|---|---|---|
| `assets/js/functions-client.js` | `getFunctions`, `httpsCallable` | Uso direto deve ser centralizado. | Mantido como único cliente de Functions callable, com mensagens amigáveis e modo silencioso. | OK |
| `assets/js/app.js` | `getPublicSystemSettings`, `getMySupportTickets`, `logSystemEvent` | Chamadas internas poderiam gerar CORS se usassem `fetch`. | Validado uso via `callFunction`; diagnóstico Admin usa callable e ganhou ações de cache/PWA. | OK |
| `assets/js/error-monitor.js` | `logSystemEvent` | Risco de loop se falha do logger for reportada novamente. | Validado filtro anti-loop e fallback para fila local. | OK |
| `assets/js/logger.js` | `logSystemEvent` | Spam remoto em falhas repetidas. | Validado circuit breaker: 3 falhas pausam envio remoto por 60s, com fila local. | OK |
| `assets/js/log-queue.js` | `logSystemEvent` | Reenvio de fila pode repetir chamadas. | Mantido envio sequencial e parada na primeira falha. | OK |
| `functions/index.js` | `onCall`, `onRequest`, `fetch` Telegram | Functions internas precisam ser callable; `fetch` backend para Telegram é permitido. | Validado que as funções internas auditadas usam `onCall`; `onRequest` fica para `paymentWebhook`. | OK |
| `service-worker.js` | `cloudfunctions.net`, `fetch(` | SW poderia servir JS antigo ou cachear APIs sensíveis. | Cache versionado para `habitflow-v2-3-4`, limpeza no activate, bloqueio de hosts Firebase/Functions e cache apenas same-origin GET estático. | OK |
| `index.html` | `data-pwa-install` | `beforeinstallprompt` não deve ser cancelado sem UI real. | Botão real mantido oculto e exibido apenas quando evento PWA estiver disponível. | OK |
| `firebase.json` | CSP `cloudfunctions.net` | Conexão remota permitida pela CSP; não é chamada direta do app. | Mantido por compatibilidade Firebase SDK/callable; sem URLs hardcoded no frontend. | OK |
| Documentação (`README.md`, `DEPLOY.md`, `FUNCTIONS_CORS.md`, `LOGGER.md`, `PWA.md`, `TODO_TECNICO.md`) | termos auditados | Documentos precisam explicar alinhamento deploy/cache. | Atualizados com checklist v2.3.4 e comandos de deploy/limpeza. | OK |

## Buscas específicas

- Marcadores Git: nenhum marcador de conflito deve permanecer após `rg -n "CONFLICT_MARKERS" .`.
- Frontend: `cloudfunctions.net` não deve aparecer em `assets`, `dist` ou `public` exceto documentação/Service Worker bloqueando cache.
- Firebase Functions SDK: `getFunctions` e `httpsCallable` devem aparecer somente em `assets/js/functions-client.js` no frontend.
- Backend: `onRequest` deve ficar restrito a webhooks externos reais; `paymentWebhook` permanece HTTP.

## Observações de produção

Se o navegador ainda mostrar CORS depois desta correção local, o cenário mais provável é desalinhamento de deploy: frontend publicado usando `httpsCallable` contra Function ainda publicada como `onRequest`, ou service worker/cache servindo JavaScript antigo. Publique Functions e Hosting juntos e limpe o cache do navegador antes de validar.
