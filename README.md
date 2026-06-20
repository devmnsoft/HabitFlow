# HabitFlow — Rastreador de Hábitos SaaS MVP

HabitFlow é um rastreador de hábitos simples, bonito e mobile first. Ele ajuda usuários a criar hábitos, marcar conclusões diárias, manter streaks, visualizar progresso e sentir recompensa cotidiana sem complexidade.

## Visão geral

A versão v1.3 aproxima o produto de um SaaS validável mantendo uma stack leve: HTML5, CSS3, Bootstrap 5, Bootstrap Icons, JavaScript Vanilla com módulos ES, Firebase Authentication, Cloud Firestore e Node.js apenas para servir arquivos estáticos localmente.

## Funcionalidades

- Landing page comercial responsiva.
- Login com Google.
- Login e cadastro com e-mail/senha.
- Dashboard autenticado com abas Hoje, Progresso, Perfil e Admin futuro.
- Criar, editar e excluir hábitos.
- Categorias e cores por hábito.
- Marcar e desmarcar conclusão do dia atual.
- Streak atual, maior streak e histórico visual dos últimos 30 dias.
- Plano gratuito limitado a 5 hábitos.
- Preparação visual e lógica para Premium futuro.
- Interesse em Premium salvo em Firestore.
- Eventos simples de uso para métricas futuras.
- Insights pessoais e ranking de hábitos.
- PWA básico com `manifest.json` e `service-worker.js`.

## Novidades da v1.3

- Hero com proposta comercial: “Construa hábitos melhores, um dia de cada vez.”
- Benefícios, como funciona, validação e planos na landing.
- Modelo de perfil em `users/{userId}/profile/main`.
- Função `getUserPlan(userId)` para preparar planos free/premium.
- Botão “Quero ser avisado” no Premium.
- Métricas em `users/{userId}/usage/events/{eventId}`.
- Insights pessoais na aba Progresso.
- Ranking ordenado por streak e total de conclusões.
- Perfil com dados da conta, totais, planos e privacidade.
- Aba Admin exibida somente para e-mails em `ADMIN_EMAILS`.
- Melhorias de UX, acessibilidade, foco visível e textos.

## Estrutura de arquivos

```text
HabitFlow/
├── index.html
├── server.js
├── package.json
├── manifest.json
├── service-worker.js
├── firebase.json
├── firestore.rules
├── CHANGELOG.md
├── README.md
└── assets/
    ├── css/style.css
    ├── icons/icon.svg
    └── js/
        ├── app.js
        └── firebase.js
```

## Como rodar localmente

Requisitos: Node.js 18 ou superior.

```bash
npm start
```

Acesse:

```text
http://localhost:5177
```

> O projeto deve usar obrigatoriamente a porta **5177** e não usa a porta 8088.

## Como configurar Firebase Auth

No Console do Firebase:

1. Abra **Authentication > Sign-in method**.
2. Habilite **Google**.
3. Habilite **Email/Password**.
4. Em **Authentication > Settings > Authorized domains**, confirme `localhost` para testes locais.

## Como configurar Firestore

1. Crie o banco Firestore em modo produção.
2. Publique o arquivo `firestore.rules`.
3. Garanta que o app esteja usando o projeto correto em `assets/js/firebase.js`.

## Modelo de dados Firestore

```text
users/{userId}/profile/main
users/{userId}/habits/{habitId}
users/{userId}/usage/events/{eventId}
```

Perfil:

```js
{
  name: string,
  email: string,
  plan: "free",
  createdAt: Timestamp,
  lastLoginAt: Timestamp,
  wantsPremiumNotice: boolean,
  appVersion: string
}
```

Evento de uso:

```js
{
  type: string,
  createdAt: Timestamp,
  metadata: object
}
```

## Regras de segurança

```js
rules_version = '2';
service cloud.firestore {
  match /databases/{database}/documents {
    match /users/{userId}/{document=**} {
      allow read, write: if request.auth != null && request.auth.uid == userId;
    }
  }
}
```

Essas regras impedem leitura pública, escrita pública e acesso a dados de outros usuários.

## Como publicar no Firebase Hosting

```bash
npm install -g firebase-tools
firebase login
firebase deploy
```

O `firebase.json` já aponta para a raiz do projeto e mantém fallback para `index.html`.

## Como testar PWA

1. Rode `npm start`.
2. Acesse `http://localhost:5177` no Chrome/Edge.
3. Abra DevTools > Application.
4. Confira `manifest.json` e o service worker registrado.
5. Use Lighthouse/PWA para validação básica.

## Próximas evoluções

- Pagamento real e controle de assinatura Premium.
- Relatórios avançados semanais e mensais.
- Desafios de 30 e 90 dias.
- Exportação de dados.
- Temas personalizados.
- Métricas globais administrativas com Cloud Functions e regras adequadas.
- Testes automatizados de UI e regras do Firestore.
