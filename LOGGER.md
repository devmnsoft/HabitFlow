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

## v2.3.2 Circuit breaker e fila local
- Após 3 falhas consecutivas em `logSystemEvent`, o logger remoto pausa por 60 segundos.
- Logs sanitizados entram em `habitflow_pending_logs`, limitado a 100 itens, sem senha, token, CPF, cartão, secrets ou chaves.
- Falhas do próprio logger não são rereportadas para evitar loop; o error-monitor deduplica erros por 30 segundos e suprime repetições.
- O Admin Geral permite enviar ou limpar logs pendentes e testar `logSystemEvent` com `callFunction`.

## v2.3.4-Audit-Fix-Callable-Cache-Layout
- Frontend interno deve usar `callFunction`/`httpsCallable`; não use `fetch` direto para Functions internas.
- Publique Functions callable e Hosting juntos para evitar CORS por desalinhamento entre frontend e backend.
- Service worker usa cache `habitflow-v2-3-4`; em validação, desregistre o service worker, limpe site data e faça hard reload.
- Admin Geral > Diagnóstico Técnico inclui ações para limpar cache PWA, desregistrar service worker e recarregar a aplicação.

## v2.3.5 bootstrap remoto
O logger remoto inicia desabilitado. `bootstrapRemoteLogger()` executa `healthCheck` e um `logSystemEvent` de validação. Antes disso, eventos globais são salvos localmente/enfileirados e `flushPendingLogs()` retorna `remote_logger_not_ready`.
