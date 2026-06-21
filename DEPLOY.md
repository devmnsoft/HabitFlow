# Deploy HabitFlow v1.4

## Testar localmente antes do deploy

1. Instale dependências do Node.js, se necessário.
2. Rode o servidor local estático:

```bash
npm start
```

3. Acesse `http://localhost:5177`.
4. Abra o console do navegador e confirme que não há erros críticos.
5. Valide landing, autenticação, CRUD de hábitos, PWA e regras de privacidade.

## Firebase CLI

### 1. Instalar Firebase CLI

```bash
npm install -g firebase-tools
```

### 2. Fazer login

```bash
firebase login
```

### 3. Conferir projeto

```bash
firebase projects:list
```

Confirme que o projeto correto é `habitflow-5f945`.

### 4. Inicializar, se necessário

```bash
firebase init hosting firestore
```

Use a raiz do projeto (`.`) como pasta pública e mantenha o fallback para `index.html`.

### 5. Publicar regras

```bash
firebase deploy --only firestore:rules
```

### 6. Publicar hosting

```bash
firebase deploy --only hosting
```

### 7. Deploy completo

```bash
firebase deploy
```

### 8. URL final

```text
https://habitflow-5f945.web.app
```

## Validações pós-deploy

### Login Google

- Em Firebase Console > Authentication > Sign-in method, confirme o provedor Google habilitado.
- Em Authentication > Settings > Authorized domains, adicione `habitflow-5f945.web.app` e qualquer domínio próprio futuro.
- Teste login, logout e retorno ao dashboard.

### Firestore

- Verifique criação de `users/{uid}/profile/main`.
- Crie, edite, marque/desmarque e exclua hábitos.
- Confirme eventos em `users/{uid}/usage/events`.
- Publique `firestore.rules` antes de abrir para usuários reais.

### PWA

- Abra DevTools > Application.
- Valide `manifest.json`, service worker ativo e cache estático.
- Confirme que o service worker não intercepta APIs do Firebase de forma destrutiva.

## Correção de domínio autorizado no Firebase Auth

Se o login Google falhar em produção:

1. Abra Firebase Console.
2. Vá em Authentication > Settings > Authorized domains.
3. Adicione o domínio do Hosting ou domínio próprio.
4. Aguarde alguns minutos e teste novamente em janela anônima.
