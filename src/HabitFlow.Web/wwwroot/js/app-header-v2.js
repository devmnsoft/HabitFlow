(() => {
  'use strict';
  const header = document.querySelector('[data-app-header]');
  if (!header) return;
  const updateShadow = () => header.classList.toggle('is-scrolled', window.scrollY > 6);
  updateShadow(); window.addEventListener('scroll', updateShadow, { passive: true });
  const drawerElement = document.getElementById('appHeaderDrawer');
  if (drawerElement) {
    drawerElement.addEventListener('click', (event) => {
      const link = event.target.closest('a[href]');
      if (!link || !window.bootstrap?.Offcanvas) return;
      window.bootstrap.Offcanvas.getOrCreateInstance(drawerElement).hide();
    });
  }
  const notificationTriggers = header.querySelectorAll('[data-notification-trigger]');
  if (notificationTriggers.length) {
    let loaded = false;
    const loadNotifications = async () => {
      if (loaded) return; loaded = true;
      const previews = document.querySelectorAll('[data-notification-preview]');
      try {
        const [countResponse, previewResponse] = await Promise.all([fetch('/notifications/unread-count'), fetch('/notifications/preview')]);
        if (!countResponse.ok || !previewResponse.ok) throw new Error('request');
        const data = await countResponse.json();
        document.querySelectorAll('[data-notification-count]').forEach((badge) => {
          if (Number(data.count) > 0) { badge.textContent = data.count > 99 ? '99+' : String(data.count); badge.hidden = false; }
        });
        const markup = await previewResponse.text();
        previews.forEach((preview) => preview.replaceChildren(document.createRange().createContextualFragment(markup)));
      } catch { previews.forEach((preview) => { preview.textContent = 'Não foi possível carregar as notificações agora.'; }); }
    };
    notificationTriggers.forEach((trigger) => {
      trigger.closest('.dropdown')?.addEventListener('show.bs.dropdown', loadNotifications);
      trigger.addEventListener('click', loadNotifications, { once: true });
    });
  }
})();
