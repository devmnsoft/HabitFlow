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
    goal: form?.querySelector('[data-preview-goal]'), duration: form?.querySelector('[data-preview-duration]'),
    tracking: form?.querySelector('[data-preview-tracking]'), minimum: form?.querySelector('[data-preview-minimum]'),
    period: form?.querySelector('[data-preview-period]')
  };
  const quantityFields = form?.querySelector('[data-quantity-fields]');
  const trackingModes = [...(form?.querySelectorAll('[data-tracking-mode]') ?? [])];
  const targetQuantity = form?.querySelector('#TargetQuantity');
  const targetUnit = form?.querySelector('#TargetUnit');
  const minimumName = form?.querySelector('#MinimumVersionName');
  const minimumQuantity = form?.querySelector('#MinimumVersionQuantity');
  const startDate = form?.querySelector('#StartDate');
  const endDate = form?.querySelector('#EndDate');
  const unitLabel = value => ({ min: 'min', minutes: 'min', pages: 'páginas', steps: 'passos', ml: 'ml', liters: 'litros', km: 'km', times: 'vezes', sessions: 'sessões' })[value] ?? value;
  const selectedTrackingMode = () => trackingModes.find(input => input.checked)?.value ?? 'binary';
  const setQuantityEnabled = () => {
    const enabled = selectedTrackingMode() === 'quantity';
    if (!quantityFields) return;
    quantityFields.classList.toggle('is-disabled', !enabled);
    quantityFields.querySelectorAll('input, select').forEach(input => {
      input.disabled = !enabled;
      if (!enabled) input.value = '';
    });
  };
  const updateEditorPreview = () => {
    if (!form) return;
    const name = form.querySelector('#Name')?.value.trim();
    const category = form.querySelector('#Category')?.value.trim();
    const goal = form.querySelector('#ObjectiveId')?.selectedOptions[0]?.textContent.trim();
    const duration = form.querySelector('#EstimatedTimeMinutes')?.value;
    const quantity = targetQuantity?.value;
    const unit = targetUnit?.value;
    const minName = minimumName?.value.trim();
    const minQuantity = minimumQuantity?.value;
    const starts = startDate?.value;
    const ends = endDate?.value;
    if (editorPreview.name) editorPreview.name.textContent = name || 'Seu novo hábito';
    if (editorPreview.category) editorPreview.category.textContent = category || 'Sem categoria';
    if (editorPreview.goal) editorPreview.goal.textContent = goal || 'Sem objetivo vinculado';
    if (editorPreview.duration) editorPreview.duration.textContent = duration ? `${duration} min` : 'Não informada';
    if (editorPreview.tracking) editorPreview.tracking.textContent = selectedTrackingMode() === 'quantity' && quantity && unit ? `${quantity} ${unitLabel(unit)}` : 'Concluir sim/não';
    if (editorPreview.minimum) editorPreview.minimum.textContent = minName || (minQuantity && unit ? `${minQuantity} ${unitLabel(unit)}` : 'Sem versão mínima');
    if (editorPreview.period) {
      const startText = starts ? `começa em ${starts.split('-').reverse().join('/')}` : 'começa hoje';
      const endText = ends ? ` e termina em ${ends.split('-').reverse().join('/')}` : '';
      editorPreview.period.textContent = `${startText}${endText}`;
    }
  };
  trackingModes.forEach(input => input.addEventListener('change', () => { setQuantityEnabled(); updateEditorPreview(); }));
  ['Name', 'Category', 'ObjectiveId', 'EstimatedTimeMinutes', 'TargetQuantity', 'TargetUnit', 'MinimumVersionName', 'MinimumVersionQuantity', 'StartDate', 'EndDate'].forEach(id => form?.querySelector(`#${id}`)?.addEventListener('input', updateEditorPreview));
  ['ObjectiveId', 'TargetUnit'].forEach(id => form?.querySelector(`#${id}`)?.addEventListener('change', updateEditorPreview));
  setQuantityEnabled();
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
    const hasQuantity = Boolean(button.dataset.quantity && button.dataset.unit);
    trackingModes.forEach(input => { input.checked = input.value === (hasQuantity ? 'quantity' : 'binary'); });
    if (targetQuantity) targetQuantity.value = button.dataset.quantity ?? '';
    if (targetUnit) targetUnit.value = button.dataset.unit ?? '';
    if (minimumName) minimumName.value = button.dataset.minimumName ?? '';
    if (minimumQuantity) minimumQuantity.value = button.dataset.minimumQuantity ?? '';
    setQuantityEnabled();
    updatePreview();
    updateEditorPreview();
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
    const quantityMode = selectedTrackingMode() === 'quantity';
    const missingQuantityPair = quantityMode && Boolean(targetQuantity?.value) !== Boolean(targetUnit?.value);
    const invalidDateRange = startDate?.value && endDate?.value && endDate.value < startDate.value;
    if (customWithoutDays) {
      event.preventDefault();
      const error = form.querySelector('#SelectedDaysError');
      if (error) error.textContent = 'Selecione pelo menos um dia da semana.';
      weekdays?.querySelector('input')?.focus();
      return;
    }
    if (missingQuantityPair) {
      event.preventDefault();
      const error = form.querySelector('#TargetUnitError');
      if (error) error.textContent = 'Informe quantidade e unidade juntas.';
      (targetQuantity?.value ? targetUnit : targetQuantity)?.focus();
      return;
    }
    if (invalidDateRange) {
      event.preventDefault();
      const error = form.querySelector('#EndDateError');
      if (error) error.textContent = 'A data final deve ser igual ou posterior ao início.';
      endDate?.focus();
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
