(() => {
  'use strict';
  document.querySelectorAll('form[data-dashboard-habit]').forEach(form => form.addEventListener('submit', async event => {
    event.preventDefault();
    const button = form.querySelector('button');
    if (!button || button.disabled) return;
    const oldText = button.textContent;
    button.disabled = true; button.textContent = 'Salvando…';
    try {
      const token = form.querySelector('input[name="__RequestVerificationToken"]');
      const response = await fetch(form.action, { method: 'POST', headers: { 'RequestVerificationToken': token?.value || '', 'Idempotency-Key': crypto.randomUUID(), 'Accept': 'application/json' } });
      const data = await response.json();
      if (!response.ok || !data.success) throw new Error(data.message || 'Não foi possível atualizar o hábito.');
      const set = (name, value) => { const element = document.querySelector(`[data-kpi="${name}"]`); if (element) element.textContent = value; };
      set('scheduled', data.daily.scheduled); set('completed', data.daily.completed); set('pending', data.daily.pending); set('percentage', `${data.daily.percentage}%`); set('current-streak', data.streak.current); set('best-streak', data.streak.best);
      const card = form.closest('[data-habit-id]'); const state = card?.querySelector('[data-state]');
      if (state) state.textContent = data.completed ? 'Concluído hoje' : 'Pendente hoje';
      button.textContent = data.completed ? 'Desmarcar' : 'Marcar';
      form.action = `/habits/${data.habitId}/${data.completed ? 'undo-completion' : 'complete'}`;
      const next = document.getElementById('next-habit'); if (next) next.textContent = data.nextHabit?.name || 'Tudo concluído';
      const live = document.getElementById('dashboard-status'); if (live) live.textContent = data.message;
    } catch (error) {
      button.textContent = oldText;
      const live = document.getElementById('dashboard-status'); if (live) live.textContent = error.message;
    } finally { button.disabled = false; }
  }));
})();
