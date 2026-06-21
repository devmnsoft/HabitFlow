# Telegram do HabitFlow

Dados do bot:
- Bot: @hablitflowmns_bot
- Chat ID admin: 7535235489

A integração envia alertas reais do HabitFlow para o Administrador Geral usando somente Firebase Functions. O token do bot nunca deve ser colocado no frontend, no Firestore, no README, no CHANGELOG, no DEPLOY ou em qualquer arquivo público.

## Como configurar o token com segurança

1. Crie ou acesse o bot no `@BotFather`.
2. Copie o token do bot apenas para um ambiente seguro.
3. Configure `TELEGRAM_BOT_TOKEN` somente nas Firebase Functions.
4. Não imprima o token em logs, respostas HTTP/callable, telas do Admin Geral ou documentação.

## Como configurar TELEGRAM_ADMIN_CHAT_ID

Configure o Chat ID administrativo nas Functions:

```env
TELEGRAM_ADMIN_CHAT_ID=7535235489
```

O Chat ID pode aparecer em exemplos porque não permite controlar o bot sem o token. Mesmo assim, não coloque o token real junto dele em arquivos versionados.

## Como usar .env local

1. Copie `functions/.env.example` para `functions/.env`.
2. Preencha o token real somente no arquivo local:

```env
TELEGRAM_ENABLED=true
TELEGRAM_BOT_TOKEN=COLOQUE_AQUI_O_TOKEN_REAL
TELEGRAM_ADMIN_CHAT_ID=7535235489
TELEGRAM_MIN_SEVERITY=warning
TELEGRAM_NOTIFY_EVENTS=critical,error,checkout_failed,webhook_error,premium_interest,user_signup,frontend_error,backend_error,unauthorized_admin_attempt
```

3. Confirme que `functions/.gitignore` contém `.env` e `.env.local`.

## Como usar Firebase Secrets em produção

Em produção, prefira Firebase Functions secrets ou uma configuração segura equivalente. Exemplo operacional:

```bash
firebase functions:secrets:set TELEGRAM_BOT_TOKEN
firebase functions:secrets:set TELEGRAM_ADMIN_CHAT_ID
```

Depois associe os secrets às Functions conforme a configuração do projeto e faça deploy. Variáveis não sensíveis como `TELEGRAM_ENABLED`, `TELEGRAM_MIN_SEVERITY` e `TELEGRAM_NOTIFY_EVENTS` também podem ser configuradas pelo ambiente seguro das Functions.

## Como testar pelo Admin Geral

1. Configure `ADMIN_EMAILS` com o e-mail do Administrador Geral.
2. Rode as Functions localmente ou publique em produção.
3. Faça login no HabitFlow como Administrador Geral.
4. Abra a aba **Admin Geral**.
5. Na seção **Telegram**, clique em **Testar Telegram**.
6. Confirme a chegada da mensagem no Telegram.

A mensagem de teste esperada é:

```text
✅ HabitFlow Telegram configurado com sucesso.

Bot: @hablitflowmns_bot
Ambiente: development/production
Versão: 1.7
Data: data/hora
```

## Quais eventos são enviados

- `critical`
- `error`
- `frontend_error`
- `backend_error`
- `firebase_error`
- `checkout_failed`
- `webhook_error`
- `unauthorized_admin_attempt`
- `premium_interest`
- `user_signup`
- `admin_set_user_plan`
- `payment_confirmed` futuramente
- `payment_failed` futuramente

## Como evitar vazamento de token

- Nunca use `TELEGRAM_BOT_TOKEN` em `assets/js/firebase.js`, `assets/js/app.js`, HTML, service worker ou qualquer arquivo público.
- Nunca salve o token no Firestore.
- Nunca retorne o token em callable functions.
- Nunca registre o token no console ou em logs das Functions.
- Nunca coloque o token real em README, CHANGELOG, DEPLOY ou documentação.
- Mantenha `functions/.env` fora do Git.

## Como revogar e gerar novo token no BotFather

1. Abra conversa com `@BotFather`.
2. Use `/mybots`.
3. Selecione `@hablitflowmns_bot`.
4. Acesse **API Token**.
5. Use **Revoke current token** para invalidar o token antigo.
6. Copie o novo token apenas para o ambiente seguro das Functions.
7. Faça novo deploy/restart das Functions.

## Checklist de segurança

- [ ] Token não está no frontend.
- [ ] Token não está no GitHub.
- [ ] Token não aparece no console.
- [ ] Token não aparece no Firestore.
- [ ] Token não aparece nas respostas de Functions.
- [ ] `.env` está no `functions/.gitignore`.
- [ ] Produção usa Firebase Secrets ou configuração segura equivalente.
- [ ] Teste pelo Admin Geral funcionou.
- [ ] Erro simulado chegou no Telegram.
- [ ] Interesse Premium chegou no Telegram.
