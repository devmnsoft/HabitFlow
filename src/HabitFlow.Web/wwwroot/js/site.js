(() => {
  'use strict';
  const storageKey = 'habitflow.ui.preferences';
  const ready = (fn) => document.readyState !== 'loading' ? fn() : document.addEventListener('DOMContentLoaded', fn);
  const normalize = (preferences) => ({ contrastMode: preferences?.contrastMode || preferences?.ContrastMode || 'Default', fontScale: preferences?.fontScale || preferences?.FontScale || 'Normal', reduceMotion: Boolean(preferences?.reduceMotion ?? preferences?.ReduceMotion) });
  window.applyUiPreferences = (preferences) => {
    const p = normalize(preferences);
    document.body.classList.remove('hf-contrast-default', 'hf-contrast-high', 'hf-font-normal', 'hf-font-large', 'hf-reduce-motion');
    document.body.classList.add(p.contrastMode === 'HighContrast' ? 'hf-contrast-high' : 'hf-contrast-default');
    document.body.classList.add(p.fontScale === 'Large' ? 'hf-font-large' : 'hf-font-normal');
    if (p.reduceMotion) document.body.classList.add('hf-reduce-motion');
  };
  window.previewContrastMode = (mode) => window.applyUiPreferences({ ...readLocalPreferences(), contrastMode: mode });
  window.previewFontScale = (scale) => window.applyUiPreferences({ ...readLocalPreferences(), fontScale: scale });
  window.previewReduceMotion = (enabled) => window.applyUiPreferences({ ...readLocalPreferences(), reduceMotion: enabled });
  function readLocalPreferences() { try { return JSON.parse(localStorage.getItem(storageKey) || '{}'); } catch { return {}; } }
  function saveLocalPreferences(preferences) { localStorage.setItem(storageKey, JSON.stringify(normalize(preferences))); }
  function initPasswordToggles() {
    document.querySelectorAll('[data-password-toggle]').forEach((button) => {
      const input = document.querySelector(button.getAttribute('data-password-toggle') || '');
      if (!input) return;
      input.type = 'password';
      button.addEventListener('click', () => {
        const visible = input.type === 'password';
        input.type = visible ? 'text' : 'password';
        button.classList.toggle('is-visible', visible);
        button.setAttribute('aria-label', visible ? 'Ocultar senha' : 'Mostrar senha');
        button.setAttribute('aria-pressed', String(visible));
      });
    });
  }
  function initRegisterPasswordValidation() {
    document.querySelectorAll('[data-register-form]').forEach((form) => form.addEventListener('submit', (ev) => {
      const password = form.querySelector('#Password'); const confirm = form.querySelector('#ConfirmPassword'); const error = form.querySelector('[data-password-match-error]');
      if (!password || !confirm || !error) return;
      const mismatch = password.value !== confirm.value;
      error.classList.toggle('d-none', !mismatch); confirm.classList.toggle('hf-input-invalid', mismatch);
      if (mismatch) { ev.preventDefault(); confirm.focus(); }
    }));
  }
  ready(() => {
    window.applyUiPreferences(readLocalPreferences());
    initPasswordToggles();
    initRegisterPasswordValidation();
    document.querySelectorAll('[data-hf-toast]').forEach((el) => showToast(el.getAttribute('data-hf-toast') || 'Atualizado'));
    document.querySelectorAll('.alert').forEach((alert) => setTimeout(() => window.bootstrap?.Alert.getOrCreateInstance(alert).close(), 6500));
    document.querySelectorAll('form').forEach((form) => form.addEventListener('submit', () => { const btn = form.querySelector('button[type="submit"],button:not([type])'); if (btn && !btn.dataset.noLoading) { btn.dataset.originalText = btn.textContent; btn.textContent = btn.dataset.loadingText || 'Processando...'; btn.disabled = true; } }));
    document.querySelectorAll('[data-confirm]').forEach((el) => el.addEventListener('click', (ev) => { ev.preventDefault(); confirmAction(el.getAttribute('data-confirm') || 'Confirmar ação?', () => el.closest('form') ? el.closest('form').submit() : window.location.assign(el.href)); }));
    document.querySelectorAll('[data-copy]').forEach((btn) => btn.addEventListener('click', async () => { await navigator.clipboard.writeText(btn.getAttribute('data-copy') || ''); showToast('Copiado com segurança.'); }));
    document.querySelectorAll('.hf-day-toggle input').forEach((input) => input.addEventListener('change', () => input.closest('.hf-day-toggle').classList.toggle('is-selected', input.checked)));
    const form = document.querySelector('[data-ui-preferences-form]');
    if (form) {
      const contrast = form.querySelector('[data-ui-contrast]'); const font = form.querySelector('[data-ui-font]'); const motion = form.querySelector('[data-ui-motion]');
      const update = () => { const preferences = { contrastMode: contrast?.value || 'Default', fontScale: font?.value || 'Normal', reduceMotion: Boolean(motion?.checked) }; window.applyUiPreferences(preferences); saveLocalPreferences(preferences); };
      contrast?.addEventListener('change', update); font?.addEventListener('change', update); motion?.addEventListener('change', update); update();
    }
  });
  function showToast(message) { const host = document.getElementById('hfToastHost'); if (!host || !window.bootstrap) return; const node = document.createElement('div'); node.className = 'toast hf-toast'; node.setAttribute('role','status'); const body = document.createElement('div'); body.className = 'toast-body'; body.textContent = message; node.appendChild(body); host.appendChild(node); window.bootstrap.Toast.getOrCreateInstance(node, { delay: 4500 }).show(); }
  function confirmAction(message, onConfirm) { const modalEl = document.getElementById('hfConfirmModal'); if (!modalEl || !window.bootstrap) { if (confirm(message)) onConfirm(); return; } document.getElementById('hfConfirmMessage').textContent = message; const action = document.getElementById('hfConfirmAction'); const clone = action.cloneNode(true); action.replaceWith(clone); clone.addEventListener('click', () => { window.bootstrap.Modal.getInstance(modalEl).hide(); onConfirm(); }); window.bootstrap.Modal.getOrCreateInstance(modalEl).show(); }
})();
