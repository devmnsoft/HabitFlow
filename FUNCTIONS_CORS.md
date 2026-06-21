# HabitFlow v1.9.1 — Functions, CORS e chamadas seguras

## onCall/httpsCallable vs onRequest

- Use `onCall` + `httpsCallable` para funções internas do app com Firebase Auth, como `getPublicSystemSettings`, `logSystemEvent`, suporte, chatbot, admin e `healthCheck`.
- Não chame callable functions com `fetch`, porque o protocolo callable do Firebase inclui envelope, autenticação e tratamento de erros próprios.
- Use `onRequest` para webhooks e endpoints HTTP públicos, como `paymentWebhook`.

## CORS em onRequest

O utilitário `functions/cors.js` aplica CORS para endpoints HTTP chamados por navegador:

- `Access-Control-Allow-Origin` somente para origem permitida.
- `Access-Control-Allow-Methods: GET,POST,OPTIONS`.
- `Access-Control-Allow-Headers: Content-Type,Authorization`.
- `Access-Control-Max-Age: 3600`.
- `OPTIONS` retorna `204`.

Origens padrão:

- `http://localhost:5177`
- `http://127.0.0.1:5177`
- `https://habitflow-5f945.web.app`
- `https://habitflow-5f945.firebaseapp.com`

Adicione domínio próprio com `APP_ALLOWED_ORIGINS`.

## Diagnóstico

Teste preflight com `curl -i -X OPTIONS <url> -H "Origin: http://localhost:5177" -H "Access-Control-Request-Method: POST"`.
