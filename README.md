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

Eventos pessoais: `users/{userId}/usage/events/{eventId}`.

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
