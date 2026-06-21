# HabitFlow — Rastreador de hábitos simples

HabitFlow é um micro SaaS de rastreamento de hábitos, criado para ajudar usuários a manter consistência diária, acompanhar streaks, visualizar progresso e construir hábitos melhores com simplicidade.

## Visão geral

A versão v1.4 prepara o HabitFlow para publicação no Firebase Hosting, mantendo a stack leve e sem frameworks front-end. O app funciona como uma SPA simples com landing comercial, autenticação Firebase, dashboard autenticado, persistência por usuário no Cloud Firestore, PWA básico, LGPD inicial e documentação de deploy.

## Funcionalidades atuais

- Landing page comercial responsiva.
- Login com Google.
- Login e cadastro com e-mail/senha.
- Dashboard autenticado com abas Hoje, Progresso, Perfil e Admin inicial.
- Criar, editar e excluir hábitos.
- Modal de confirmação antes de excluir hábitos.
- Categorias e cores por hábito.
- Marcar e desmarcar conclusão do dia atual.
- Streak atual, maior streak e histórico visual dos últimos 30 dias.
- Plano gratuito limitado a 5 hábitos.
- Cards de planos Gratuito e Premium futuro.
- Registro de interesse no Premium.
- Eventos simples de uso para melhoria do produto.
- Insights pessoais e ranking de hábitos.
- Perfil do usuário salvo em `users/{userId}/profile/main`.
- Estados vazios com ícones, texto explicativo e ação recomendada.
- PWA com manifest, service worker e aviso de instalação quando disponível.

## Novidades da v1.4

- Preparação de `firebase.json`, `.firebaserc` e `firestore.rules` para Firebase Hosting e Firestore.
- SEO básico com title, description, keywords, Open Graph, Twitter Card, theme-color e `lang="pt-BR"`.
- Seções institucionais em modais: Sobre, Privacidade, Termos e Contato.
- Card de privacidade no Perfil com link para política de privacidade.
- Tratamento centralizado de erros com `handleAppError(error, friendlyMessage)`.
- Estados vazios refinados para hábitos, progresso, ranking, histórico e admin.
- Preparação para checkout Premium futuro sem implementar pagamento real.
- Checklist de deploy em `DEPLOY.md`.
- Roadmap interno em `ROADMAP.md`.
- `.env.example` para orientar futura migração para variáveis de ambiente em build.

## Stack

- HTML5.
- CSS3.
- Bootstrap 5.
- Bootstrap Icons.
- JavaScript Vanilla com módulos ES.
- Firebase Authentication.
- Cloud Firestore.
- Firebase Hosting.
- PWA básico.
- Node.js apenas para servidor local estático.

O projeto não usa React, Vue, Angular ou Next.js.

## Como rodar localmente

Requisitos: Node.js 18 ou superior.

```bash
npm start
```

Acesse:

```text
http://localhost:5177
```

A porta local obrigatória é **5177**. Não use a porta 8088.

## Estrutura de arquivos

```text
HabitFlow/
├── index.html
├── server.js
├── package.json
├── manifest.json
├── service-worker.js
├── firebase.json
├── .firebaserc
├── .env.example
├── firestore.rules
├── DEPLOY.md
├── ROADMAP.md
├── CHANGELOG.md
├── README.md
└── assets/
    ├── css/style.css
    ├── icons/icon.svg
    └── js/
        ├── app.js
        └── firebase.js
```

## Configuração Firebase

A configuração atual fica em `assets/js/firebase.js` para manter o MVP simples, estático e sem etapa de build. O arquivo `.env.example` documenta as chaves esperadas caso o projeto migre futuramente para um bundler ou pipeline de build com variáveis de ambiente.

### Firebase Authentication

No Console do Firebase:

1. Abra Authentication > Sign-in method.
2. Habilite Google.
3. Habilite Email/Password.
4. Em Authentication > Settings > Authorized domains, confirme `localhost` para testes locais e `habitflow-5f945.web.app` para produção.

### Firestore Database

Modelo de dados:

```text
users/{userId}/profile/main
users/{userId}/habits/{habitId}
users/{userId}/usage/events/{eventId}
```

### Firestore Rules

As regras mantêm isolamento por UID:

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

Não há leitura pública, escrita pública ou acesso entre usuários.

## Firebase Hosting

O `firebase.json` serve a raiz do projeto, ignora arquivos sensíveis/desnecessários para produção e usa rewrite para `index.html`, adequado para uma SPA simples. O `server.js` é apenas para ambiente local e não é necessário em produção.

Consulte `DEPLOY.md` para o passo a passo completo.

## PWA

O app inclui:

- `manifest.json` com `name`, `short_name`, `start_url`, `display`, cores e ícones.
- `service-worker.js` com cache simples dos arquivos estáticos principais.
- Exclusão prática de requisições Firebase/Google do cache para não interferir em Auth ou Firestore.
- Botão discreto “Instalar app” exibido apenas quando `beforeinstallprompt` estiver disponível.

## LGPD e privacidade

- Dados ficam organizados por usuário autenticado.
- As regras Firestore limitam leitura e escrita ao UID autenticado.
- Não há leitura pública dos hábitos.
- Não há compartilhamento de dados entre usuários.
- Eventos de uso são mínimos e usados apenas para melhoria do produto.
- O app evita solicitar ou salvar dados sensíveis desnecessários.
- O Perfil exibe um card de “Privacidade e controle” com acesso à política de privacidade.

## Checklist de testes

- Rodar `npm start` e abrir `http://localhost:5177`.
- Conferir console do navegador sem erros críticos.
- Validar SEO básico no HTML.
- Testar hero, benefícios, como funciona, planos e rodapé.
- Abrir modais Sobre, Privacidade, Termos e Contato.
- Testar login Google.
- Testar cadastro e login e-mail/senha.
- Testar logout.
- Confirmar criação/atualização de `profile/main`.
- Criar, editar e excluir hábito com modal.
- Marcar e desmarcar hábito concluído.
- Registrar interesse Premium.
- Validar eventos de uso.
- Validar manifest e service worker.
- Testar largura mobile próxima de 360px.
- Publicar e validar regras Firestore antes de usuários reais.

## Próximas evoluções

- Pagamento real e controle de assinatura Premium.
- Integração Stripe ou Mercado Pago.
- Histórico completo para assinantes.
- Relatórios avançados semanais e mensais.
- Desafios de 30 dias.
- Notificações e e-mails motivacionais.
- Exportação PDF.
- Painel admin global com métricas agregadas.
- Domínio próprio e suporte.
