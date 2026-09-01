(function () {
    "use strict";

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

    function satir(liste, baslik, deger) {
        if (!deger) {
            return;
        }
        liste.appendChild(make("dt", baslik));
        liste.appendChild(make("dd", deger));
    }

    function telefonSatiri(liste, baslik, numara) {
        if (!numara) {
            return;
        }
        liste.appendChild(make("dt", baslik));
        var dd = document.createElement("dd");
        var link = document.createElement("a");
        link.href = "tel:" + numara.replace(/\s/g, "");
        link.textContent = numara;
        dd.appendChild(link);
        liste.appendChild(dd);
    }

    function tokenOku() {
        var eslesme = /[?&]t=([^&]+)/.exec(window.location.search);
        return eslesme ? decodeURIComponent(eslesme[1]) : null;
    }

    function hataGoster(mesaj) {
        el("durum").textContent = mesaj;
        el("kart").classList.add("hidden");
        el("yazdir").classList.add("hidden");
    }

    function ciz(kart) {
        el("plaka").textContent = kart.plaka;
        el("arac").textContent = kart.marka + " " + kart.model + " · " + kart.yil;

        var bilgi = el("bilgi");
        satir(bilgi, "Acil durumda aranacak", kart.acilKisiAd);
        telefonSatiri(bilgi, "Telefon", kart.acilKisiTelefon);
        satir(bilgi, "Trafik sigortası", kart.sigortaSaglayici);
        satir(bilgi, "Poliçe no", kart.sigortaPoliceNo);

        if (kart.acilNot) {
            el("not").classList.remove("hidden");
            el("not").textContent = kart.acilNot;
        }

        el("durum").classList.add("hidden");
        el("kart").classList.remove("hidden");
        el("yazdir").classList.remove("hidden");
    }

    function baslat() {
        el("yazdir").addEventListener("click", function () { window.print(); });

        var token = tokenOku();
        if (!token) {
            hataGoster("Kart bağlantısı geçersiz.");
            return;
        }

        fetch("/api/karne/" + encodeURIComponent(token) + "/acil")
            .then(function (cevap) {
                if (!cevap.ok) {
                    throw new Error("Bu acil durum kartı bulunamadı ya da paylaşım kapatılmış.");
                }
                return cevap.json();
            })
            .then(function (govde) {
                ciz(govde.data);
            })
            .catch(function (hata) {
                hataGoster(hata.message || "Kart yüklenemedi.");
            });
    }

    document.addEventListener("DOMContentLoaded", baslat);
})();
