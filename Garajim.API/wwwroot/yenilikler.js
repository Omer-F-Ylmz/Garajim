(function () {
    "use strict";

    function make(tag, text, className) {
        var node = document.createElement(tag);
        if (text !== undefined && text !== null) {
            node.textContent = String(text);
        }
        if (className) {
            node.className = className;
        }
        return node;
    }

    function ciz(kayitlar) {
        var kap = document.getElementById("yenilik-liste");

        if (!kayitlar || kayitlar.length === 0) {
            kap.appendChild(make("p", "Sürüm listesi şu an okunamadı.", "hint"));
            return;
        }

        kayitlar.forEach(function (surum) {
            var bolum = document.createElement("section");
            bolum.className = "yenilik-surum";

            bolum.appendChild(make("h2", surum.baslik));

            var liste = document.createElement("ul");
            (surum.maddeler || []).forEach(function (madde) {
                liste.appendChild(make("li", madde));
            });

            bolum.appendChild(liste);
            kap.appendChild(bolum);
        });
    }

    fetch("yenilikler.json", { cache: "no-cache" })
        .then(function (cevap) { return cevap.json(); })
        .then(ciz)
        .catch(function () { ciz(null); });
})();
