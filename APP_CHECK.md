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
