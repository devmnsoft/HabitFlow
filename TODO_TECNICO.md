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


## v2.3.1-Hotfix — Callable Functions

- Use `assets/js/functions-client.js` e `callFunction()` para Functions internas.
- Não chame `cloudfunctions.net` com `fetch` no frontend para Functions internas.
- `getPublicSystemSettings`, `logSystemEvent`, `healthCheck` e `sendTestTelegramAlert` precisam estar deployadas como callable/onCall.
- Deploy recomendado:

```bash
cd functions
npm install
firebase deploy --only functions:getPublicSystemSettings
firebase deploy --only functions:logSystemEvent
firebase deploy --only functions:healthCheck
firebase deploy --only functions:sendTestTelegramAlert
firebase deploy --only functions
cd ..
firebase deploy --only hosting
```

## v2.3.2 pendências operacionais
- Deployar Functions callable e Hosting juntos.
- Validar console limpo em `http://localhost:5177` após login, dashboard, suporte, chatbot e Admin Geral.
- Conferir Diagnóstico Técnico como Admin Geral e confirmar que usuário comum não visualiza a seção.

## v2.3.3-Hotfix — pendência de ambiente de build

- `npm run build` foi executado após a resolução do hotfix callable, mas o ambiente local não possui o binário `vite` instalado em `node_modules`.
- Erro retornado: `sh: 1: vite: not found`.
- Pacote afetado: `vite` (devDependency do projeto).
- Classificação: falha de ambiente/dependências, não falha de código validada por `node --check functions/index.js`, `npm test` e `npm run security:scan`.
- Não contornar com alteração de código; restaurar dependências pelo registry autorizado antes do próximo build.

## v2.3.4-Audit-Fix-Callable-Cache-Layout
- Frontend interno deve usar `callFunction`/`httpsCallable`; não use `fetch` direto para Functions internas.
- Publique Functions callable e Hosting juntos para evitar CORS por desalinhamento entre frontend e backend.
- Service worker usa cache `habitflow-v2-3-4`; em validação, desregistre o service worker, limpe site data e faça hard reload.
- Admin Geral > Diagnóstico Técnico inclui ações para limpar cache PWA, desregistrar service worker e recarregar a aplicação.

## v2.3.5 pós-deploy
- Executar `firebase functions:list` e registrar evidência em auditoria.
- Executar `firebase deploy --only functions` e `firebase deploy --only hosting` no ambiente autenticado.
- Revalidar console em `http://localhost:5177` após limpar service worker/cache.

## v2.4 PRD pendências
- Configurar domínio final no Firebase Auth, App Check, CSP e APP_ALLOWED_ORIGINS.
- Executar smoke test PRD e atualizar PRD_DEPLOY_LOG.md.
- Habilitar Force HTTPS no IIS após certificado válido.

## v2.4.2-IIS-Publisher-Pro

- Validar publicação real em Windows Server com IIS URL Rewrite instalado.
- Autorizar domínio final IIS no Firebase Auth e App Check.
- Manter `publisher.config.json`, `publish/`, `dist/` e ZIPs fora do Git.
