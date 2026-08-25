(() => {
  'use strict';
  const closeStaleOverlays = () => {
    document.querySelectorAll('.modal.show, .offcanvas.show').forEach(element => {
      if (element.matches('[data-show-on-load="true"]')) return;
      element.classList.remove('show', 'showing');
      element.setAttribute('aria-hidden', 'true');
    });
    document.querySelectorAll('.dropdown-menu.show').forEach(menu => menu.classList.remove('show'));
    document.querySelectorAll('[aria-expanded="true"][data-bs-toggle="dropdown"]').forEach(button => button.setAttribute('aria-expanded', 'false'));
    document.querySelectorAll('.modal-backdrop, .offcanvas-backdrop').forEach(backdrop => backdrop.remove());
    document.body.classList.remove('modal-open');
    document.body.style.removeProperty('overflow');
    document.body.style.removeProperty('padding-right');
  };
  closeStaleOverlays();
  addEventListener('pageshow', closeStaleOverlays);

  document.addEventListener('hidden.bs.dropdown', event => event.target.querySelector('[data-bs-toggle="dropdown"]')?.focus());
  document.addEventListener('hidden.bs.offcanvas', event => {
    const trigger = document.querySelector(`[data-bs-target="#${CSS.escape(event.target.id)}"]`);
    trigger?.focus();
  });
})();
