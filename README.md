## HabitFlow 2.0-Security

Esta versão adiciona hardening de produção: build em `dist/`, CSP/headers, Firestore Rules restritivas, App Check opcional, scanner de secrets e documentação de segurança.

> Por limitação natural da web, qualquer JavaScript executado no navegador pode ser inspecionado. O HabitFlow reduz exposição usando build de produção, minificação, ofuscação, remoção de source maps e, principalmente, movendo lógica sensível para Firebase Functions com validações de segurança no backend.

Comandos principais: `npm start`, `npm run dev`, `npm run build`, `npm run preview`, `npm run security:scan` e `npm run deploy`.

## HabitFlow v1.7 — Observabilidade, Admin Geral e Telegram

A versão 1.7 adiciona observabilidade de produção com logs globais protegidos, painel **Admin Geral**, monitoramento de bugs, captura global de erros frontend, auditoria administrativa e alertas via Telegram enviados exclusivamente pelo backend/Firebase Functions.


### Monitoramento via Telegram

O HabitFlow possui monitoramento via Telegram para o Administrador Geral usando Firebase Functions. Alertas de erros, eventos críticos, interesse Premium, cadastros e tentativas suspeitas são enviados pelo backend para o bot `@hablitflowmns_bot`, sem expor tokens no frontend ou em arquivos públicos.

### Admin Geral

- A aba é exibida apenas para e-mails configurados no frontend como ajuda visual.
- A autorização real acontece nas Functions com `ADMIN_EMAILS`.
- O painel mostra resumo, eventos recentes, bugs, atividades, status do Telegram e botão **Testar Telegram**.

### Segurança e LGPD

- `systemAuditLogs`, `adminAuditLogs`, `billingEvents` e `appMetrics` são bloqueados no cliente.
- Tokens, senhas, CPF, cartão, CVV, secrets e payloads brutos são removidos dos metadados.
- Usuários comuns acessam apenas `users/{uid}/...`.

### Telegram

Configure `TELEGRAM_ENABLED`, `TELEGRAM_BOT_TOKEN`, `TELEGRAM_ADMIN_CHAT_ID`, `TELEGRAM_MIN_SEVERITY` e `TELEGRAM_NOTIFY_EVENTS` nas Functions. Consulte `TELEGRAM_MONITORAMENTO.md`.

### Observabilidade

Consulte `OBSERVABILIDADE.md` para eventos monitorados, retenção, sanitização e próximos passos.

# HabitFlow v1.5

HabitFlow é um micro SaaS de rastreamento de hábitos com foco em simplicidade extrema, consistência diária, streaks e experiência mobile first.

## Funcionalidades atuais

- Landing page comercial e SEO básico.
- Login com Google e e-mail/senha via Firebase Authentication.
- Dashboard autenticado com abas Hoje, Progresso, Perfil e Admin autorizado.
- CRUD de hábitos com categorias, cores, streak atual, maior streak e histórico visual dos últimos 30 dias.
- Arquivamento lógico de hábitos, área de hábitos arquivados e restauração.
- Plano gratuito com limite de 5 hábitos ativos.
- Premium simulado com controle de plano no Firestore e checkout futuro preparado.
- Relatórios pessoais básicos: taxas de 7/30 dias, dias com conclusão, melhor dia e hábitos por frequência.
- Onboarding guiado para primeiro hábito e primeira conclusão.
- Desafios futuros em cards visuais.
- Eventos de uso por usuário, sem IP e sem dados sensíveis.
- PWA básico com manifest e service worker.
- LGPD básica com Termos, Privacidade e modal de consentimento.

## Planos

- **Gratuito:** até 5 hábitos ativos, histórico de 30 dias, streaks, categorias e PWA.
- **Premium futuro:** hábitos ilimitados, histórico completo, relatórios avançados, desafios, temas, exportação futura e prioridade em novidades.

Preços planejados: R$ 14,90/mês ou R$ 99/ano. A versão 1.5 não implementa pagamento real.

## Premium simulado

O arquivo `assets/js/plans.js` possui `ENABLE_DEV_PLAN_TOGGLE = true` para testes locais. Quando ativo, a tela Perfil permite alternar entre gratuito e premium trial. Em produção, desative essa constante até existir backend seguro.

## Como rodar localmente

```bash
npm install
npm start
```

Acesse: <http://localhost:5177>

A porta obrigatória do projeto é **5177**. Não use a porta 8088.

## Configuração Firebase

1. Crie um projeto no Firebase.
2. Ative Firebase Authentication.
3. Habilite Google e e-mail/senha em Auth.
4. Crie o Cloud Firestore.
5. Atualize `assets/js/firebase.js` com a configuração web do projeto, se necessário.
6. Publique `firestore.rules` para manter isolamento por usuário.

## Modelo de dados

Perfil: `users/{userId}/profile/main`.

Hábitos: `users/{userId}/habits/{habitId}`.

Eventos pessoais: `users/{userId}/usageEvents/{eventId}`.

`appMetrics` fica bloqueado nas regras. Métricas globais devem ser gravadas futuramente por backend/Firebase Functions.

## Firebase Hosting

```bash
firebase login
firebase use <project-id>
firebase deploy --only hosting
```

Para regras:

```bash
firebase deploy --only firestore:rules
```

## PWA

O projeto inclui `manifest.json` e `service-worker.js`. Após deploys, se houver cache antigo, atualize o service worker ou limpe os dados do site no navegador.

## LGPD e consentimento

Usuários sem `acceptedTermsAt` e `acceptedPrivacyAt` veem modal obrigatório antes de usar o dashboard. O HabitFlow evita registrar IP, dados sensíveis ou conteúdo privado em eventos de uso.

## Admin

A aba Admin aparece apenas para e-mails em `ADMIN_EMAILS` e mostra somente dados do usuário atual, respeitando as regras de segurança.

## Limitações frontend-only

- Sem checkout real.
- Sem webhook de pagamento.
- Sem painel admin global.
- Sem gravação global de métricas pelo frontend.
- Sem notificações ou e-mails automáticos.

## Próximas evoluções

- Firebase Functions.
- Mercado Pago ou Stripe.
- Webhooks de pagamento.
- Plano Premium real.
- Métricas agregadas seguras.
- Notificações, e-mails e relatórios PDF.

## HabitFlow v1.6 — Backend de pagamentos

A versão 1.6 adiciona Firebase Functions para preparar monetização Premium com Mercado Pago como gateway principal e Stripe como alternativa futura.

### Rodar frontend

```bash
npm start
```

Abra `http://localhost:5177`. A porta 5177 permanece obrigatória.

### Rodar Functions localmente

```bash
cd functions
npm install
npm run lint
npm run serve
```

### Ambiente
Copie `functions/.env.example` para a configuração local/em secrets. Não versione tokens reais. Sem credenciais, o checkout retorna `mode: mock` e mostra mensagem controlada.

### Checkout mock
No app, clique em Assinar mensal/anual. A callable `createCheckoutSession` retorna mock quando não há `MERCADOPAGO_ACCESS_TOKEN` ou `STRIPE_SECRET_KEY` configurado.

### Publicação
- Hosting: `firebase deploy --only hosting`.
- Functions: `firebase deploy --only functions`.
- Firestore rules: `firebase deploy --only firestore:rules`.
- Completo: `firebase deploy`.

### Mercado Pago e webhook
Configure `PAYMENT_PROVIDER=mercadopago`, `MERCADOPAGO_ACCESS_TOKEN` e `MERCADOPAGO_WEBHOOK_SECRET` em secrets. Configure o gateway para chamar `paymentWebhook`. Valide assinatura conforme documentação oficial antes de ativar cobrança real.

### Validação Premium
O plano efetivo considera `profile/main.plan`, `profile/main.planStatus` e `billing/subscription.status`. O frontend não altera plano real; apenas o backend atualiza Premium via webhook ou admin.

### Segurança
Não há secrets no frontend. `billingEvents`, `adminAuditLogs` e `appMetrics` ficam bloqueados para clientes pelas regras do Firestore.

## v1.7.1 — Correção identitytoolkit 400 e eventos pessoais

Eventos pessoais agora são gravados em `users/{userId}/usageEvents/{eventId}`. A coleção registra ações e erros pessoais do usuário com o modelo `{ type, createdAt, metadata, appVersion, environment }`.

### Correção de erro identitytoolkit 400
- Ativar Google em Firebase Console > Authentication > Sign-in method.
- Ativar Email/Senha em Firebase Console > Authentication > Sign-in method.
- Conferir Firebase Console > Authentication > Settings > Authorized domains.
- Adicionar `localhost`.
- Adicionar `habitflow-5f945.web.app`.
- Adicionar domínio próprio futuro, se existir.

### Correção de collection reference inválida
- Não usar `users/{uid}/usage/events` como coleção.
- Usar `users/{uid}/usageEvents` para logs pessoais.
- Logs globais ficam em `systemAuditLogs` e só são acessados por Functions administrativas.

## HabitFlow v1.8 — robustez, suporte e atendimento

A v1.8 adiciona logger centralizado, `safeAsync`, painel administrativo com bugs/status, Assistente HabitFlow seguro baseado em regras, suporte MNSOFT e configuração administrativa de WhatsApp.

Dados institucionais públicos: MNSOFT — MNSOLUÇÕES TECNOLÓGICAS & CONSULTORIA LTDA — CNPJ 18.160.057/0001-13 — comercial@mnsoft.com.br.

Documentação nova: `LOGGER.md`, `CHATBOT.md` e `SUPORTE_WHATSAPP.md`.


## v1.9
Chatbot híbrido seguro, Functions de suporte, tickets, Central de Suporte, Admin Suporte e documentação de IA segura. Porta local: 5177.

## v1.9.1 — estabilidade de Functions e PWA

Esta versão usa `httpsCallable` para Functions internas, fallback local para configurações públicas da MNSOFT, logger remoto com fila local anti-loop e diagnóstico de Functions no Admin Geral. O servidor local permanece em `http://localhost:5177` via `npm start`.

## v2.1-SecurityOps
- Adicionada camada operacional de segurança com CI, scanners de secrets/dist, validação de Firebase config e Firestore Rules.
- Admin Geral passa a ter painel de Segurança, eventos suspeitos, incidentes e solicitações LGPD.
- Functions críticas usam rate limit e auditoria administrativa backend; dados globais continuam protegidos por Rules e acessados via Functions.
- Produção deve usar `npm run build`, `npm run security:scan`, `npm run security:dist`, `npm run security:rules`, `npm run security:firebase` e `npm test` antes de publicar.
- Source maps, `.env`, Functions, `node_modules` e documentação interna não devem ser publicados no Hosting.

## v2.2-Production

Esta versão prepara o HabitFlow para produção comercial controlada com ambientes documentados, App Check progressivo, backup Firestore operacional, LGPD operacional, e-mails transacionais simuláveis, Admin Geral com visão de produção, score de prontidão, deploy controlado e pagamentos Mercado Pago em sandbox.

Arquivos principais:
- `ENVIRONMENTS.md`
- `DOMAIN_SETUP.md`
- `APP_CHECK.md`
- `BACKUP_RECOVERY.md`
- `EMAILS.md`
- `LGPD.md`
- `GO_LIVE_CHECKLIST.md`
- `POST_DEPLOY_CHECKLIST.md`


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

## v2.3.2 Hotfix CORS/Callable/App Check/PWA
O HabitFlow usa `assets/js/functions-client.js` para chamadas internas com Firebase `httpsCallable`, fallback local de configurações públicas da MNSOFT, logger remoto com circuit breaker/fila local, App Check opcional em desenvolvimento e fluxo PWA acionado por clique. Rode localmente em `http://localhost:5177`.
