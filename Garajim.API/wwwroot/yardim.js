(function () {
    "use strict";

    var sorular = [];

    function el(id) {
        return document.getElementById(id);
    }

    function clear(node) {
        while (node.firstChild) {
            node.removeChild(node.firstChild);
        }
    }

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

    function katla(metin) {
        return String(metin || "")
            .toLocaleLowerCase("tr")
            .replace(/ı/g, "i")
            .replace(/ş/g, "s")
            .replace(/ğ/g, "g")
            .replace(/ü/g, "u")
            .replace(/ö/g, "o")
            .replace(/ç/g, "c");
    }

    function uyanlar(arama) {
        var anahtar = katla(arama).trim();
        if (anahtar.length === 0) {
            return sorular;
        }

        return sorular.filter(function (soru) {
            var havuz = katla(soru.baslik + " " + soru.cevap + " " + (soru.anahtarlar || []).join(" "));
            return havuz.indexOf(anahtar) >= 0;
        });
    }

    function kartCiz(soru) {
        var kart = document.createElement("details");
        kart.className = "sss-kart";
        kart.id = soru.id;

        var baslik = document.createElement("summary");
        baslik.appendChild(make("span", soru.baslik));

        var baglanti = make("button", "#", "link-btn sss-capa");
        baglanti.type = "button";
        baglanti.title = "Bu sorunun bağlantısını al";
        baglanti.addEventListener("click", function (olay) {
            olay.preventDefault();
            olay.stopPropagation();
            location.hash = soru.id;
        });
        baslik.appendChild(baglanti);

        kart.appendChild(baslik);
        kart.appendChild(make("p", soru.cevap));

        return kart;
    }

    function listeyiCiz(arama) {
        var liste = el("sss-liste");
        var secilen = uyanlar(arama);

        clear(liste);

        el("sss-sayac").textContent = secilen.length === sorular.length
            ? sorular.length + " soru"
            : secilen.length + " / " + sorular.length + " soru";

        if (secilen.length === 0) {
            liste.appendChild(make("p", "Bu aramaya uyan soru yok. Aşağıdaki destek adresine yazabilirsin.", "hint"));
            return;
        }

        secilen.forEach(function (soru) {
            liste.appendChild(kartCiz(soru));
        });
    }

    function capayiAc() {
        var id = location.hash.replace("#", "");
        if (!id) {
            return;
        }

        var kart = document.getElementById(id);
        if (kart && kart.tagName === "DETAILS") {
            kart.open = true;
            kart.scrollIntoView({ block: "start" });
        }
    }

    function destegiBagla(eposta) {
        var baglanti = el("destek-baglanti");
        if (!eposta) {
            baglanti.textContent = "Destek adresi tanımlı değil";
            return;
        }

        baglanti.href = "mailto:" + eposta + "?subject=" + encodeURIComponent("Garajım yardım");
        baglanti.textContent = eposta;
    }

    function yukle() {
        fetch("/api/Yardim/sss", { headers: { "Accept": "application/json" } })
            .then(function (cevap) { return cevap.json(); })
            .then(function (sonuc) {
                var veri = (sonuc && sonuc.data) || {};
                sorular = veri.sorular || [];
                destegiBagla(veri.destekEposta);
                listeyiCiz("");
                capayiAc();
            })
            .catch(function () {
                el("sss-liste").appendChild(make("p", "Yardım içeriği şu an yüklenemedi. Sayfayı yenilemeyi dene.", "hint"));
            });
    }

    el("sss-ara").addEventListener("input", function (olay) {
        listeyiCiz(olay.target.value);
    });

    window.addEventListener("hashchange", capayiAc);

    yukle();
})();
