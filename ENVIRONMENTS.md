# HabitFlow — Ambientes v2.2-Production

## local
- URL: `http://localhost:5177`.
- `APP_ENV`: `local` ou `development`.
- Firebase: projeto atual pode ser usado com cuidado; preferir emuladores quando possível.
- App Check: debug token permitido e enforcement desativado.
- Logs: técnicos completos permitidos.
- Pagamento: sandbox/mock; botões de simulação de plano permitidos apenas fora de production.

## staging/homologação
- URL: canal preview do Firebase Hosting ou domínio de teste.
- `APP_ENV`: `staging`.
- App Check: monitoramento, sem enforcement amplo inicialmente.
- Pagamento: `PAYMENT_MODE=sandbox`.
- Telegram/e-mail: configurados para alertas administrativos de teste.
- Logs: completos para Admin Geral.

## production
- URL: `PRODUCTION_DOMAIN=https://SEU-DOMINIO.com.br`.
- `APP_ENV`: `production`.
- App Check: enforcement progressivo (Functions, depois Firestore).
- Logs: sem dados sensíveis e com menor verbosidade.
- Pagamento: cobrança real somente com `PAYMENT_MODE=production` e credenciais configuradas explicitamente.
- Backup: export Firestore registrado em `systemBackups`.

## Variáveis operacionais
- `APP_ALLOWED_ORIGINS`: lista restrita de origens permitidas para Functions/CSP.
- `ADMIN_EMAILS`: admins gerais separados por vírgula.
- `TELEGRAM_BOT_TOKEN` e `TELEGRAM_ADMIN_CHAT_ID`: somente em Functions.
- `EMAIL_PROVIDER`, `RESEND_API_KEY`, `SENDGRID_API_KEY`: somente backend.
