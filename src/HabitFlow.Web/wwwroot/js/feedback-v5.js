(() => {
  'use strict';
  const host = document.getElementById('hfToastHost');
  const dialog = document.getElementById('hfConfirmationDialog');
  const severityLabels = { success: 'Sucesso', error: 'Erro', warning: 'Atenção', info: 'Informação' };

  const show = ({ severity = 'info', title, message } = {}) => {
    if (!host) return;
    const safeSeverity = Object.hasOwn(severityLabels, severity) ? severity : 'info';
    const toast = document.createElement('div');
    toast.className = `toast hf-toast hf-toast--${safeSeverity}`;
    toast.setAttribute('role', safeSeverity === 'error' ? 'alert' : 'status');
    toast.setAttribute('aria-live', safeSeverity === 'error' ? 'assertive' : 'polite');
    const content = document.createElement('div');
    content.className = 'hf-toast__content';
    const heading = document.createElement('strong');
    heading.textContent = title || severityLabels[safeSeverity];
    const body = document.createElement('p');
    body.textContent = message || '';
    const close = document.createElement('button');
    close.type = 'button'; close.className = 'btn-close'; close.setAttribute('aria-label', 'Fechar');
    close.addEventListener('click', () => window.bootstrap?.Toast.getOrCreateInstance(toast).hide());
    content.append(heading, body); toast.append(content, close); host.append(toast);
    toast.addEventListener('hidden.bs.toast', () => toast.remove(), { once: true });
    window.bootstrap?.Toast.getOrCreateInstance(toast, { delay: safeSeverity === 'error' ? 7000 : 4500 }).show();
  };

  const confirmAction = ({ title = 'Confirmar ação', message = 'Deseja continuar?', confirmLabel = 'Confirmar', cancelLabel = 'Cancelar', destructive = false } = {}) => new Promise(resolve => {
    if (!dialog || !window.bootstrap?.Modal) { resolve(false); return; }
    const modal = window.bootstrap.Modal.getOrCreateInstance(dialog, { backdrop: 'static', keyboard: true });
    const confirmButton = dialog.querySelector('[data-confirm-submit]');
    const cancelButton = dialog.querySelector('[data-bs-dismiss="modal"]');
    dialog.querySelector('#hfConfirmationTitle').textContent = title;
    dialog.querySelector('#hfConfirmationText').textContent = message;
    confirmButton.textContent = confirmLabel; cancelButton.textContent = cancelLabel;
    confirmButton.classList.toggle('btn-danger', destructive);
    confirmButton.classList.toggle('btn-success', !destructive);
    let settled = false;
    const finish = value => { if (settled) return; settled = true; resolve(value); modal.hide(); };
    confirmButton.addEventListener('click', () => finish(true), { once: true });
    dialog.addEventListener('hidden.bs.modal', () => finish(false), { once: true });
    modal.show();
  });

  window.HabitFlowFeedback = { show, confirm: confirmAction };

  let trigger = null;
  document.addEventListener('click', async event => {
    const button = event.target.closest('[data-feedback-confirm]');
    if (!button) return;
    event.preventDefault(); trigger = button;
    const accepted = await confirmAction({ title: button.dataset.confirmTitle, message: button.dataset.confirmMessage, confirmLabel: button.dataset.confirmLabel, destructive: button.dataset.confirmDestructive !== 'false' });
    if (accepted) document.getElementById(button.dataset.form)?.requestSubmit();
    else trigger?.focus();
  });
})();
