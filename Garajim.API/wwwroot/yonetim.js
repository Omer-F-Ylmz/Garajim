(function () {
    "use strict";

    var TOKEN_KEY = "garajim_token";

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

    function token() {
        try {
            return localStorage.getItem(TOKEN_KEY);
        } catch (e) {
            return null;
        }
    }

    function api(yol, secenekler) {
        var ayar = secenekler || {};
        var basliklar = { "Accept": "application/json" };
        var anahtar = token();

        if (anahtar) {
            basliklar.Authorization = "Bearer " + anahtar;
        }

        return fetch(yol, { method: ayar.method || "GET", headers: basliklar }).then(function (cevap) {
            return cevap.text().then(function (metin) {
                var govde = null;

                if (metin) {
                    try {
                        govde = JSON.parse(metin);
                    } catch (e) {
                        govde = null;
                    }
                }

                if (!cevap.ok || (govde && govde.success === false)) {
                    throw new Error((govde && govde.message) || "İstek başarısız (" + cevap.status + ").");
                }

                return govde;
            });
        });
    }

    function sayi(deger, basamak) {
        return new Intl.NumberFormat("tr-TR", { maximumFractionDigits: basamak || 0 }).format(deger || 0);
    }

    function kart(etiket, deger) {
        var kutu = make("div", null, "card");
        kutu.appendChild(make("span", etiket, "card-label"));
        kutu.appendChild(make("strong", deger, "card-value"));
        return kutu;
    }

    function kartlariCiz(veri) {
        var kap = el("ozet-kartlar");
        clear(kap);

        [
            ["Şirket", sayi(veri.sirketSayisi)],
            ["Kullanıcı", sayi(veri.kullaniciSayisi)],
            ["Araç", sayi(veri.aracSayisi)],
            ["Fiş", sayi(veri.fisSayisi)],
            ["Fiş doğruluğu", sayi(veri.fisDogrulukOrani, 1) + " %"],
            ["Oto onay", sayi(veri.otoOnayOrani, 1) + " %"],
            ["Karne paylaşımı", sayi(veri.karnePaylasimOrani, 1) + " %"],
            ["Davet → kayıt", sayi(veri.davetKayitOrani, 1) + " %"],
            ["AI token (ay)", sayi(veri.aiTokenKullanilan)],
            ["AI maliyet", "$" + sayi(veri.aiTahminiMaliyetUsd, 4)],
            ["Kota hatası", sayi(veri.kotaHatasi)],
            ["AI Usta", veri.ustaAcik ? "Açık" : "Kapalı"],
            ["Çalışma kümesi", sayi(veri.bellek ? veri.bellek.calismaKumesiMb : 0, 1) + " MB"],
            ["En yüksek bellek", sayi(veri.bellek ? veri.bellek.enYuksekCalismaKumesiMb : 0, 1) + " MB"],
            ["Sürüm", (veri.bellek && veri.bellek.surum) || "-"]
        ].forEach(function (satir) {
            kap.appendChild(kart(satir[0], satir[1]));
        });
    }

    function seriyiCiz(seri) {
        var govde = el("seri-satirlar");
        clear(govde);

        (seri || []).slice().reverse().forEach(function (gun) {
            var satir = document.createElement("tr");
            satir.appendChild(make("td", gun.gun));
            satir.appendChild(make("td", sayi(gun.sirket)));
            satir.appendChild(make("td", sayi(gun.kullanici)));
            govde.appendChild(satir);
        });
    }

    function geriBildirimleriCiz(liste) {
        var kap = el("geri-bildirim-liste");
        clear(kap);

        if (!liste || liste.length === 0) {
            kap.appendChild(make("li", "Henüz geri bildirim yok."));
            return;
        }

        liste.forEach(function (kayit) {
            var satir = document.createElement("li");
            var tarih = new Date(kayit.tarih).toLocaleDateString("tr-TR");

            satir.appendChild(make("span", tarih + " · " + kayit.tur + " · " + (kayit.kullaniciAdi || "-") +
                " · " + (kayit.sayfa || "-") + " · " + (kayit.surum || "-")));
            satir.appendChild(make("p", kayit.mesaj));

            kap.appendChild(satir);
        });
    }

    function yukle() {
        api("/api/Yonetim/ozet").then(function (sonuc) {
            var veri = (sonuc && sonuc.data) || {};

            kartlariCiz(veri);
            seriyiCiz(veri.gunlukKayitlar);
            geriBildirimleriCiz(veri.sonGeriBildirimler);
        }).catch(function (hata) {
            el("yonetim-mesaj").textContent = hata.message === "İstek başarısız (401)."
                ? "Önce uygulamadan yönetici hesabıyla giriş yapın."
                : hata.message;
        });
    }

    el("demo-sifirla").addEventListener("click", function () {
        el("demo-sifirla").disabled = true;

        api("/api/Yonetim/demo-sifirla", { method: "POST" }).then(function (sonuc) {
            el("yonetim-mesaj").textContent = (sonuc && sonuc.message) || "Demo verisi sıfırlandı.";
        }).catch(function (hata) {
            el("yonetim-mesaj").textContent = hata.message;
        }).finally(function () {
            el("demo-sifirla").disabled = false;
        });
    });

    yukle();
})();
