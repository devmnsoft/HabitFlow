(() => {
  'use strict';
  const form = document.querySelector('[data-habit-editor]');
  const frequency = form?.querySelector('[data-frequency]');
  const weekdays = form?.querySelector('[data-weekdays]');
  const preview = form?.querySelector('[data-schedule-preview]');
  const target = form?.querySelector('#TargetPerWeek');
  const defaults = { Daily: 7, Weekdays: 5, Weekends: 2 };
  const descriptions = {
    Daily: 'Este hábito aparecerá todos os dias.',
    Weekdays: 'Este hábito aparecerá de segunda a sexta.',
    Weekends: 'Este hábito aparecerá aos sábados e domingos.'
  };

  const checkedDays = () => [...(weekdays?.querySelectorAll('input:checked') ?? [])];
  const updatePreview = () => {
    if (!frequency || !weekdays || !preview) return;
    const custom = frequency.value === 'CustomWeekly';
    weekdays.hidden = !custom;
    weekdays.querySelectorAll('input').forEach(input => { input.disabled = !custom; });
    if (!custom) {
      preview.textContent = descriptions[frequency.value] ?? 'Escolha uma frequência válida.';
      target.max = String(defaults[frequency.value] ?? 7);
      return;
    }
    const names = checkedDays().map(input => input.dataset.dayName);
    preview.textContent = names.length ? `Este hábito aparecerá em: ${names.join(', ')}.` : 'Selecione pelo menos um dia da semana.';
    target.max = String(Math.max(1, names.length));
  };
  const suggestTarget = () => {
    if (!frequency || !target) return;
    target.value = String(frequency.value === 'CustomWeekly' ? checkedDays().length || 1 : defaults[frequency.value] ?? 1);
  };
  frequency?.addEventListener('change', () => { suggestTarget(); updatePreview(); });
  weekdays?.addEventListener('change', () => { suggestTarget(); updatePreview(); });
  updatePreview();

  const editorPreview = {
    name: form?.querySelector('[data-preview-name]'), category: form?.querySelector('[data-preview-category]'),
    goal: form?.querySelector('[data-preview-goal]'), duration: form?.querySelector('[data-preview-duration]')
  };
  const updateEditorPreview = () => {
    if (!form) return;
    const name = form.querySelector('#Name')?.value.trim();
    const category = form.querySelector('#Category')?.value.trim();
    const goal = form.querySelector('#ObjectiveId')?.selectedOptions[0]?.textContent.trim();
    const duration = form.querySelector('#EstimatedTimeMinutes')?.value;
    if (editorPreview.name) editorPreview.name.textContent = name || 'Seu novo hábito';
    if (editorPreview.category) editorPreview.category.textContent = category || 'Sem categoria';
    if (editorPreview.goal) editorPreview.goal.textContent = goal || 'Sem objetivo vinculado';
    if (editorPreview.duration) editorPreview.duration.textContent = duration ? `${duration} min` : 'Não informada';
  };
  ['Name', 'Category', 'ObjectiveId', 'EstimatedTimeMinutes'].forEach(id => form?.querySelector(`#${id}`)?.addEventListener('input', updateEditorPreview));
  updateEditorPreview();

  document.querySelectorAll('[data-habit-template]').forEach(button => button.addEventListener('click', () => {
    if (!form || !frequency || !target) return;
    form.querySelector('#Name').value = button.dataset.name ?? '';
    frequency.value = button.dataset.frequency ?? 'Daily';
    target.value = button.dataset.target ?? '';
    const icon = form.querySelector('#IconCode');
    if (icon && [...icon.options].some(option => option.value === button.dataset.icon)) icon.value = button.dataset.icon;
    if (frequency.value === 'CustomWeekly') {
      const preferred = new Set(['1', '3', '5']);
      weekdays?.querySelectorAll('input').forEach(input => { input.checked = preferred.has(input.value); });
    }
    updatePreview();
    form.querySelector('#Name')?.focus();
  }));

  let returnFocus = null;
  document.querySelectorAll('[data-modal-open]').forEach(button => button.addEventListener('click', () => {
    const modal = document.getElementById(button.dataset.modalOpen);
    if (!(modal instanceof HTMLDialogElement)) return;
    returnFocus = button; modal.showModal();
  }));
  document.querySelectorAll('dialog').forEach(dialog => dialog.addEventListener('close', () => returnFocus?.focus()));

  form?.addEventListener('submit', event => {
    const customWithoutDays = frequency?.value === 'CustomWeekly' && checkedDays().length === 0;
    if (customWithoutDays) {
      event.preventDefault();
      const error = form.querySelector('#SelectedDaysError');
      if (error) error.textContent = 'Selecione pelo menos um dia da semana.';
      weekdays?.querySelector('input')?.focus();
      return;
    }
    if (!form.checkValidity()) {
      event.preventDefault();
      form.reportValidity();
      form.querySelector(':invalid')?.focus();
      return;
    }
    const button = form.querySelector('[data-submit]');
    if (button) { button.disabled = true; button.textContent = 'Salvando…'; }
  });
})();
