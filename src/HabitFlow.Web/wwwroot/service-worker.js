const CACHE='habitflow-public-v63';
const PUBLIC=['/offline.html','/','/help','/css/site.css','/js/site.js','/js/pwa.js','/favicon.svg'];
const PRIVATE=/\/(dashboard|habits|goals|progress|reports|account|profile|notifications|billing|superadmin|checkout|webhooks?|fiscal)/i;
self.addEventListener('install',event=>event.waitUntil(caches.open(CACHE).then(cache=>cache.addAll(PUBLIC)).then(()=>self.skipWaiting())));
self.addEventListener('activate',event=>event.waitUntil(caches.keys().then(keys=>Promise.all(keys.filter(k=>k!==CACHE).map(k=>caches.delete(k)))).then(()=>self.clients.claim())));
self.addEventListener('fetch',event=>{const request=event.request;if(request.method!=='GET'||PRIVATE.test(new URL(request.url).pathname)||request.headers.get('accept')?.includes('application/json'))return;event.respondWith(fetch(request).then(response=>{if(response.ok&&response.type==='basic'){const copy=response.clone();caches.open(CACHE).then(c=>c.put(request,copy));}return response;}).catch(()=>caches.match(request).then(cached=>cached||(request.mode==='navigate'?caches.match('/offline.html'):Response.error()))));});
