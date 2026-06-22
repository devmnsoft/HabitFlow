## Deploy 2.0-Security

1. Configure secrets somente em Firebase Functions/env.
2. Execute `npm run security:scan`.
3. Execute `npm run build`.
4. Valide `dist/` sem `.map`, `sourceMappingURL` ou secrets.
5. Execute `npm run preview` na porta 5177.
6. Execute `npm run deploy`.

Firebase Hosting publica somente `dist/`.

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

## Configurar Telegram nas Firebase Functions

Local:
1. Criar arquivo `functions/.env`.
2. Adicionar:

```env
TELEGRAM_ENABLED=true
TELEGRAM_BOT_TOKEN=
TELEGRAM_ADMIN_CHAT_ID=7535235489
TELEGRAM_MIN_SEVERITY=warning
```

Produção:
Usar Firebase Functions secrets ou configuração segura equivalente.

Checklist:
- Token não está no frontend
- Token não está no GitHub
- Token não aparece no console
- Token não aparece no Firestore
- `.env` está no `.gitignore`
- Teste pelo Admin Geral funcionou
- Erro simulado chegou no Telegram
- Interesse Premium chegou no Telegram

## v1.7.1 — Correção de erro identitytoolkit 400
- Ativar Google em Authentication > Sign-in method.
- Ativar Email/Senha em Authentication > Sign-in method.
- Conferir Authorized domains.
- Adicionar `localhost`.
- Adicionar `habitflow-5f945.web.app`.
- Adicionar domínio próprio futuro, se existir.

## v1.8
Antes do deploy, configure `ADMIN_EMAILS`, `TELEGRAM_BOT_TOKEN`, `TELEGRAM_ADMIN_CHAT_ID` e flags de ambiente das Functions. A porta local permanece `5177` via `npm start`.

## v1.9 — Deploy do chatbot seguro
Antes do deploy, configure as variáveis futuras de IA com `AI_ENABLED=false` quando não houver provedor real. Publique Functions novas (`askHabitFlowAssistant`, `createSupportTicket`, `getMySupportTickets`, `getAdminSupportTickets`, `updateSupportTicketStatus`, `getAdminSupportSummary`) e as regras que bloqueiam leitura direta de `supportTickets`.

## v1.9.1 — variáveis e deploy

Configure `APP_ALLOWED_ORIGINS` nas Functions quando houver domínio próprio. Mantenha secrets como Telegram apenas no backend. Após deploy, teste `healthCheck`, `getPublicSystemSettings`, `logSystemEvent` e `sendTestTelegramAlert` pelo Admin Geral.

## v2.1-SecurityOps
- Adicionada camada operacional de segurança com CI, scanners de secrets/dist, validação de Firebase config e Firestore Rules.
- Admin Geral passa a ter painel de Segurança, eventos suspeitos, incidentes e solicitações LGPD.
- Functions críticas usam rate limit e auditoria administrativa backend; dados globais continuam protegidos por Rules e acessados via Functions.
- Produção deve usar `npm run build`, `npm run security:scan`, `npm run security:dist`, `npm run security:rules`, `npm run security:firebase` e `npm test` antes de publicar.
- Source maps, `.env`, Functions, `node_modules` e documentação interna não devem ser publicados no Hosting.

## Fluxo controlado v2.2

1. Rodar local em `http://localhost:5177`.
2. Rodar testes e validações.
3. `npm run security:scan`.
4. `npm run build`.
5. `npm run preview` na porta 5177.
6. Deploy em canal preview Firebase Hosting.
7. Testar preview.
8. Publicar Functions.
9. Publicar Rules.
10. Publicar Hosting produção.
11. Validar pós-deploy.
12. Registrar deploy no Admin Geral com `registerDeployment`.


## v2.3.1-Hotfix — Callable Functions

- Use `assets/js/functions-client.js` e `callFunction()` para Functions internas.
- Não chame `cloudfunctions.net` com `fetch` no frontend para Functions internas.
- `getPublicSystemSettings`, `logSystemEvent`, `healthCheck` e `sendTestTelegramAlert` precisam estar deployadas como callable/onCall.
- Deploy recomendado:

```bash
cd functions
npm install
firebase deploy --only functions:getPublicSystemSettings
firebase deploy --only functions:logSystemEvent
firebase deploy --only functions:healthCheck
firebase deploy --only functions:sendTestTelegramAlert
firebase deploy --only functions
cd ..
firebase deploy --only hosting
```

## Deploy v2.3.2 callable Functions
```bash
cd functions
npm install
firebase deploy --only functions:getPublicSystemSettings
firebase deploy --only functions:logSystemEvent
firebase deploy --only functions:getMySupportTickets
firebase deploy --only functions:healthCheck
# ou
firebase deploy --only functions
firebase deploy --only hosting
```
Frontend e backend precisam estar alinhados: callable publicado com frontend `httpsCallable`; `onRequest` permanece apenas para webhooks externos.

## v2.3.4-Audit-Fix-Callable-Cache-Layout
- Frontend interno deve usar `callFunction`/`httpsCallable`; não use `fetch` direto para Functions internas.
- Publique Functions callable e Hosting juntos para evitar CORS por desalinhamento entre frontend e backend.
- Service worker usa cache `habitflow-v2-3-4`; em validação, desregistre o service worker, limpe site data e faça hard reload.
- Admin Geral > Diagnóstico Técnico inclui ações para limpar cache PWA, desregistrar service worker e recarregar a aplicação.

## v2.3.5 callable deploy checklist
1. Validar formato local: `npm run verify:functions`.
2. Conferir publicadas: `firebase functions:list`.
3. Publicar Functions callable críticas: `firebase deploy --only functions:getPublicSystemSettings,functions:logSystemEvent,functions:getMySupportTickets,functions:healthCheck`.
4. Se a CLI não aceitar múltiplas Functions, executar uma a uma ou `firebase deploy --only functions`.
5. Publicar Hosting após build/cache: `firebase deploy --only hosting`.
6. Limpar PWA em testes: DevTools > Application > Service Workers > Unregister, Clear site data e hard reload.
