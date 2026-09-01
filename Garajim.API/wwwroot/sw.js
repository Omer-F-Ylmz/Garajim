var KABUK_SURUMU = "garajim-kabuk-v1";
var KABUK_DOSYALARI = [
    "/",
    "/index.html",
    "/styles.css",
    "/app.js",
    "/garajim-logo.svg",
    "/garajim-icon-180.png",
    "/garajim-icon-512.png",
    "/manifest.json"
];

self.addEventListener("install", function (event) {
    event.waitUntil(
        caches.open(KABUK_SURUMU)
            .then(function (cache) { return cache.addAll(KABUK_DOSYALARI); })
            .then(function () { return self.skipWaiting(); })
    );
});

self.addEventListener("activate", function (event) {
    event.waitUntil(
        caches.keys().then(function (adlar) {
            return Promise.all(adlar.map(function (ad) {
                return ad === KABUK_SURUMU ? null : caches.delete(ad);
            }));
        }).then(function () { return self.clients.claim(); })
    );
});

self.addEventListener("fetch", function (event) {
    var istek = event.request;

    if (istek.method !== "GET") {
        return;
    }

    var url = new URL(istek.url);

    if (url.origin !== self.location.origin) {
        return;
    }

    if (url.pathname.indexOf("/api/") === 0) {
        event.respondWith(fetch(istek));
        return;
    }

    if (url.pathname.indexOf("/karne") === 0) {
        event.respondWith(fetch(istek));
        return;
    }

    event.respondWith(
        fetch(istek).then(function (cevap) {
            if (cevap && cevap.status === 200 && cevap.type === "basic") {
                var kopya = cevap.clone();
                caches.open(KABUK_SURUMU).then(function (cache) { cache.put(istek, kopya); });
            }
            return cevap;
        }).catch(function () {
            return caches.match(istek).then(function (onbellekli) {
                return onbellekli || caches.match("/index.html");
            });
        })
    );
});
