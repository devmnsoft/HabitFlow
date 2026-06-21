# Firebase App Check

1. No Firebase Console, crie App Check para o app Web com reCAPTCHA v3 ou Enterprise.
2. Configure no build: `VITE_APP_CHECK_ENABLED=true` e `VITE_APP_CHECK_SITE_KEY=<site-key>`.
3. Em localhost, use `VITE_APP_CHECK_DEBUG_TOKEN` apenas em desenvolvimento.
4. Valide Auth, Firestore e Functions antes de ativar enforcement.
5. Ative enforcement gradualmente para Firestore e Functions após monitorar erros.
