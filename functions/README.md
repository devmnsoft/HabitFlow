## v1.7 — Observabilidade, Admin Geral e Telegram

### Instalação

```bash
npm install
```

### Emulador

```bash
npm run serve
```

### Variáveis importantes

Copie `functions/.env.example` e configure:

- `ADMIN_EMAILS=admin@habitflow.app,marcelo@mnsoft.com.br`
- `TELEGRAM_ENABLED=true`
- `TELEGRAM_BOT_TOKEN=`
- `TELEGRAM_ADMIN_CHAT_ID=7535235489`
- `TELEGRAM_MIN_SEVERITY=warning`
- `TELEGRAM_NOTIFY_EVENTS=critical,error,checkout_failed,webhook_error,premium_interest,user_signup,frontend_error,backend_error,unauthorized_admin_attempt`

### Testes manuais

- Chamar `logSystemEvent` autenticado.
- Entrar como admin e abrir **Admin Geral**.
- Chamar `sendTestTelegramAlert` pelo botão do painel.
- Verificar `getAdminDashboardSummary`, `getAdminRecentLogs`, `getAdminErrorLogs`, `getAdminUserActivitySummary` e `markAuditLogAsRead`.

### Publicação

```bash
npm run deploy
```

### Cuidados com secrets

Nunca exponha `TELEGRAM_BOT_TOKEN`, tokens de pagamento ou secrets no frontend ou em arquivos versionados.

# HabitFlow Functions

Backend Firebase Functions da versão 1.6. Mercado Pago é o gateway preferencial para o Brasil; Stripe fica preparado para uso futuro.

Nunca versione credenciais reais. Em produção, use variáveis/secrets do Firebase Functions. Sem tokens configurados, o checkout retorna modo `mock` controlado.

## Comandos

```bash
npm install
npm run lint
npm run serve
npm run deploy
```
