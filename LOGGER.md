# HabitFlow v2.3.1-Hotfix — Logger

O logger remoto envia eventos para `logSystemEvent` via `callFunction`/`httpsCallable`.

## Proteções

- **Fallback local:** falhas remotas entram em `localStorage` na chave `habitflow_pending_logs`.
- **Fila local:** limitada a 100 logs para evitar crescimento ilimitado.
- **Sanitização:** senha, token, CPF, cartão, secret, authorization, apiKey e payload bruto são descartados.
- **Circuit breaker:** após falhas consecutivas, o envio remoto é pausado temporariamente.
- **Anti-loop:** falhas de `logSystemEvent` não são reportadas novamente para `logSystemEvent`.
- **Deduplicação:** erros repetidos são suprimidos e resumidos como `repeated_error_suppressed`.
- **Flush:** `flushPendingLogs()` tenta reenviar quando o app volta a ficar online ou via diagnóstico Admin.

## Diagnóstico Admin

O Admin Geral mostra status do logger remoto, falhas consecutivas, logs pendentes, última falha, além de botões para enviar/limpar fila e testar `getPublicSystemSettings`, `logSystemEvent` e `healthCheck`.

## v2.3.3-Hotfix-Functions-Callable-Logger-Stability

- Functions internas chamadas pelo navegador devem usar exclusivamente `assets/js/functions-client.js` com `httpsCallable`.
- `onRequest` fica reservado para webhooks externos, como `paymentWebhook`; se uma Function publicada ainda for HTTP, alinhe backend/frontend antes do deploy.
- O logger remoto possui circuit breaker: após 3 falhas consecutivas, pausa por 60 segundos, sanitiza e mantém até 100 logs em `localStorage` (`habitflow_pending_logs`).
- O monitor de erros ignora falhas originadas pelo próprio logger e deduplica erros repetidos para evitar loop, spam no console, Telegram e Functions.
- App Check é opcional em desenvolvimento; com `VITE_APP_CHECK_ENABLED=false`, o app apenas informa localmente que está desativado.
- O fluxo PWA `beforeinstallprompt` só chama `preventDefault()` quando há botão real de instalação e só chama `prompt()` após clique do usuário.
- Admin Geral inclui Diagnóstico Técnico para validar `getPublicSystemSettings`, `logSystemEvent`, `getMySupportTickets`, `healthCheck`, App Check e fila local de logs.
