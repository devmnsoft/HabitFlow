(() => {
  'use strict';
  const forms = document.querySelectorAll('form[data-hf-validate]');
  const connectErrors = form => {
    form.querySelectorAll('.field-validation-error, .hf-validation-message').forEach(error => {
      const fieldName = error.dataset.valmsgFor;
      const field = fieldName ? form.elements.namedItem(fieldName) : error.previousElementSibling;
      if (!(field instanceof HTMLElement)) return;
      if (!error.id) error.id = `hf-error-${crypto.randomUUID()}`;
      field.setAttribute('aria-invalid', 'true');
      field.setAttribute('aria-describedby', [field.getAttribute('aria-describedby'), error.id].filter(Boolean).join(' '));
    });
  };
  forms.forEach(form => {
    connectErrors(form);
    form.querySelectorAll('[maxlength]').forEach(field => {
      const counter = form.querySelector(`[data-character-counter="${CSS.escape(field.name)}"]`);
      if (!counter) return;
      const update = () => { counter.textContent = `${field.value.length} de ${field.maxLength}`; };
      field.addEventListener('input', update); update();
    });
    form.addEventListener('submit', event => {
      connectErrors(form);
      if (!form.checkValidity()) {
        event.preventDefault(); form.querySelector(':invalid')?.focus(); return;
      }
      if (form.dataset.submitting === 'true') { event.preventDefault(); return; }
      form.dataset.submitting = 'true';
      form.querySelectorAll('button[type="submit"]').forEach(button => { button.disabled = true; button.dataset.hfBusy = 'true'; });
    });
  });
})();
