(() => {
  document.querySelectorAll('.js-cycle').forEach(button => button.addEventListener('click', () => {
    const yearly=button.dataset.cycle==='Yearly'; document.querySelectorAll('.js-cycle').forEach(item=>{item.classList.toggle('active',item===button);item.classList.toggle('btn-success',item===button);item.classList.toggle('btn-outline-success',item!==button);item.setAttribute('aria-pressed',String(item===button));});
    document.querySelectorAll('[data-plan-monthly]').forEach(price=>price.textContent=yearly&&price.dataset.planYearly?price.dataset.planYearly:price.dataset.planMonthly);
    document.querySelectorAll('.js-cycle-input').forEach(input=>input.value=yearly?'Yearly':'Monthly');
    document.querySelectorAll('[data-yearly-saving]').forEach(label=>{label.hidden=!yearly;});
    document.querySelectorAll('[data-register-cycle]').forEach(link=>{link.href=`/register?intent=${encodeURIComponent(link.dataset.registerCycle)}&cycle=${yearly?'Yearly':'Monthly'}`;});
  }));
  const dialog = document.createElement('dialog'); dialog.className='hf-info-dialog'; dialog.setAttribute('aria-labelledby','info-dialog-title');
  const title=document.createElement('h2'); title.id='info-dialog-title'; const text=document.createElement('p'); text.id='info-dialog-description'; dialog.setAttribute('aria-describedby',text.id);
  const close=document.createElement('button'); close.type='button'; close.className='btn btn-success'; close.textContent='Entendi'; close.addEventListener('click',()=>dialog.close());
  dialog.append(title,text,close); document.body.append(dialog);
  document.querySelectorAll('[data-info-title]').forEach(button=>button.addEventListener('click',()=>{title.textContent=button.dataset.infoTitle||'';text.textContent=button.dataset.infoText||'';dialog.showModal();close.focus();}));
  dialog.addEventListener('click',event=>{if(event.target===dialog)dialog.close();});
})();
