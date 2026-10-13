(() => {
  if ('serviceWorker' in navigator) window.addEventListener('load', async () => {
    const registration = await navigator.serviceWorker.register('/service-worker.js');
    // Recarrega SOMENTE quando o usuário aceitou uma atualização (skipWaiting).
    // Sem isso, a primeira visita recarregava sozinha: controllerchange também
    // dispara na instalação inicial, quando não havia controller anterior.
    let updateAccepted = false;
    registration.addEventListener('updatefound', () => {
      const worker = registration.installing;
      worker?.addEventListener('statechange', async () => {
        if (worker.state !== 'installed' || !navigator.serviceWorker.controller) return;
        const accepted = await window.HabitFlowFeedback?.confirm({
          title: 'Atualização disponível',
          message: 'Uma atualização do HabitFlow está pronta. Atualizar agora?',
          confirmLabel: 'Atualizar agora',
          cancelLabel: 'Mais tarde'
        });
        if (accepted) { updateAccepted = true; worker.postMessage({ type: 'SKIP_WAITING' }); }
      });
    });
    navigator.serviceWorker.addEventListener('controllerchange', () => { if (updateAccepted) window.location.reload(); });
  });
  let prompt;
  const button = document.querySelector('[data-pwa-install]');
  window.addEventListener('beforeinstallprompt', event => { event.preventDefault(); prompt = event; if (button) button.hidden = false; });
  button?.addEventListener('click', async () => { if (!prompt) return; prompt.prompt(); await prompt.userChoice; prompt = null; button.hidden = true; });
  window.addEventListener('appinstalled', () => {
    if (button) button.hidden = true;
    prompt = null;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    fetch('/product-events/pwa-installed', { method: 'POST', headers: { RequestVerificationToken: token } }).catch(() => {});
  });
})();
