// Minimal service worker: caches the static app shell only. It never intercepts
// non-GET requests, API calls, or SignalR (_blazor) traffic — those always go to
// the network untouched, so authenticated pages/responses are never cached.

const CACHE_NAME = 'sisonke-shell-v1';
const PRECACHE_URLS = [
    '/manifest.webmanifest',
    '/offline.html',
    '/favicon.svg',
    '/icon-192.png',
    '/icon-512.png',
    '/app.css'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(PRECACHE_URLS))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys()
            .then(keys => Promise.all(
                keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key))
            ))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', event => {
    const request = event.request;

    if (request.method !== 'GET') {
        return;
    }

    const url = new URL(request.url);
    if (url.origin !== self.location.origin) {
        return;
    }

    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request).catch(() => caches.match('/offline.html'))
        );
        return;
    }

    if (PRECACHE_URLS.includes(url.pathname)) {
        event.respondWith(
            caches.match(request).then(cached => cached ?? fetch(request))
        );
    }
    // Anything else (API endpoints, _blazor negotiate/websocket, other pages) is
    // intentionally left unhandled and falls through to the network as normal.
});
