const VERSION='v6.16.8';
const CACHE=`habitflow-public-${VERSION}`;
const STATIC=['/offline','/offline.html','/css/site.css','/js/pwa.js','/js/offline-sync.js','/favicon.svg','/icons/icon-192.svg','/icons/icon-512.svg','/icons/icon-maskable.svg'];
const SENSITIVE=/\/(auth|login|register|account|billing|plans\/checkout|payments?|reports\/export|admin|superadmin|webhooks?|lgpd\/export)(\/|$)/i;
self.addEventListener('install',e=>e.waitUntil(caches.open(CACHE).then(c=>c.addAll(STATIC)).then(()=>self.skipWaiting())));
self.addEventListener('activate',e=>e.waitUntil(caches.keys().then(keys=>Promise.all(keys.filter(k=>k.startsWith('habitflow-')&&k!==CACHE).map(k=>caches.delete(k)))).then(()=>self.clients.claim())));
self.addEventListener('message',e=>{if(e.data?.type==='SKIP_WAITING')self.skipWaiting();if(e.data?.type==='SYNC_NOW')e.waitUntil(notifyClients('SYNC_REQUESTED'));});
self.addEventListener('fetch',e=>{const r=e.request,u=new URL(r.url);if(r.method!=='GET'||u.origin!==location.origin||SENSITIVE.test(u.pathname)||r.headers.get('accept')?.includes('application/json'))return;
 if(r.mode==='navigate'){e.respondWith(fetch(r,{cache:'no-store'}).catch(()=>caches.match('/offline')));return;}
 if(STATIC.includes(u.pathname)){e.respondWith(caches.match(r).then(hit=>hit||fetch(r).then(res=>{if(res.ok)caches.open(CACHE).then(c=>c.put(r,res.clone()));return res;})));}
});
self.addEventListener('push',e=>{let p={title:'Hora do seu hábito',body:'Você tem um hábito planejado para agora.',url:'/my-day'};try{p={...p,...e.data.json()};}catch(_invalidPayload){p={...p};}e.waitUntil(self.registration.showNotification(p.title,{body:p.body,icon:'/icons/icon-192.svg',badge:'/icons/icon-192.svg',tag:'habit-reminder',data:{url:safeUrl(p.url)}}));});
self.addEventListener('notificationclick',e=>{e.notification.close();e.waitUntil(clients.openWindow(e.notification.data?.url||'/my-day'));});
self.addEventListener('sync',e=>{if(e.tag==='habitflow-sync')e.waitUntil(notifyClients('SYNC_REQUESTED'));});
const safeUrl=url=>typeof url==='string'&&url.startsWith('/')&&!url.startsWith('//')?url:'/my-day';
const notifyClients=type=>clients.matchAll({includeUncontrolled:true,type:'window'}).then(list=>list.forEach(c=>c.postMessage({type})));
