(function () {
    "use strict";

    var MAINTENANCE_LABELS = {
        PeriyodikBakim: "Periyodik bakım",
        YagDegisimi: "Yağ değişimi",
        FrenBakimi: "Fren bakımı",
        LastikDegisimi: "Lastik değişimi",
        AkuDegisimi: "Akü değişimi",
        Diger: "Diğer"
    };

    var moneyFormat = new Intl.NumberFormat("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    var wholeFormat = new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 0 });

    function el(id) {
        return document.getElementById(id);
    }

    function make(tag, text) {
        var node = document.createElement(tag);
        if (text !== undefined && text !== null) {
            node.textContent = String(text);
        }
        return node;
    }

    function money(value) {
        var sayi = Number(value);
        return isFinite(sayi) ? moneyFormat.format(sayi) + " TL" : "-";
    }

    function km(value) {
        var sayi = Number(value);
        return isFinite(sayi) ? wholeFormat.format(sayi) + " km" : "-";
    }

    function tarih(value) {
        if (!value) {
            return "-";
        }
        var d = new Date(value);
        return isNaN(d.getTime()) ? "-" : d.toLocaleDateString("tr-TR");
    }

    function bilgiEkle(liste, baslik, deger) {
        liste.appendChild(make("dt", baslik));
        liste.appendChild(make("dd", deger));
    }

    function tokenOku() {
        var eslesme = /[?&]t=([^&]+)/.exec(window.location.search);
        return eslesme ? decodeURIComponent(eslesme[1]) : null;
    }

    function hataGoster(mesaj) {
        el("durum").textContent = mesaj;
        el("icerik").classList.add("hidden");
        el("yazdir").classList.add("hidden");
    }

    function acilKartiDenetle(token) {
        fetch("/api/karne/" + encodeURIComponent(token) + "/acil")
            .then(function (cevap) {
                if (!cevap.ok) {
                    return;
                }

                el("acil-baglanti").href = "acil.html?t=" + encodeURIComponent(token);
                el("acil-bolumu").classList.remove("hidden");
            })
            .catch(function () { });
    }

    function ciz(karne, token) {
        var arac = karne.arac;

        el("arac-baslik").textContent = arac.plaka + " — " + arac.marka + " " + arac.model;

        var bilgi = el("arac-bilgi");
        bilgiEkle(bilgi, "Yıl", arac.yil);
        bilgiEkle(bilgi, "Yakıt", arac.yakitTipi);
        bilgiEkle(bilgi, "Güncel kilometre", km(arac.guncelKm));

        if (karne.bakimlar && karne.bakimlar.length > 0) {
            el("bakim-bolumu").classList.remove("hidden");
            var tutarVar = karne.bakimlar.some(function (b) { return b.tutar !== null && b.tutar !== undefined; });
            el("bakim-tutar-baslik").textContent = tutarVar ? "Tutar" : "";

            if (karne.bakimToplami !== null && karne.bakimToplami !== undefined) {
                el("bakim-toplami").classList.remove("hidden");
                el("bakim-toplami").textContent = "Toplam bakım harcaması: " + money(karne.bakimToplami);
            }

            var govde = el("bakim-satirlari");
            karne.bakimlar.forEach(function (bakim) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", tarih(bakim.tarih)));
                tr.appendChild(make("td", MAINTENANCE_LABELS[bakim.tur] || bakim.tur));
                tr.appendChild(make("td", km(bakim.km)));
                tr.appendChild(make("td", bakim.servisAdi || "-"));
                tr.appendChild(make("td", tutarVar && bakim.tutar !== null && bakim.tutar !== undefined ? money(bakim.tutar) : ""));
                govde.appendChild(tr);
            });
        }

        if (karne.parcalar && karne.parcalar.length > 0) {
            el("parca-bolumu").classList.remove("hidden");
            var parcaTutarVar = karne.parcalar.some(function (p) { return p.toplamTutar > 0; });
            el("parca-tutar-baslik").textContent = parcaTutarVar ? "Toplam" : "";

            var parcaGovde = el("parca-satirlari");
            karne.parcalar.forEach(function (parca) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", parca.parcaAdi));
                tr.appendChild(make("td", tarih(parca.sonDegisimTarihi) + (parca.sonDegisimKm ? " · " + km(parca.sonDegisimKm) : "")));
                tr.appendChild(make("td", parca.degisimSayisi));
                tr.appendChild(make("td", parcaTutarVar ? money(parca.toplamTutar) : ""));
                parcaGovde.appendChild(tr);
            });
        }

        if (karne.yakitOzeti && karne.yakitOzeti.kayitSayisi > 0) {
            el("yakit-bolumu").classList.remove("hidden");
            var yakit = el("yakit-bilgi");
            bilgiEkle(yakit, "Dolum sayısı", karne.yakitOzeti.kayitSayisi);
            bilgiEkle(yakit, "Toplam litre", wholeFormat.format(karne.yakitOzeti.toplamLitre) + " L");
            if (karne.yakitOzeti.toplamTutar !== null && karne.yakitOzeti.toplamTutar !== undefined) {
                bilgiEkle(yakit, "Toplam tutar", money(karne.yakitOzeti.toplamTutar));
            }
            bilgiEkle(yakit, "Son dolum", tarih(karne.yakitOzeti.sonDolumTarihi));
        }

        if (karne.beyanDegeri) {
            el("deger-bolumu").classList.remove("hidden");
            var degerBilgi = el("deger-bilgi");
            bilgiEkle(degerBilgi, "Değer", money(karne.beyanDegeri.deger));
            bilgiEkle(degerBilgi, "Kaynak", karne.beyanDegeri.kaynakAdi);
            bilgiEkle(degerBilgi, "Tarih", tarih(karne.beyanDegeri.tarih));
        }

        if (karne.hasarlar && karne.hasarlar.length > 0) {
            el("hasar-bolumu").classList.remove("hidden");
            var hasarGovde = el("hasar-satirlari");
            karne.hasarlar.forEach(function (hasar) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", tarih(hasar.olayTarihi)));
                tr.appendChild(make("td", hasar.tur));
                tr.appendChild(make("td", hasar.durum));
                hasarGovde.appendChild(tr);
            });
        }

        if (karne.belgeler && karne.belgeler.length > 0) {
            el("belge-bolumu").classList.remove("hidden");
            var belgeListesi = el("belge-listesi");
            karne.belgeler.forEach(function (belge) {
                var li = document.createElement("li");
                var link = document.createElement("a");
                link.href = "/api/karne/" + encodeURIComponent(token) + "/belge/" + belge.id;
                link.textContent = belge.ad;
                li.appendChild(link);
                li.appendChild(make("span", " · " + tarih(belge.tarih)));
                belgeListesi.appendChild(li);
            });
        }

        acilKartiDenetle(token);

        el("durum").classList.add("hidden");
        el("icerik").classList.remove("hidden");
    }

    function baslat() {
        el("yazdir").addEventListener("click", function () { window.print(); });

        var token = tokenOku();
        if (!token) {
            hataGoster("Karne bağlantısı geçersiz.");
            return;
        }

        fetch("/api/karne/" + encodeURIComponent(token))
            .then(function (cevap) {
                if (!cevap.ok) {
                    throw new Error("Bu karne bulunamadı ya da paylaşım kapatılmış.");
                }
                return cevap.json();
            })
            .then(function (govde) {
                ciz(govde.data, token);
            })
            .catch(function (hata) {
                hataGoster(hata.message || "Karne yüklenemedi.");
            });
    }

    document.addEventListener("DOMContentLoaded", baslat);
})();
