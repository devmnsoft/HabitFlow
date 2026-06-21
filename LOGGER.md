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

## v1.9.1 — logger resiliente

- `logSystemEvent` é chamado via `httpsCallable` pelo helper `functions-client.js`.
- Falhas do logger remoto não exibem toast para usuários comuns.
- Após 3 falhas consecutivas, o envio remoto é pausado por 60 segundos.
- Logs pendentes são sanitizados e salvos em `localStorage` (`habitflow_pending_logs`) com limite de 100 itens.
- A fila local pode ser reenviada por `flushPendingLogs()` ao voltar online, após login e pelo diagnóstico do Admin Geral.
- `error-monitor.js` ignora falhas causadas pelo próprio logger e deduplica erros repetidos para evitar loop/spam.

## v2.1-SecurityOps
- Adicionada camada operacional de segurança com CI, scanners de secrets/dist, validação de Firebase config e Firestore Rules.
- Admin Geral passa a ter painel de Segurança, eventos suspeitos, incidentes e solicitações LGPD.
- Functions críticas usam rate limit e auditoria administrativa backend; dados globais continuam protegidos por Rules e acessados via Functions.
- Produção deve usar `npm run build`, `npm run security:scan`, `npm run security:dist`, `npm run security:rules`, `npm run security:firebase` e `npm test` antes de publicar.
- Source maps, `.env`, Functions, `node_modules` e documentação interna não devem ser publicados no Hosting.
