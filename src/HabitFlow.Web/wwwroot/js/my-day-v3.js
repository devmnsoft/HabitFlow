(() => {
  'use strict';
  const root = document.querySelector('[data-my-day]');
  if (!root) return;
  const live = root.querySelector('[data-routine-status]');
  const announce = message => { if (live) live.textContent = message; };

  // Guarda anti-duplo-submit: NÃO implementar aqui. O form-validation.js decora
  // TODOS os formulários não-GET com data-hf-validate e já é o guarda único
  // (dois guardas no mesmo submit se cancelam: o segundo vê a flag do primeiro
  // e chama preventDefault, travando o botão sem enviar nada — foi o que travou
  // todos os botões de ação do Meu Dia). Aqui fica só o anúncio de progresso.
  root.querySelectorAll('[data-routine-form], [data-routine-confirm-form]').forEach(form => {
    form.addEventListener('submit', () => {
      if (!form.checkValidity()) return;
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
