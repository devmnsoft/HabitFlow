(() => {
  'use strict';
  const frequency = document.querySelector('[data-frequency]');
  const weekdays = document.querySelector('[data-weekdays]');
  const syncFrequency = () => {
    if (!frequency || !weekdays) return;
    const custom = frequency.value === 'CustomWeekly';
    weekdays.hidden = !custom;
    weekdays.querySelectorAll('input').forEach(input => { input.disabled = !custom; });
  };
  frequency?.addEventListener('change', syncFrequency);
  syncFrequency();

  let returnFocus = null;
  document.querySelectorAll('[data-modal-open]').forEach(button => button.addEventListener('click', () => {
    const modal = document.getElementById(button.dataset.modalOpen);
    if (!(modal instanceof HTMLDialogElement)) return;
    returnFocus = button;
    modal.showModal();
  }));
  document.querySelectorAll('dialog').forEach(dialog => dialog.addEventListener('close', () => returnFocus?.focus()));

  document.querySelectorAll('[data-habit-editor]').forEach(form => form.addEventListener('submit', () => {
    const button = form.querySelector('[data-submit]');
    if (button && form.checkValidity()) { button.disabled = true; button.textContent = 'Salvando…'; }
  }));
})();
