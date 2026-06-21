const CACHE_VERSION = "habitflow-v2-security-static";
const STATIC_ASSETS = ["/", "/index.html", "/manifest.json", "/assets/icons/icon.svg"];
const BLOCKED_HOSTS = ["googleapis.com", "firebaseio.com", "cloudfunctions.net", "identitytoolkit.googleapis.com", "securetoken.googleapis.com", "firestore.googleapis.com"];

self.addEventListener("install", (event) => {
  event.waitUntil(caches.open(CACHE_VERSION).then((cache) => cache.addAll(STATIC_ASSETS)));
  self.skipWaiting();
});

self.addEventListener("activate", (event) => {
  event.waitUntil(caches.keys().then((keys) => Promise.all(keys.filter((key) => key !== CACHE_VERSION).map((key) => caches.delete(key)))).then(() => self.clients.claim()));
});

self.addEventListener("fetch", (event) => {
  const url = new URL(event.request.url);
  const isSensitiveRemote = BLOCKED_HOSTS.some((host) => url.hostname.includes(host));
  if (event.request.method !== "GET" || isSensitiveRemote || url.pathname.endsWith(".map")) return;
  event.respondWith(caches.match(event.request).then((cached) => cached || fetch(event.request)));
});
