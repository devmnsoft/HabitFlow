const CACHE_VERSION = "habitflow-v1.5-static";
const STATIC_ASSETS = [
  "/",
  "/index.html",
  "/assets/css/style.css",
  "/assets/js/app.js",
  "/assets/js/firebase.js",
  "/assets/js/plans.js",
  "/assets/js/utils.js",
  "/assets/icons/icon.svg",
  "/manifest.json"
];

self.addEventListener("install", (event) => {
  event.waitUntil(caches.open(CACHE_VERSION).then((cache) => cache.addAll(STATIC_ASSETS)));
  self.skipWaiting();
});

self.addEventListener("activate", (event) => {
  event.waitUntil(caches.keys().then((keys) => Promise.all(keys.filter((key) => key !== CACHE_VERSION).map((key) => caches.delete(key)))).then(() => self.clients.claim()));
});

self.addEventListener("fetch", (event) => {
  const url = new URL(event.request.url);
  if (event.request.method !== "GET" || url.hostname.includes("googleapis.com") || url.hostname.includes("firebase")) return;
  event.respondWith(caches.match(event.request).then((cached) => cached || fetch(event.request)));
});
