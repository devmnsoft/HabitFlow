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
