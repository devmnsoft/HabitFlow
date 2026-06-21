# HabitFlow — Domínio Próprio

Placeholder de produção: `https://SEU-DOMINIO.com.br`.

1. Escolha um domínio controlado pela MNSOFT.
2. Firebase Console > Hosting > Add custom domain.
3. Adicione registros DNS exigidos (TXT para verificação e A/AAAA/CNAME conforme Firebase).
4. Aguarde emissão de SSL gerenciado.
5. Firebase Auth > Authorized domains: adicione o domínio final.
6. App Check: cadastre o domínio na site key reCAPTCHA Enterprise/v3 usada pelo frontend.
7. Atualize `PRODUCTION_DOMAIN` e `APP_ALLOWED_ORIGINS` nas Functions.
8. Atualize CSP para incluir somente `https://SEU-DOMINIO.com.br`, Firebase, Google Auth e APIs necessárias.
9. Teste Google Login, PWA, Firestore, Functions e chatbot no domínio.
