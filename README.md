# HabitFlow — Rastreador de Hábitos MVP

Projeto web completo do MVP HabitFlow, feito com HTML5, Bootstrap 5, JavaScript Vanilla e Firebase.

## Funcionalidades

- Landing page responsiva.
- Login com Google.
- Login e cadastro com email/senha.
- Cadastro de até 5 hábitos no plano gratuito.
- Edição e exclusão de hábitos.
- Marcação de hábito como feito no dia atual.
- Cálculo de streak atual e maior streak histórico.
- Histórico visual dos últimos 30 dias.
- Dados isolados por usuário no Cloud Firestore.

## Estrutura

```text
habitflow/
├── index.html
├── firebase.json
├── firestore.rules
├── .firebaserc
├── README.md
└── assets/
    ├── css/
    │   └── style.css
    └── js/
        ├── app.js
        └── firebase.js
```

## Configuração obrigatória no Firebase

No console do Firebase, ative:

1. Authentication > Sign-in method > Google.
2. Authentication > Sign-in method > Email/Password.
3. Firestore Database em modo produção.
4. Publique as regras de segurança contidas em `firestore.rules`.

## Como rodar localmente

Como o projeto usa módulos JavaScript, rode com um servidor local. Exemplos:

```bash
# opção 1: Python
python -m http.server 8080

# opção 2: Node
npx serve .
```

Acesse:

```text
http://localhost:8080
```

## Como publicar no Firebase Hosting

Instale o Firebase CLI, faça login e publique:

```bash
npm install -g firebase-tools
firebase login
firebase deploy
```

## Regras de segurança

O projeto usa a estrutura:

```text
users/{userId}/habits/{habitId}
```

Somente o usuário autenticado pode ler e gravar os próprios dados.

## Observação sobre a chave Firebase

A chave `apiKey` do Firebase Web App não é uma senha de banco. A segurança real depende das regras do Firestore e dos provedores de autenticação habilitados corretamente.
