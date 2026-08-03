(() => {
  const update = (form) => {
    const items = [...form.querySelectorAll('[data-collection-item]')];
    const included = items.filter((item) => { const toggle=item.querySelector('[data-item-toggle]'); return !toggle || toggle.checked; });
    const count=form.querySelector('[data-selected-count]'); const time=form.querySelector('[data-time-estimate]'); const remaining=form.querySelector('[data-plan-remaining]');
    if(count) count.textContent=String(included.length);
    if(time) time.textContent=String(included.reduce((sum,item)=>sum+Number(item.dataset.minutes||0),0));
    if(remaining && Number.isFinite(Number(form.dataset.planRemaining))) remaining.textContent=String(Math.max(0,Number(form.dataset.planRemaining)-included.length));
    items.forEach((item)=>{const toggle=item.querySelector('[data-item-toggle]');const options=item.querySelector('[data-item-options]');if(toggle&&options){options.hidden=!toggle.checked;toggle.setAttribute('aria-expanded',String(toggle.checked));options.querySelectorAll('input,select').forEach((control)=>control.disabled=!toggle.checked);}});
  };
  document.querySelectorAll('[data-collection-form]').forEach((form)=>{form.addEventListener('change',()=>update(form));form.addEventListener('submit',(event)=>{const button=form.querySelector('[data-submit]');if(button?.disabled){event.preventDefault();return;}if(button){button.disabled=true;button.textContent='Ativando…';}const status=form.querySelector('[data-form-status]');if(status)status.textContent='Salvando sua rotina com segurança.';});update(form);});
  const difficulty=document.querySelector('[data-filter-difficulty]');const duration=document.querySelector('[data-filter-duration]');
  const filter=()=>{let visible=0;document.querySelectorAll('[data-collection-grid] > [data-difficulty]').forEach((card)=>{const show=(!difficulty?.value||card.dataset.difficulty===difficulty.value)&&(!duration?.value||Number(card.dataset.duration)<=Number(duration.value));card.hidden=!show;if(show)visible++;});document.querySelector('[data-filter-empty]')?.classList.toggle('d-none',visible>0);};difficulty?.addEventListener('change',filter);duration?.addEventListener('change',filter);
})();
