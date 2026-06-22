# HabitFlow v2.3.1-Hotfix — PWA install prompt

O evento `beforeinstallprompt` só deve ser interceptado quando houver botão de instalação na UI.

## Fluxo correto

1. O navegador dispara `beforeinstallprompt`.
2. O app chama `preventDefault()` apenas se `#installCard` e `#btnInstallApp` existirem.
3. O evento é guardado em `deferredInstallPrompt`.
4. O botão de instalação aparece.
5. Somente o clique do usuário chama `prompt()`.
6. `userChoice` é registrado no logger sem exibir erro técnico ao usuário.

Isso evita o warning: “Banner not shown: beforeinstallpromptevent.preventDefault() called”.

## v2.3.2 beforeinstallprompt
- `preventDefault()` só é chamado quando existe botão real `[data-pwa-install]`.
- O evento fica guardado em `deferredInstallPrompt` e `prompt()` é chamado somente após clique do usuário.
- Após `userChoice`, o evento é limpo e o botão é ocultado, sem prompt automático.

## v2.3.4-Audit-Fix-Callable-Cache-Layout
- Frontend interno deve usar `callFunction`/`httpsCallable`; não use `fetch` direto para Functions internas.
- Publique Functions callable e Hosting juntos para evitar CORS por desalinhamento entre frontend e backend.
- Service worker usa cache `habitflow-v2-3-4`; em validação, desregistre o service worker, limpe site data e faça hard reload.
- Admin Geral > Diagnóstico Técnico inclui ações para limpar cache PWA, desregistrar service worker e recarregar a aplicação.

## v2.3.5 cache
`service-worker.js` usa `habitflow-v2-3-5`, apaga caches antigos no activate e não intercepta Firebase, Firestore, Auth, Secure Token, Realtime Database ou Cloud Functions. Para diagnóstico manual: DevTools > Application > Service Workers > Unregister; Clear site data; hard reload.


## v2.4 Cache PRD
O service worker usa `habitflow-v2-4-prd`, apaga caches antigos no activate e não intercepta Firebase Auth, Firestore, Cloud Functions, googleapis, firebaseio/firebaseapp ou source maps. O Admin Geral possui ações para limpar cache PWA, desregistrar SW e recarregar.
