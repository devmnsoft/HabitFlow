(() => {
  document.querySelector('[data-legal-print]')?.addEventListener('click', () => window.print());
  const links=[...document.querySelectorAll('.legal-summary a')];
  const observer=new IntersectionObserver(entries=>entries.forEach(entry=>{if(entry.isIntersecting){links.forEach(link=>link.classList.toggle('active',link.hash===`#${entry.target.id}`));}}),{rootMargin:'-20% 0px -70%'});
  document.querySelectorAll('.legal-article section[id]').forEach(section=>observer.observe(section));
  const dialog=document.createElement('dialog'); dialog.className='hf-info-dialog'; dialog.setAttribute('aria-labelledby','legal-dialog-title');
  const title=document.createElement('h2');title.id='legal-dialog-title';const description=document.createElement('p');description.id='legal-dialog-description';dialog.setAttribute('aria-describedby',description.id);const close=document.createElement('button');close.type='button';close.className='btn btn-success';close.textContent='Entendi';close.addEventListener('click',()=>dialog.close());dialog.append(title,description,close);document.body.append(dialog);
  document.querySelectorAll('[data-info-title]').forEach(button=>button.addEventListener('click',()=>{title.textContent=button.dataset.infoTitle||'';description.textContent=button.dataset.infoText||'';dialog.showModal();close.focus();})); dialog.addEventListener('click',event=>{if(event.target===dialog)dialog.close();});
})();
