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
