const CACHE_NAME = "habitflow-v2-4-prd";
const STATIC_ASSETS = ["/", "/index.html", "/manifest.json", "/assets/icons/icon.svg", "/assets/css/style.css"];
const BYPASS_HOSTS = ["cloudfunctions.net", "googleapis.com", "firestore.googleapis.com", "identitytoolkit.googleapis.com", "securetoken.googleapis.com", "firebaseio.com", "firebaseapp.com", "gstatic.com"];

self.addEventListener("install", (event) => {
  event.waitUntil(caches.open(CACHE_NAME).then((cache) => cache.addAll(STATIC_ASSETS)));
  self.skipWaiting();
});

self.addEventListener("activate", (event) => {
  event.waitUntil(caches.keys().then((keys) => Promise.all(keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key)))).then(() => self.clients.claim()));
});

self.addEventListener("fetch", (event) => {
  const url = new URL(event.request.url);
  const isRemote = BYPASS_HOSTS.some((host) => url.hostname.includes(host));
  const isPrivateOrGenerated = url.pathname.endsWith(".map") || url.pathname.includes("/__/auth") || url.pathname.includes("/identitytoolkit/");
  if (event.request.method !== "GET" || isRemote || isPrivateOrGenerated || url.origin !== self.location.origin) return;
  event.respondWith(caches.match(event.request).then((cached) => cached || fetch(event.request).then((response) => {
    if (response && response.ok && ["basic", "default"].includes(response.type) && !url.pathname.endsWith("index.html")) {
      const copy = response.clone();
      caches.open(CACHE_NAME).then((cache) => cache.put(event.request, copy)).catch(() => {});
    }
    return response;
  })));
});
