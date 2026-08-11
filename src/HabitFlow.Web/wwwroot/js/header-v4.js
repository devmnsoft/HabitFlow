(() => {
  const header = document.querySelector('[data-header-root]');
  if (!header) return;
  const sync = () => header.classList.toggle('is-scrolled', window.scrollY > 8);
  addEventListener('scroll', sync, { passive: true }); sync();
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape') document.activeElement?.blur();
  });
})();
