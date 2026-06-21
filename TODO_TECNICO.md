## TODO técnico — pós 2.0-Security

- Executar build quando dependências estiverem disponíveis no registry.
- Testar Google Login, Firestore, Functions, Admin, Chatbot e PWA no preview.
- Criar scripts seguros para set/revoke custom claims.
- Revisar CSP em navegador real e ajustar domínios mínimos.

# TODO Técnico do HabitFlow

- Implementar backend com Firebase Functions.
- Implementar checkout real.
- Implementar webhook.
- Implementar painel admin global.
- Implementar notificações.
- Implementar relatórios PDF.
- Implementar exclusão completa de conta.
- Implementar exportação de dados LGPD.
- Implementar exclusão definitiva de dados LGPD.
- Implementar testes automatizados.

## v1.7 — Observabilidade e retenção

- Implementar limpeza automática de logs antigos via scheduled function respeitando `LOG_RETENTION_DAYS = 90` e `ERROR_LOG_RETENTION_DAYS = 180`.
- Implementar exportação de logs administrativos.
- Implementar agrupamento de bugs por assinatura de erro (`errorFingerprint`).

## v1.7.1 — Estabilização pós-console
- Validar em produção a migração completa para `users/{uid}/usageEvents/{eventId}`.
- Testar erro `auth/unauthorized-domain` em domínio não autorizado e confirmar mensagem amigável.
- Testar `sendTestTelegramAlert` pelo Admin Geral após configurar `functions/.env` sem versionar token.
- Simular `frontend_error` e conferir visibilidade em Admin Geral via `systemAuditLogs`.

## v1.8 concluída / próximos refinamentos
- Expandir cobertura automatizada de fluxos UI do chatbot.
- Criar testes unitários para normalização de WhatsApp nas Functions.
- Evoluir Admin Geral com paginação e exportação segura.

## v1.9 concluída / próximos refinamentos
- Conectar provedor real de IA somente no backend.
- Evoluir rate limit por minuto em `users/{uid}/rateLimits/chatbot`.
- Criar tela dedicada para detalhes e respostas de tickets.
- Adicionar testes automatizados de callable Functions com Firebase Emulator.

## Pós v1.9.1

- Automatizar testes end-to-end de queda/restauração de Functions.
- Validar domínio próprio em `APP_ALLOWED_ORIGINS` antes do go-live.
- Criar painel histórico para taxa de flush da fila local.

## v2.1-SecurityOps
- Adicionada camada operacional de segurança com CI, scanners de secrets/dist, validação de Firebase config e Firestore Rules.
- Admin Geral passa a ter painel de Segurança, eventos suspeitos, incidentes e solicitações LGPD.
- Functions críticas usam rate limit e auditoria administrativa backend; dados globais continuam protegidos por Rules e acessados via Functions.
- Produção deve usar `npm run build`, `npm run security:scan`, `npm run security:dist`, `npm run security:rules`, `npm run security:firebase` e `npm test` antes de publicar.
- Source maps, `.env`, Functions, `node_modules` e documentação interna não devem ser publicados no Hosting.

## v2.2-Production — Pendências operacionais externas
- Definir domínio final e configurar DNS/SSL.
- Criar bucket Cloud Storage de backup.
- Escolher provedor de e-mail e configurar secret backend.
- Validar App Check em monitoramento antes de enforcement.
- Configurar Mercado Pago sandbox e webhook.
