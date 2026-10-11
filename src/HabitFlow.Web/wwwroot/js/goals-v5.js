(() => {
  'use strict';
  const dialog = document.querySelector('[data-goal-dialog]');
  const opener = document.querySelector('[data-goal-dialog-open]');
  if (!dialog || !opener) return;
  let returnFocus = opener;
  const close = () => { dialog.close(); returnFocus.focus(); };
  opener.addEventListener('click', () => { returnFocus = document.activeElement; dialog.showModal(); });
  dialog.querySelector('[data-goal-dialog-close]')?.addEventListener('click', close);
  dialog.addEventListener('click', event => { if (event.target === dialog) close(); });
  dialog.addEventListener('cancel', event => { event.preventDefault(); close(); });
})();

(() => {
  'use strict';
  const form = document.querySelector('.hf-goal-editor');
  if (!form) return;
  const select = form.querySelector('[name="TargetType"]');
  const target = form.querySelector('[data-goal-rule]');
  const valueLabel = form.querySelector('label[for="TargetValue"]');
  const rules = {
    HabitCompletions: { unit: 'conclusões', mode: 'Automática', text: 'Soma cada conclusão real dos hábitos conectados durante o período da meta.' },
    ActiveDays: { unit: 'dias ativos', mode: 'Automática', text: 'Conta dias distintos com pelo menos uma conclusão dos hábitos conectados.' },
    StreakDays: { unit: 'dias em sequência', mode: 'Automática', text: 'Usa a sequência canônica calculada pelo progresso diário.' },
    WeeklyCompletions: { unit: 'conclusões nesta semana', mode: 'Automática', text: 'Conta conclusões da semana ISO atual, de segunda a domingo.' },
    Custom: { unit: 'unidades manuais', mode: 'Manual', text: 'Você atualiza o valor manualmente; hábitos conectados servem como contexto.' }
  };
  const render = () => {
    const rule = rules[select?.value] || rules.HabitCompletions;
    if (target) {
      target.replaceChildren();
      const strong = document.createElement('strong');
      strong.textContent = `${rule.mode}:`;
      target.append(strong, ` ${rule.text}`);
    }
    if (valueLabel) {
      valueLabel.replaceChildren();
      valueLabel.append(`Meta em ${rule.unit} `);
      const hidden = document.createElement('span');
      hidden.className = 'visually-hidden';
      hidden.textContent = '(obrigatório)';
      valueLabel.append(hidden);
    }
  };
  select?.addEventListener('change', render);
  form.addEventListener('submit', () => form.querySelector('button[type="submit"]')?.setAttribute('disabled', 'disabled'));
})();
