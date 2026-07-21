(() => {
  'use strict';
  const ready = (fn) => document.readyState !== 'loading' ? fn() : document.addEventListener('DOMContentLoaded', fn);
  ready(() => {
    document.querySelectorAll('[data-hf-toast]').forEach((el) => showToast(el.getAttribute('data-hf-toast') || 'Atualizado'));
    document.querySelectorAll('.alert').forEach((alert) => setTimeout(() => bootstrap.Alert.getOrCreateInstance(alert).close(), 6500));
    document.querySelectorAll('form').forEach((form) => form.addEventListener('submit', () => {
      const btn = form.querySelector('button[type="submit"],button:not([type])');
      if (btn && !btn.dataset.noLoading) { btn.dataset.originalText = btn.textContent; btn.textContent = btn.dataset.loadingText || 'Processando...'; btn.disabled = true; }
    }));
    document.querySelectorAll('[data-confirm]').forEach((el) => el.addEventListener('click', (ev) => {
      ev.preventDefault(); confirmAction(el.getAttribute('data-confirm') || 'Confirmar ação?', () => el.closest('form') ? el.closest('form').submit() : window.location.assign(el.href));
    }));
    document.querySelectorAll('[data-copy]').forEach((btn) => btn.addEventListener('click', async () => { await navigator.clipboard.writeText(btn.getAttribute('data-copy') || ''); showToast('Copiado com segurança.'); }));
    document.querySelectorAll('.hf-day-toggle input').forEach((input) => input.addEventListener('change', () => input.closest('.hf-day-toggle').classList.toggle('is-selected', input.checked)));
  });
  function showToast(message) { const host = document.getElementById('hfToastHost'); if (!host || !window.bootstrap) return; const node = document.createElement('div'); node.className = 'toast hf-toast'; node.setAttribute('role','status'); const body = document.createElement('div'); body.className = 'toast-body'; body.textContent = message; node.appendChild(body); host.appendChild(node); bootstrap.Toast.getOrCreateInstance(node, { delay: 4500 }).show(); }
  function confirmAction(message, onConfirm) { const modalEl = document.getElementById('hfConfirmModal'); if (!modalEl || !window.bootstrap) { if (confirm(message)) onConfirm(); return; } document.getElementById('hfConfirmMessage').textContent = message; const action = document.getElementById('hfConfirmAction'); const clone = action.cloneNode(true); action.replaceWith(clone); clone.addEventListener('click', () => { bootstrap.Modal.getInstance(modalEl).hide(); onConfirm(); }); bootstrap.Modal.getOrCreateInstance(modalEl).show(); }
})();
