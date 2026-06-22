# Publicação IIS - HabitFlow

## Pré-requisitos

- Node.js 18+.
- IIS com Static Content habilitado.
- IIS URL Rewrite instalado.
- Domínio IIS autorizado no Firebase Authentication e App Check quando aplicável.

## Gerar pacote sem ZIP

```bash
npm run publish:iis:nozip
```

Saída: `publish/iis/HabitFlow-IIS`.

## Publicar no IIS

1. Copie todo o conteúdo de `publish/iis/HabitFlow-IIS` para `C:\inetpub\wwwroot\habitflow`.
2. Confirme que `web.config` está presente.
3. Configure site/aplicação, bindings HTTP/HTTPS e certificado SSL.
4. Adicione o domínio em Firebase Auth > Authorized domains.
5. Garanta que Firestore Rules e Firebase Functions já foram publicadas.

## Testes

- Abrir a home.
- Recarregar rota interna para validar fallback SPA.
- Login Firebase Auth.
- Criar hábito e abrir dashboard.
- Testar chatbot, PWA e console sem erro MIME/CORS.

## Problemas comuns

- **500.19**: instalar IIS URL Rewrite ou corrigir `web.config`.
- **Login bloqueado**: domínio não autorizado no Firebase Auth.
- **Functions/CORS**: publicar Functions e validar origens permitidas.
- **PWA antigo**: limpar cache/service worker no navegador.
