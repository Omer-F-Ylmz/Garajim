(function () {
    "use strict";

    var KATEGORI_ADI = {
        belirti: "Belirti",
        obd: "Arıza kodu",
        bakim: "Bakım aralığı",
        muayene: "Muayene",
        turkiye: "Türkiye kuralları"
    };

    var kayitlar = [];

    function el(id) {
        return document.getElementById(id);
    }

    function temizle(dugum) {
        while (dugum.firstChild) {
            dugum.removeChild(dugum.firstChild);
        }
    }

    function yap(etiket, metin, sinif) {
        var dugum = document.createElement(etiket);
        if (metin !== undefined && metin !== null) {
            dugum.textContent = String(metin);
        }
        if (sinif) {
            dugum.className = sinif;
        }
        return dugum;
    }

    function sadelestir(metin) {
        return String(metin || "")
            .toLocaleLowerCase("tr")
            .replace(/ç/g, "c").replace(/ğ/g, "g").replace(/ı/g, "i")
            .replace(/ö/g, "o").replace(/ş/g, "s").replace(/ü/g, "u")
            .replace(/[^a-z0-9]+/g, " ")
            .trim();
    }

    function puan(kayit, sorgu) {
        var baslik = kayit.aramaBaslik;
        var anahtar = kayit.aramaAnahtar;

        if (baslik.indexOf(sorgu) === 0) {
            return 3;
        }
        if (baslik.indexOf(sorgu) >= 0) {
            return 2;
        }
        if (anahtar.indexOf(sorgu) >= 0) {
            return 1;
        }
        return 0;
    }

    function ciz(sonuclar, sorgu) {
        var liste = el("rehber-sonuc");
        var sayac = el("rehber-sayac");

        temizle(liste);

        if (!sorgu) {
            sayac.textContent = kayitlar.length + " başlık";
            return;
        }

        sayac.textContent = sonuclar.length + " / " + kayitlar.length + " başlık";

        if (sonuclar.length === 0) {
            liste.appendChild(yap("li", "Sonuç yok. Başka bir sözcük deneyin.", "rehber-bos"));
            return;
        }

        sonuclar.slice(0, 40).forEach(function (kayit) {
            var madde = document.createElement("li");
            var baglanti = document.createElement("a");

            baglanti.href = "/rehber/" + kayit.slug + ".html";
            baglanti.appendChild(yap("span", kayit.baslik, "rehber-sonuc-baslik"));
            baglanti.appendChild(yap("span", KATEGORI_ADI[kayit.kategori] || kayit.kategori, "rehber-sonuc-etiket"));

            madde.appendChild(baglanti);
            liste.appendChild(madde);
        });
    }

    function ara() {
        var sorgu = sadelestir(el("rehber-ara").value);

        if (sorgu.length < 2) {
            ciz([], "");
            return;
        }

        var sonuclar = [];

        kayitlar.forEach(function (kayit) {
            var p = puan(kayit, sorgu);
            if (p > 0) {
                sonuclar.push({ kayit: kayit, puan: p });
            }
        });

        sonuclar.sort(function (a, b) {
            if (a.puan !== b.puan) {
                return b.puan - a.puan;
            }
            return a.kayit.baslik.localeCompare(b.kayit.baslik, "tr");
        });

        ciz(sonuclar.map(function (s) { return s.kayit; }), sorgu);
    }

    function baslat() {
        var kutu = el("rehber-ara");

        if (!kutu) {
            return;
        }

        fetch("/rehber/index.json", { cache: "no-cache" }).then(function (cevap) {
            return cevap.json();
        }).then(function (veri) {
            kayitlar = (veri || []).map(function (kayit) {
                kayit.aramaBaslik = sadelestir(kayit.baslik);
                kayit.aramaAnahtar = sadelestir((kayit.anahtarlar || []).join(" "));
                return kayit;
            });

            el("rehber-sayac").textContent = kayitlar.length + " başlık";
            kutu.addEventListener("input", ara);
            ara();
        }).catch(function () {
            el("rehber-sayac").textContent = "Arama şu an yüklenemedi; aşağıdaki kartlardan gezebilirsiniz.";
        });
    }

    document.addEventListener("DOMContentLoaded", baslat);
})();
