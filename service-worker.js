const CACHE_NAME = "habitflow-v2-3-4";
const STATIC_ASSETS = ["/", "/index.html", "/manifest.json", "/assets/icons/icon.svg", "/assets/css/style.css", "/assets/js/app.js", "/assets/js/functions-client.js"];
const BLOCKED_HOSTS = ["googleapis.com", "firebaseio.com", "cloudfunctions.net", "identitytoolkit.googleapis.com", "securetoken.googleapis.com", "firestore.googleapis.com"];

self.addEventListener("install", (event) => {
  event.waitUntil(caches.open(CACHE_NAME).then((cache) => cache.addAll(STATIC_ASSETS)));
  self.skipWaiting();
});

self.addEventListener("activate", (event) => {
  event.waitUntil(caches.keys().then((keys) => Promise.all(keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key)))).then(() => self.clients.claim()));
});

self.addEventListener("fetch", (event) => {
  const url = new URL(event.request.url);
  const isSensitiveRemote = BLOCKED_HOSTS.some((host) => url.hostname.includes(host));
  if (event.request.method !== "GET" || isSensitiveRemote || url.pathname.endsWith(".map")) return;
  if (url.origin !== self.location.origin) return;
  event.respondWith(caches.match(event.request).then((cached) => cached || fetch(event.request).then((response) => {
    if (response && response.ok && ["basic", "default"].includes(response.type)) {
      const copy = response.clone();
      caches.open(CACHE_NAME).then((cache) => cache.put(event.request, copy)).catch(() => {});
    }
    return response;
  })));
});
