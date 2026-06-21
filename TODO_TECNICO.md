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
