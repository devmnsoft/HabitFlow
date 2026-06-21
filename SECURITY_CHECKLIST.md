# Checklist de produção

- [ ] `npm run security:scan`
- [ ] `npm run build`
- [ ] verificar `dist/` sem `.map`
- [ ] verificar `dist/` sem `sourceMappingURL`
- [ ] verificar `dist/` sem secrets
- [ ] testar login Google e e-mail/senha
- [ ] testar Firestore, Functions, Admin Geral e Chatbot
- [ ] testar PWA, WhatsApp e Telegram quando configurado
- [ ] testar CSP no navegador
- [ ] testar App Check antes de enforcement
- [ ] testar usuário comum sem acesso admin
- [ ] `firebase deploy`

## v2.1-SecurityOps
- Adicionada camada operacional de segurança com CI, scanners de secrets/dist, validação de Firebase config e Firestore Rules.
- Admin Geral passa a ter painel de Segurança, eventos suspeitos, incidentes e solicitações LGPD.
- Functions críticas usam rate limit e auditoria administrativa backend; dados globais continuam protegidos por Rules e acessados via Functions.
- Produção deve usar `npm run build`, `npm run security:scan`, `npm run security:dist`, `npm run security:rules`, `npm run security:firebase` e `npm test` antes de publicar.
- Source maps, `.env`, Functions, `node_modules` e documentação interna não devem ser publicados no Hosting.
