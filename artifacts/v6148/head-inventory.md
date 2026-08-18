# Inventário inicial v6.14.8

- SHA inicial: `fae7d51f4677638c42ba5607d25f4a9ea802a421`.
- Branch inicial: `work`.
- Último PR detectado no histórico: PR #150, `codex/executar-validacao-no-windows-real`.
- Migration 063: encontrada em `database/migrations/063_account_privacy_center.sql`; contém as tabelas de consentimentos/eventos e o trigger de auditoria.
- Próximo número real após o inventário: `066` (já existiam 064 e 065).
- Runner Windows: existente; antes desta correção não fazia uma asserção explícita das duas tabelas LGPD.
- Banco local: não diagnosticado neste contêiner Linux, pois `psql`, a connection string local e o PostgreSQL de `C:\MNSOFT` não estão disponíveis.
- Árvore de trabalho inicial: limpa.
