(() => {
  'use strict';
  const root = document.querySelector('[data-my-day]');
  if (!root) return;
  const live = root.querySelector('[data-routine-status]');
  const announce = message => { if (live) live.textContent = message; };

  root.querySelectorAll('[data-routine-form], [data-routine-confirm-form]').forEach(form => {
    form.addEventListener('submit', event => {
      if (form.dataset.submitting === 'true') { event.preventDefault(); return; }
      if (!form.checkValidity()) return;
      form.dataset.submitting = 'true';
      const button = form.querySelector('button[type="submit"]');
      if (button) { button.disabled = true; button.setAttribute('aria-busy', 'true'); button.dataset.label = button.textContent; button.textContent = 'Salvando…'; }
      announce('Salvando sua alteração.');
    });
  });

  root.querySelectorAll('[data-routine-modal]').forEach(trigger => trigger.addEventListener('click', () => {
    const kind = trigger.dataset.routineModal;
    const element = document.getElementById(`routine-${kind}-modal`);
    if (!element || !window.bootstrap?.Modal) return;
    const form = element.querySelector('[data-routine-confirm-form]');
    const name = element.querySelector('[data-routine-name]');
    const version = element.querySelector('[data-routine-version]');
    form.action = form.dataset.actionTemplate.replace('{habitId}', trigger.dataset.habit);
    if (name) name.textContent = trigger.dataset.name || 'este hábito';
    if (version) version.value = trigger.dataset.version || '0';
    window.bootstrap.Modal.getOrCreateInstance(element).show(trigger);
  }));
})();
