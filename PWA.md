# HabitFlow v2.3.1-Hotfix — PWA install prompt

O evento `beforeinstallprompt` só deve ser interceptado quando houver botão de instalação na UI.

## Fluxo correto

1. O navegador dispara `beforeinstallprompt`.
2. O app chama `preventDefault()` apenas se `#installCard` e `#btnInstallApp` existirem.
3. O evento é guardado em `deferredInstallPrompt`.
4. O botão de instalação aparece.
5. Somente o clique do usuário chama `prompt()`.
6. `userChoice` é registrado no logger sem exibir erro técnico ao usuário.

Isso evita o warning: “Banner not shown: beforeinstallpromptevent.preventDefault() called”.

## v2.3.3-Hotfix-Functions-Callable-Logger-Stability

- Functions internas chamadas pelo navegador devem usar exclusivamente `assets/js/functions-client.js` com `httpsCallable`.
- `onRequest` fica reservado para webhooks externos, como `paymentWebhook`; se uma Function publicada ainda for HTTP, alinhe backend/frontend antes do deploy.
- O logger remoto possui circuit breaker: após 3 falhas consecutivas, pausa por 60 segundos, sanitiza e mantém até 100 logs em `localStorage` (`habitflow_pending_logs`).
- O monitor de erros ignora falhas originadas pelo próprio logger e deduplica erros repetidos para evitar loop, spam no console, Telegram e Functions.
- App Check é opcional em desenvolvimento; com `VITE_APP_CHECK_ENABLED=false`, o app apenas informa localmente que está desativado.
- O fluxo PWA `beforeinstallprompt` só chama `preventDefault()` quando há botão real de instalação e só chama `prompt()` após clique do usuário.
- Admin Geral inclui Diagnóstico Técnico para validar `getPublicSystemSettings`, `logSystemEvent`, `getMySupportTickets`, `healthCheck`, App Check e fila local de logs.
