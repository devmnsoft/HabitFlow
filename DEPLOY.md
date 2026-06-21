## Checklist v1.7 — Telegram e observabilidade

- Criar bot no BotFather.
- Obter `TELEGRAM_BOT_TOKEN`.
- Obter `TELEGRAM_ADMIN_CHAT_ID`.
- Configurar secrets/env das Functions.
- Publicar functions.
- Acessar Admin Geral.
- Clicar em **Testar Telegram**.
- Simular erro frontend.
- Simular evento `premium_interest`.
- Conferir Telegram.
- Conferir `systemAuditLogs`.
- Conferir Firestore Rules.

### Checklist de segurança

- Token Telegram não aparece no frontend.
- `systemAuditLogs` bloqueado no client.
- `adminAuditLogs` bloqueado no client.
- Apenas admin acessa functions administrativas.
- Metadata sanitizada.
- Nenhum dado sensível salvo.

# Deploy do HabitFlow

## Rodar localmente

```bash
npm install
npm start
```

Acesse <http://localhost:5177>.

## Testar antes do deploy

- Abrir o app localmente.
- Verificar console do navegador.
- Testar login Google e e-mail/senha.
- Criar, editar, arquivar, restaurar e concluir hábitos.
- Testar consentimento LGPD.
- Testar Premium simulado e interesse Premium.
- Testar layout mobile em 360px.
- Testar PWA/manifest/service worker.

## Publicar Hosting

```bash
firebase login
firebase use <project-id>
firebase deploy --only hosting
```

## Publicar regras Firestore

```bash
firebase deploy --only firestore:rules
```

## Domínios autorizados no Firebase Auth

No Firebase Console, acesse Authentication > Settings > Authorized domains e inclua o domínio do Hosting e qualquer domínio próprio futuro.

## Domínio próprio futuramente

No Firebase Hosting, adicione o domínio personalizado, siga a validação DNS e aguarde emissão do certificado SSL.

## Cache PWA

Se usuários receberem versão antiga:

- Atualize o `service-worker.js`.
- Oriente limpar dados do site quando necessário.
- Faça hard reload durante validação interna.

## Checklist pós-deploy

- Hosting abre sem erro.
- Auth funciona no domínio publicado.
- Firestore respeita isolamento por usuário.
- `appMetrics` permanece bloqueado.
- PWA instala quando o navegador permitir.
- Modal de consentimento aparece para usuários sem aceite.
- Plano gratuito limita 5 hábitos ativos.
- Premium simulado está desativado se o ambiente for produção real.

## Firebase Functions (v1.6)

Instalar dependências:

```bash
cd functions
npm install
```

Rodar local:

```bash
firebase emulators:start
```

Deploy functions:

```bash
firebase deploy --only functions
```

Deploy rules:

```bash
firebase deploy --only firestore:rules
```

Deploy hosting:

```bash
firebase deploy --only hosting
```

Deploy completo:

```bash
firebase deploy
```

### Checklist de segurança
- Conferir `.env`/secrets e não versionar credenciais reais.
- Conferir webhook no gateway.
- Conferir domínio autorizado no Firebase Auth.
- Conferir se Functions não expõem secrets.
- Conferir se regras bloqueiam `billingEvents` e `adminAuditLogs`.
