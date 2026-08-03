(() => {
  'use strict';
  const modalElement = document.getElementById('hfFeedbackModal');
  if (!modalElement || !window.bootstrap?.Modal) return;
  const modal = window.bootstrap.Modal.getOrCreateInstance(modalElement);
  const title = modalElement.querySelector('#hfFeedbackTitle');
  const message = modalElement.querySelector('#hfFeedbackMessage');
  const primary = modalElement.querySelector('#hfFeedbackPrimary');
  const secondary = modalElement.querySelector('#hfFeedbackSecondary');
  let origin = null; let critical = false; let submitted = false;

  const reset = () => {
    title.textContent = 'Mensagem do HabitFlow'; message.textContent = '';
    primary.textContent = 'Continuar'; secondary.textContent = 'Fechar';
    primary.dataset.action = ''; primary.removeAttribute('data-hf-busy');
    primary.disabled = false; submitted = false; critical = false;
  };
  const open = trigger => {
    reset(); origin = trigger;
    title.textContent = trigger.dataset.modalTitle || 'Confirme esta ação';
    message.textContent = trigger.dataset.modalMessage || trigger.dataset.hfModal || 'Deseja continuar?';
    primary.textContent = trigger.dataset.modalConfirm || 'Confirmar';
    secondary.textContent = trigger.dataset.modalCancel || 'Cancelar';
    primary.dataset.action = trigger.dataset.modalAction || '';
    critical = trigger.dataset.modalCritical === 'true';
    modalElement.dataset.bsBackdrop = critical ? 'static' : 'true';
    modalElement.dataset.bsKeyboard = critical ? 'false' : 'true';
    modal.show();
  };
  document.addEventListener('click', event => {
    const trigger = event.target.closest('[data-hf-modal]');
    if (!trigger) return; event.preventDefault(); open(trigger);
  });
  primary.addEventListener('click', () => {
    if (submitted) return; submitted = true; primary.disabled = true;
    primary.dataset.hfBusy = 'true'; primary.textContent = 'Processando…';
    const action = primary.dataset.action;
    const form = action ? document.querySelector(action) : null;
    if (form instanceof HTMLFormElement) form.requestSubmit(); else modal.hide();
  });
  modalElement.addEventListener('hide.bs.modal', event => { if (critical && submitted) event.preventDefault(); });
  modalElement.addEventListener('hidden.bs.modal', () => { const target = origin; reset(); origin = null; target?.focus(); });
  window.HabitFlowModal = Object.freeze({ open });
})();
