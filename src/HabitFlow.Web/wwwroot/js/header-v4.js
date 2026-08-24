(() => {
  'use strict';
  const header = document.querySelector('[data-header-root]');
  if (!header) return;

  const syncShadow = () => header.classList.toggle('is-scrolled', window.scrollY > 8);
  addEventListener('scroll', syncShadow, { passive: true });
  syncShadow();

  const dropdowns = [...header.querySelectorAll('[data-bs-toggle="dropdown"]')];
  const hideOtherDropdowns = current => dropdowns.forEach(trigger => {
    if (trigger !== current && window.bootstrap?.Dropdown) window.bootstrap.Dropdown.getOrCreateInstance(trigger).hide();
  });
  dropdowns.forEach(trigger => trigger.closest('.dropdown')?.addEventListener('show.bs.dropdown', () => hideOtherDropdowns(trigger)));

  // A page restored from the back/forward cache can retain Popper's inline
  // position and Bootstrap's `show` class. Always restore the closed contract;
  // otherwise the public "Mais" menu becomes a large floating white panel.
  const resetHeaderOverlays = () => {
    header.querySelectorAll('.dropdown-menu.show').forEach(menu => {
      menu.classList.remove('show');
      menu.removeAttribute('data-popper-placement');
      menu.style.removeProperty('position');
      menu.style.removeProperty('inset');
      menu.style.removeProperty('margin');
      menu.style.removeProperty('transform');
    });
    dropdowns.forEach(trigger => {
      trigger.classList.remove('show');
      trigger.setAttribute('aria-expanded', 'false');
    });
    const drawer = document.getElementById('headerDrawer');
    drawer?.classList.remove('show', 'showing');
    drawer?.setAttribute('aria-hidden', 'true');
    document.querySelectorAll('.offcanvas-backdrop').forEach(backdrop => backdrop.remove());
  };
  resetHeaderOverlays();

  const previews = [...header.querySelectorAll('[data-notification-preview]')];
  let notificationRequest;
  const renderPreview = content => previews.forEach(preview => {
    preview.replaceChildren(typeof content === 'string' ? document.createTextNode(content) : content.cloneNode(true));
  });
  const loadNotifications = () => notificationRequest ??= (async () => {
    renderPreview('Carregando notificações…');
    try {
      const [countResponse, previewResponse] = await Promise.all([fetch('/notifications/unread-count'), fetch('/notifications/preview')]);
      if (!countResponse.ok || !previewResponse.ok) throw new Error('notification request failed');
      const { count = 0 } = await countResponse.json();
      header.querySelectorAll('[data-notification-count]').forEach(badge => {
        badge.textContent = count > 99 ? '99+' : String(count);
        badge.hidden = Number(count) < 1;
      });
      const markup = (await previewResponse.text()).trim();
      if (!markup) return renderPreview('Você não tem novas notificações');
      const fragment = document.createRange().createContextualFragment(markup);
      renderPreview(fragment);
    } catch {
      notificationRequest = undefined;
      renderPreview('Não foi possível carregar as notificações agora. Tente novamente.');
    }
  })();
  header.querySelectorAll('[data-notification-trigger]').forEach(trigger => {
    trigger.closest('.dropdown')?.addEventListener('show.bs.dropdown', loadNotifications);
  });

  header.querySelectorAll('[data-header-dropdown-close]').forEach(button => button.addEventListener('click', () => {
    const trigger = button.closest('.dropdown')?.querySelector('[data-bs-toggle="dropdown"]');
    if (!trigger || !window.bootstrap?.Dropdown) return;
    window.bootstrap.Dropdown.getOrCreateInstance(trigger).hide();
    trigger.focus();
  }));

  const drawer = document.getElementById('headerDrawer');
  drawer?.addEventListener('click', event => {
    if (!event.target.closest('a[href]') || !window.bootstrap?.Offcanvas) return;
    window.bootstrap.Offcanvas.getOrCreateInstance(drawer).hide();
  });
  addEventListener('pageshow', () => {
    resetHeaderOverlays();
    document.body.classList.remove('modal-open');
    document.body.style.removeProperty('overflow');
  });
})();
