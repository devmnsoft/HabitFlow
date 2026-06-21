# HabitFlow v1.9.1 — PWA install prompt

O evento `beforeinstallprompt` é interceptado somente para guardar o prompt e exibir o botão **Instalar app**. O app não chama `prompt()` automaticamente.

Fluxo correto:

1. `beforeinstallprompt` chega.
2. O app chama `preventDefault()` e salva o evento em `deferredInstallPrompt`.
3. O botão de instalação aparece.
4. No clique do usuário, o app chama `deferredInstallPrompt.prompt()`.
5. O resultado `userChoice.outcome` é registrado via logger resiliente.
6. O evento é limpo e o botão é ocultado.

Se o navegador não suportar instalação PWA ou não emitir o evento, nenhum erro técnico é exibido ao usuário.
