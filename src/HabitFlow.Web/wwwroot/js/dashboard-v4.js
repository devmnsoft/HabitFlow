(() => {
  const root = document.querySelector('[data-dashboard]');
  if (!root) return;
  root.querySelectorAll('form').forEach(form => form.addEventListener('submit', () => {
    const button = form.querySelector('button[type="submit"]');
    if (!button || button.disabled) return;
    button.disabled = true;
    button.setAttribute('aria-busy', 'true');
    const live = root.querySelector('[data-dashboard-status]');
    if (live) live.textContent = 'Salvando sua ação…';
  }));
})();
