(() => {
  const DB='habitflow-offline', STORE='actions', TTL=24*60*60*1000;
  const open=()=>new Promise((ok,no)=>{const r=indexedDB.open(DB,1);r.onupgradeneeded=()=>r.result.createObjectStore(STORE,{keyPath:'id'});r.onsuccess=()=>ok(r.result);r.onerror=()=>no(r.error);});
  const transaction=async(mode,fn)=>{const db=await open(),tx=db.transaction(STORE,mode),s=tx.objectStore(STORE);return new Promise((ok,no)=>{fn(s,ok);tx.onerror=()=>no(tx.error);tx.oncomplete=()=>db.close();});};
  const list=()=>transaction('readonly',(s,ok)=>{const r=s.getAll();r.onsuccess=()=>ok(r.result);});
  const remove=id=>transaction('readwrite',(s,ok)=>{s.delete(id);ok();});
  window.HabitFlowOffline={enqueue: action=>transaction('readwrite',(s,ok)=>{const id=action.id||crypto.randomUUID();s.put({...action,id,createdAt:Date.now(),expiresAt:Date.now()+TTL});ok(id);}),sync};
  async function sync(){for(const a of await list()){if(a.expiresAt<Date.now()){await remove(a.id);continue;}try{const response=await fetch(a.url,{method:'POST',headers:{'Content-Type':'application/json','RequestVerificationToken':a.antiforgery,'X-Idempotency-Key':a.id},body:JSON.stringify(a.body||{})});if(response.ok||response.status===409)await remove(a.id);else if(response.status===401){document.dispatchEvent(new CustomEvent('habitflow:sync-auth'));break;}else if(response.status>=400&&response.status<500){await remove(a.id);document.dispatchEvent(new CustomEvent('habitflow:sync-conflict'));}}catch{return;}}localStorage.setItem('habitflow:last-sync',new Date().toISOString());}
  addEventListener('online',sync);navigator.serviceWorker?.addEventListener('message',e=>{if(e.data?.type==='SYNC_REQUESTED')sync();});if(navigator.onLine)sync();
})();
