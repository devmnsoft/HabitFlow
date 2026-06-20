# HabitFlow — Rastreador de Hábitos MVP SaaS

HabitFlow é um rastreador de hábitos simples, leve e mobile first. O foco do produto é ajudar usuários a manter consistência diária usando streaks, histórico visual e uma experiência rápida, bonita e direta.

## Visão do projeto

O MVP foi criado para validação com usuários reais mantendo uma stack simples: HTML5, CSS3, Bootstrap 5, Bootstrap Icons, JavaScript Vanilla com módulos ES, Firebase Authentication e Cloud Firestore. A aplicação está preparada para Firebase Hosting e roda localmente na porta **5177**.

## Funcionalidades

- Landing page responsiva com proposta de valor comercial.
- Login com Google.
- Login e cadastro com email/senha.
- Dashboard autenticado com saudação do usuário.
- Cards de resumo: total de hábitos, feitos hoje, melhor streak geral e percentual de conclusão diária.
- Criar, editar e excluir hábitos.
- Escolha de cor para cada hábito.
- Marcar e desmarcar hábito como feito no dia atual.
- Limite de 5 hábitos no plano gratuito.
- Estrutura preparada para plano Premium futuro com hábitos ilimitados.
- Cálculo de streak atual e maior streak histórico.
- Mini calendário visual dos últimos 30 dias.
- Onboarding para usuários sem hábitos com sugestões rápidas.
- Toasts amigáveis para sucesso, alerta e erro.
- PWA básico com `manifest.json`, ícone SVG e `theme-color`.
- Dados isolados por usuário em `users/{userId}/habits/{habitId}`.

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
├── iniciar-local.bat
├── iniciar-local.ps1
├── README.md
└── assets/
    ├── css/
    │   └── style.css
    ├── icons/
    │   └── icon.svg
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

> Importante: o projeto não usa a porta 8088.

## Como configurar Firebase Auth

No Console do Firebase do projeto `habitflow-5f945`:

1. Acesse **Authentication > Sign-in method**.
2. Habilite **Google**.
3. Habilite **Email/Password**.
4. Em **Authentication > Settings > Authorized domains**, confirme que `localhost` está autorizado para testes locais.

## Como configurar Cloud Firestore

1. Acesse **Firestore Database**.
2. Crie o banco em modo produção.
3. Publique as regras do arquivo `firestore.rules`.

Estrutura usada pela aplicação:

```text
users/{userId}/habits/{habitId}
```

Regra principal:

```js
allow read, write: if request.auth != null && request.auth.uid == userId;
```

Assim, cada usuário autenticado só acessa os próprios hábitos.

## Como publicar no Firebase Hosting

Instale e autentique o Firebase CLI:

```bash
npm install -g firebase-tools
firebase login
```

Depois publique:

```bash
firebase deploy
```

O arquivo `firebase.json` já aponta o diretório público para a raiz do projeto e mantém fallback para `index.html`.

## Próximas evoluções

- Plano Premium com hábitos ilimitados.
- Pagamentos e controle de assinatura.
- Notificações e lembretes diários.
- Estatísticas semanais e mensais.
- Service worker com cache offline seguro.
- Testes automatizados de UI e regras do Firestore.
