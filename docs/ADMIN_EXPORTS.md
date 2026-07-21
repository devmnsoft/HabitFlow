# Exportações administrativas

As exportações CSV administrativas são restritas a administradores e registradas em `habitflow.admin_exports` e auditoria administrativa.

## Proteções
- Não exportar senha hash, tokens, cookies ou secrets.
- Sanitizar células que começam com `=`, `+`, `-` ou `@` prefixando apóstrofo.
- Gerar arquivos por tipo: usuários, leads, suporte, LGPD, logs do sistema e auditoria admin.
