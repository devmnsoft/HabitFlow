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
