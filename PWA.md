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
