document.querySelectorAll('[data-collection-form]').forEach((form) => form.addEventListener('submit', () => {
  const button = form.querySelector('[data-submit]'); const status = form.querySelector('[data-form-status]');
  if (button) { button.disabled = true; button.textContent = 'Ativando…'; }
  if (status) status.textContent = 'Salvando sua coleção.';
}));
