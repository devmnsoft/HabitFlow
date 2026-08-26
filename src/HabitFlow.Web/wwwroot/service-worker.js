const VERSION = 'v6.17.7';
const CACHE = `habitflow-public-${VERSION}`;
const STATIC = ['/offline.html','/offline-private.html','/css/site.css','/css/design-system.css','/js/pwa.js','/favicon.svg','/icons/icon-192.svg','/icons/icon-512.svg','/icons/icon-maskable.svg'];
const PRIVATE_ROUTE = /^\/(dashboard|my-day|habits|reminders|notifications|profile|settings|account|billing|reports|admin|superadmin)(\/|$)/i;
const NEVER_INTERCEPT = /^\/(auth|login|logout|register|password|payments?|webhooks?|api)(\/|$)/i;
const debug = (...args) => { if (self.location.hostname === 'localhost') console.info('[HabitFlow SW]', ...args); };
self.addEventListener('install', event => event.waitUntil(caches.open(CACHE).then(cache => cache.addAll(STATIC)).then(() => debug('assets públicos prontos'))));
self.addEventListener('activate', event => event.waitUntil(caches.keys().then(keys => Promise.all(keys.filter(key => key.startsWith('habitflow-') && key !== CACHE).map(key => caches.delete(key)))).then(() => self.clients.claim())));
self.addEventListener('message', event => { if (event.data?.type === 'SKIP_WAITING') self.skipWaiting(); });
self.addEventListener('fetch', event => {
  const request = event.request; const url = new URL(request.url);
  if (request.method !== 'GET' || url.origin !== self.location.origin || NEVER_INTERCEPT.test(url.pathname) || request.headers.get('authorization') || request.headers.get('accept')?.includes('application/json')) return;
  if (request.mode === 'navigate') {
    event.respondWith(fetch(request, { cache: 'no-store', credentials: 'include' }).catch(() => caches.match(PRIVATE_ROUTE.test(url.pathname) ? '/offline-private.html' : '/offline.html')));
    return;
  }
  if (STATIC.includes(url.pathname)) event.respondWith(caches.match(request).then(cached => cached || fetch(request, { credentials: 'omit' }).then(response => { if (response.ok && response.type === 'basic') caches.open(CACHE).then(cache => cache.put(request, response.clone())); return response; })));
});
self.addEventListener('push', event => {
  let payload = { title:'Hora do seu hábito', body:'Você tem um hábito planejado para agora.', url:'/my-day', tag:'habit-reminder' };
  try { payload = { ...payload, ...event.data.json() }; } catch { /* payload mínimo, sem registrar conteúdo potencialmente inválido */ }
  event.waitUntil(self.registration.showNotification(payload.title, { body:payload.body, icon:'/icons/icon-192.svg', badge:'/icons/icon-192.svg', tag:String(payload.tag).slice(0,100), renotify:false, data:{ url:safeUrl(payload.url) } }));
});
self.addEventListener('notificationclick', event => { event.notification.close(); event.waitUntil(clients.openWindow(event.notification.data?.url || '/my-day')); });
const safeUrl = url => typeof url === 'string' && url.startsWith('/') && !url.startsWith('//') ? url : '/my-day';
