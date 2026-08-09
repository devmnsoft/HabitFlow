(() => {
  'use strict';
  const header = document.querySelector('[data-app-header]');
  if (!header) return;
  const updateShadow = () => header.classList.toggle('is-scrolled', window.scrollY > 6);
  updateShadow(); window.addEventListener('scroll', updateShadow, { passive: true });
  const notificationTrigger = header.querySelector('[data-notification-trigger]');
  if (notificationTrigger) {
    let loaded = false;
    const loadNotifications = async () => {
      if (loaded) return; loaded = true;
      const preview = document.querySelector('[data-notification-preview]');
      try {
        const [countResponse, previewResponse] = await Promise.all([fetch('/notifications/unread-count'), fetch('/notifications/preview')]);
        if (!countResponse.ok || !previewResponse.ok) throw new Error('request');
        const data = await countResponse.json();
        const badge = document.querySelector('[data-notification-count]');
        if (badge && Number(data.count) > 0) { badge.textContent = data.count > 99 ? '99+' : String(data.count); badge.hidden = false; }
        if (preview) preview.replaceChildren(document.createRange().createContextualFragment(await previewResponse.text()));
      } catch { if (preview) preview.textContent = 'Não foi possível carregar as notificações agora.'; }
    };
    notificationTrigger.addEventListener('show.bs.dropdown', loadNotifications);
    notificationTrigger.addEventListener('click', loadNotifications, { once: true });
  }
})();
