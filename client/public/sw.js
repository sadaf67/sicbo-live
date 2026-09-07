// Minimal service worker whose only real job is to make the site installable
// ("Add to Home Screen" / desktop install icon) - Chrome and Android require an
// active service worker with a fetch handler before they'll offer the install
// prompt. Deliberately conservative: it does NOT try to cache/replay API calls
// (http://localhost:5299), the SignalR hub, or LiveKit media - those must always
// hit the network live, this is a real-time betting game. It only caches the
// static app shell so a repeat visit (or a brief network blip) still shows the
// UI instead of a blank offline error page.
const CACHE_NAME = "sicbo-live-shell-v1";
const APP_SHELL = ["/", "/manifest.webmanifest", "/favicon.svg", "/pwa-192.png", "/pwa-512.png"];

self.addEventListener("install", (event) => {
  event.waitUntil(
    caches
      .open(CACHE_NAME)
      .then((cache) => cache.addAll(APP_SHELL))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) => Promise.all(keys.filter((key) => key !== CACHE_NAME).map((key) => caches.delete(key))))
      .then(() => self.clients.claim())
  );
});

self.addEventListener("fetch", (event) => {
  const { request } = event;
  if (request.method !== "GET") return;

  const url = new URL(request.url);
  // Only handle same-origin static requests. Everything else (the API on
  // :5299, the SignalR hub, LiveKit, any cross-origin call) passes straight
  // through to the network untouched.
  if (url.origin !== self.location.origin) return;

  // Page navigations: try the network first (so users always get the latest
  // build while online), fall back to the cached shell only if offline.
  if (request.mode === "navigate") {
    event.respondWith(
      fetch(request).catch(() => caches.match("/").then((res) => res || Response.error()))
    );
    return;
  }

  // Hashed build assets (/assets/*.js, *.css) never change contents for a
  // given filename, so cache-first is safe and speeds up repeat loads.
  if (url.pathname.startsWith("/assets/")) {
    event.respondWith(
      caches.match(request).then(
        (cached) =>
          cached ||
          fetch(request).then((res) => {
            const copy = res.clone();
            caches.open(CACHE_NAME).then((cache) => cache.put(request, copy));
            return res;
          })
      )
    );
  }
});
