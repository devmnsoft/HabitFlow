(() => {
  'use strict';
  const mutationForms = [...document.forms].filter(form =>
    (form.method || 'get').toLowerCase() !== 'get' && form.method.toLowerCase() !== 'dialog');

  const fieldFor = (form, name, fallback) => form.elements.namedItem(name) || fallback;
  const describeError = (field, error) => {
    if (!(field instanceof HTMLElement)) return;
    if (!error.id) error.id = `hf-error-${crypto.randomUUID()}`;
    field.setAttribute('aria-invalid', 'true');
    field.setAttribute('aria-describedby', [...new Set([
      ...(field.getAttribute('aria-describedby') || '').split(' ').filter(Boolean), error.id
    ])].join(' '));
  };
  const connectServerErrors = form => {
    form.querySelectorAll('[data-valmsg-for], .field-validation-error').forEach(error =>
      describeError(fieldFor(form, error.dataset.valmsgFor, error.previousElementSibling), error));
    const summary = form.querySelector('[data-valmsg-summary="true"]');
    if (summary?.querySelector('li')) summary.focus();
  };
  const showClientErrors = form => {
    let summary = form.querySelector('[data-hf-client-summary]');
    if (!summary) {
      summary = document.createElement('div');
      summary.dataset.hfClientSummary = '';
      summary.className = 'hf-form-error'; summary.role = 'alert'; summary.tabIndex = -1;
      form.prepend(summary);
    }
    const invalid = [...form.querySelectorAll(':invalid')];
    summary.innerHTML = '<strong>Revise os campos indicados.</strong><ul></ul>';
    const list = summary.querySelector('ul');
    invalid.forEach((field, index) => {
      field.setAttribute('aria-invalid', 'true');
      const label = field.labels?.[0]?.textContent?.trim() || field.name || `Campo ${index + 1}`;
      const error = document.createElement('span');
      error.className = 'hf-validation-message'; error.textContent = field.validationMessage;
      error.dataset.hfClientError = '';
      field.insertAdjacentElement('afterend', error); describeError(field, error);
      const item = document.createElement('li'); item.textContent = `${label}: ${field.validationMessage}`; list.append(item);
    });
    summary.focus(); invalid[0]?.focus();
  };

  mutationForms.forEach(form => {
    form.dataset.hfValidate = '';
    form.querySelectorAll('[required]').forEach(field => field.setAttribute('aria-required', 'true'));
    connectServerErrors(form);
    form.querySelectorAll('[maxlength]').forEach(field => {
      const counter = form.querySelector(`[data-character-counter="${CSS.escape(field.name)}"]`);
      if (!counter) return;
      const update = () => { counter.textContent = `${field.value.length} de ${field.maxLength}`; };
      field.addEventListener('input', update); update();
    });
    form.addEventListener('input', event => {
      const field = event.target;
      if (!(field instanceof HTMLElement) || !field.matches('input,select,textarea')) return;
      if (field.checkValidity()) field.removeAttribute('aria-invalid');
      form.querySelectorAll('[data-hf-client-error]').forEach(error => error.remove());
      form.querySelector('[data-hf-client-summary]')?.remove();
    });
    form.addEventListener('submit', event => {
      connectServerErrors(form);
      if (!form.checkValidity()) { event.preventDefault(); showClientErrors(form); return; }
      if (form.dataset.submitting === 'true') { event.preventDefault(); return; }
      form.dataset.submitting = 'true'; form.setAttribute('aria-busy', 'true');
      form.querySelectorAll('button[type="submit"], button:not([type])').forEach(button => {
        button.disabled = true; button.dataset.hfBusy = 'true';
        if (button.dataset.loadingText) { button.dataset.originalText = button.textContent; button.textContent = button.dataset.loadingText; }
      });
    });
  });
})();
