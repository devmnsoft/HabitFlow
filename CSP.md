# Content Security Policy

A CSP está em `firebase.json` e permite Firebase, Google Auth, Bootstrap CDN, Google Fonts e endpoints de Firestore/Functions. Se o popup de Google Login quebrar, revise `script-src`, `frame-src` e `connect-src` mantendo `frame-ancestors 'none'` e `object-src 'none'`.

## v2.1-SecurityOps
- Adicionada camada operacional de segurança com CI, scanners de secrets/dist, validação de Firebase config e Firestore Rules.
- Admin Geral passa a ter painel de Segurança, eventos suspeitos, incidentes e solicitações LGPD.
- Functions críticas usam rate limit e auditoria administrativa backend; dados globais continuam protegidos por Rules e acessados via Functions.
- Produção deve usar `npm run build`, `npm run security:scan`, `npm run security:dist`, `npm run security:rules`, `npm run security:firebase` e `npm test` antes de publicar.
- Source maps, `.env`, Functions, `node_modules` e documentação interna não devem ser publicados no Hosting.
