(() => { if ('serviceWorker' in navigator) window.addEventListener('load', () => navigator.serviceWorker.register('/service-worker.js'));
let prompt; const button=document.querySelector('[data-pwa-install]'); if(!button)return;
window.addEventListener('beforeinstallprompt',e=>{e.preventDefault();prompt=e;button.hidden=false;});
button.addEventListener('click',async()=>{if(!prompt)return;prompt.prompt();const choice=await prompt.userChoice;prompt=null;button.hidden=true;if(choice.outcome==='accepted')fetch('/product-events/pwa-installed',{method:'POST',headers:{'RequestVerificationToken':document.querySelector('input[name="__RequestVerificationToken"]')?.value||''}}).catch(()=>{});});
window.addEventListener('appinstalled',()=>{button.hidden=true;prompt=null;}); })();
