(function () {
  'use strict';
  const tour = document.querySelector('[data-guided-tour]');
  if (!tour || localStorage.getItem('habitflow.tour.dashboard.done') === 'true') return;
  const steps = [
    ['[data-tour="today"]', 'Aqui aparece o que você precisa fazer hoje.'],
    ['[data-tour="habit-list"]', 'Clique aqui para marcar um hábito como concluído.'],
    ['[data-tour="streak"]', 'Sua sequência mostra há quantos dias você mantém consistência.'],
    ['[data-tour="library"]', 'Use a biblioteca para adicionar hábitos prontos.'],
    ['[data-tour="reports"]', 'Veja relatórios para acompanhar sua evolução.']
  ];
  let index = 0;
  const text = tour.querySelector('.hf-tour-step');
  const next = tour.querySelector('.hf-tour-next');
  const prev = tour.querySelector('.hf-tour-prev');
  const skip = tour.querySelector('.hf-tour-skip');
  function clear(){ document.querySelectorAll('.hf-tour-highlight').forEach(e => e.classList.remove('hf-tour-highlight')); }
  function done(){ clear(); localStorage.setItem('habitflow.tour.dashboard.done','true'); tour.hidden = true; }
  function show(){ clear(); const step = steps[index]; const target = document.querySelector(step[0]); text.textContent = step[1]; if (target) { target.classList.add('hf-tour-highlight'); target.scrollIntoView({block:'center', behavior:'smooth'}); } prev.disabled = index === 0; next.textContent = index === steps.length - 1 ? 'Entendi' : 'Próximo'; tour.hidden = false; }
  next.addEventListener('click', function(){ if (index >= steps.length - 1) done(); else { index += 1; show(); } });
  prev.addEventListener('click', function(){ if (index > 0) { index -= 1; show(); } });
  skip.addEventListener('click', done);
  show();
}());
