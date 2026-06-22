# Firebase App Check

1. No Firebase Console, crie App Check para o app Web com reCAPTCHA v3 ou Enterprise.
2. Configure no build: `VITE_APP_CHECK_ENABLED=true` e `VITE_APP_CHECK_SITE_KEY=<site-key>`.
3. Em localhost, use `VITE_APP_CHECK_DEBUG_TOKEN` apenas em desenvolvimento.
4. Valide Auth, Firestore e Functions antes de ativar enforcement.
5. Ative enforcement gradualmente para Firestore e Functions após monitorar erros.

## v2.1-SecurityOps
- Adicionada camada operacional de segurança com CI, scanners de secrets/dist, validação de Firebase config e Firestore Rules.
- Admin Geral passa a ter painel de Segurança, eventos suspeitos, incidentes e solicitações LGPD.
- Functions críticas usam rate limit e auditoria administrativa backend; dados globais continuam protegidos por Rules e acessados via Functions.
- Produção deve usar `npm run build`, `npm run security:scan`, `npm run security:dist`, `npm run security:rules`, `npm run security:firebase` e `npm test` antes de publicar.
- Source maps, `.env`, Functions, `node_modules` e documentação interna não devem ser publicados no Hosting.

## v2.2 — Modo controlado

Mensagem amigável em falha de validação: "Não foi possível validar a segurança desta sessão. Atualize a página ou tente novamente."

### Fase 1 — Monitoramento
- Criar site key reCAPTCHA no Firebase App Check.
- Configurar `VITE_APP_CHECK_SITE_KEY` no frontend.
- Usar debug token local em `localhost:5177`.
- Manter enforcement desativado enquanto mede tráfego válido no Console.

### Fase 2 — Enforcement parcial
- Ativar enforcement primeiro em Cloud Functions.
- Monitorar erros, logs e suporte.
- Reverter enforcement se autenticação, chatbot ou LGPD quebrarem.

### Fase 3 — Enforcement total
- Ativar Firestore após validação.
- Storage somente quando for usado futuramente.

## v2.3.3-Hotfix-Functions-Callable-Logger-Stability

- Functions internas chamadas pelo navegador devem usar exclusivamente `assets/js/functions-client.js` com `httpsCallable`.
- `onRequest` fica reservado para webhooks externos, como `paymentWebhook`; se uma Function publicada ainda for HTTP, alinhe backend/frontend antes do deploy.
- O logger remoto possui circuit breaker: após 3 falhas consecutivas, pausa por 60 segundos, sanitiza e mantém até 100 logs em `localStorage` (`habitflow_pending_logs`).
- O monitor de erros ignora falhas originadas pelo próprio logger e deduplica erros repetidos para evitar loop, spam no console, Telegram e Functions.
- App Check é opcional em desenvolvimento; com `VITE_APP_CHECK_ENABLED=false`, o app apenas informa localmente que está desativado.
- O fluxo PWA `beforeinstallprompt` só chama `preventDefault()` quando há botão real de instalação e só chama `prompt()` após clique do usuário.
- Admin Geral inclui Diagnóstico Técnico para validar `getPublicSystemSettings`, `logSystemEvent`, `getMySupportTickets`, `healthCheck`, App Check e fila local de logs.
