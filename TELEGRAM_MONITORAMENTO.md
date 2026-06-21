# Monitoramento via Telegram no HabitFlow

A integração de Telegram da versão 1.7 envia alertas administrativos do backend/Firebase Functions para o dono do sistema, sem expor token no frontend.

## Objetivo

- Avisar rapidamente sobre erros `error` e `critical`.
- Notificar eventos relevantes como novo cadastro, interesse Premium, falha de checkout, erro de webhook e tentativa admin indevida.
- Manter mensagens curtas, sanitizadas e sem dados sensíveis.

## Criar bot com BotFather

1. Abra o Telegram e procure `@BotFather`.
2. Use `/newbot`.
3. Defina nome e username do bot.
4. Copie o token retornado para `TELEGRAM_BOT_TOKEN` nas variáveis de ambiente das Functions.

## Obter TELEGRAM_ADMIN_CHAT_ID

1. Envie uma mensagem para o bot criado.
2. Consulte `https://api.telegram.org/bot<TELEGRAM_BOT_TOKEN>/getUpdates` em ambiente seguro.
3. Copie o `chat.id` do seu usuário/grupo para `TELEGRAM_ADMIN_CHAT_ID`.
4. Nunca coloque esse valor no frontend.

## Variáveis de ambiente

```env
TELEGRAM_ENABLED=true
TELEGRAM_BOT_TOKEN=coloque_o_token_aqui
TELEGRAM_ADMIN_CHAT_ID=coloque_o_chat_id_aqui
TELEGRAM_MIN_SEVERITY=warning
TELEGRAM_NOTIFY_EVENTS=critical,error,checkout_failed,webhook_error,premium_interest,user_signup
```

## Como testar

1. Configure `ADMIN_EMAILS` com seu e-mail.
2. Publique ou rode as Functions no emulador.
3. Entre no app em `http://localhost:5177` com e-mail admin.
4. Abra a aba **Admin Geral**.
5. Clique em **Testar Telegram**.
6. Simule um erro frontend ou um interesse Premium e confira a mensagem.

## Eventos enviados

- Erro crítico, backend, frontend, Firebase Auth, Firestore e PWA.
- Falha checkout e webhook.
- Novo cadastro.
- Interesse Premium.
- Tentativa de acesso admin não autorizada.
- Alteração manual de plano.
- Eventos futuros de pagamento confirmado e cancelamento.

## O que nunca deve ser enviado

- Senha, token, accessToken, refreshToken, authorization, apiKey ou secrets.
- Cartão, CVV, CPF, documento ou payload bruto de pagamento.
- Payload completo de webhook.

## Desativar e reduzir ruído

- Defina `TELEGRAM_ENABLED=false` para desligar.
- Aumente `TELEGRAM_MIN_SEVERITY=error` para reduzir alertas.
- Remova eventos informativos de `TELEGRAM_NOTIFY_EVENTS`.

## Recomendações de produção

- Use secrets/variáveis seguras das Firebase Functions.
- Restrinja `ADMIN_EMAILS` a contas reais do dono do SaaS.
- Revogue o token no BotFather em caso de suspeita de vazamento.
- Revise periodicamente os alertas para evitar excesso de mensagens.
