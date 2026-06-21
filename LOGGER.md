# LOGGER.md — Observabilidade HabitFlow v1.8

## Frontend
`assets/js/logger.js` fornece `logger.info`, `logger.warning`, `logger.error`, `logger.critical` e `safeAsync`. O logger sanitiza metadata, inclui versão, ambiente, timestamp/contexto de usuário, grava eventos pessoais em `users/{uid}/usageEvents` e chama `logSystemEvent` para eventos globais relevantes.

## Backend
`functions/index.js` centraliza gravação em `systemAuditLogs`, inclui severidade, origem, usuário, ação, mensagem, metadata sanitizada, fingerprint, status de bug e status de Telegram. `functions/logger.js` documenta/utiliza helpers reutilizáveis para evolução modular.

## Severidades
- `info`: ação normal.
- `warning`: limite, tentativa inválida ou falha não crítica.
- `error`: falha Firebase/Function/checkout/chatbot/PWA.
- `critical`: falha grave, acesso admin indevido ou abuso.

## Bugs
Logs de erro recebem `bugStatus`: `new`, `read`, `resolved` ou `ignored`. Admins usam `markBugAsRead`, `markBugAsResolved` e `ignoreBug`.

## Não logar
Nunca registrar senha, token, CPF, cartão, CVV, secrets, payload bruto de pagamento ou stack completa para usuário comum.

## v1.9 — Eventos do chatbot e suporte
Novos fluxos registram `chatbot_opened`, `chatbot_closed`, `chatbot_message_sent`, `chatbot_response_generated`, `chatbot_fallback_used`, `chatbot_unknown_question`, `chatbot_sensitive_request_blocked`, `chatbot_ticket_created`, `chatbot_support_clicked`, `chatbot_whatsapp_clicked`, `chatbot_email_clicked`, `chatbot_premium_interest` e `chatbot_error`. Mensagens sensíveis não devem ser persistidas completas.
