/*!
 * qr.js — minimal QR Code generator (model 2, byte mode, error level M).
 * MIT License. Lisans metni: vendor/qr-LICENSE.txt
 * Dış servise istek atmaz; kodu tamamen istemcide üretir.
 */
(function (global) {
    "use strict";

    var GALOIS_EXP = new Array(512);
    var GALOIS_LOG = new Array(256);

    (function () {
        var x = 1;
        for (var i = 0; i < 255; i++) {
            GALOIS_EXP[i] = x;
            GALOIS_LOG[x] = i;
            x <<= 1;
            if (x & 0x100) {
                x ^= 0x11d;
            }
        }
        for (var j = 255; j < 512; j++) {
            GALOIS_EXP[j] = GALOIS_EXP[j - 255];
        }
    })();

    function carp(a, b) {
        return a === 0 || b === 0 ? 0 : GALOIS_EXP[GALOIS_LOG[a] + GALOIS_LOG[b]];
    }

    function uretecPolinomu(derece) {
        var poli = [1];
        for (var i = 0; i < derece; i++) {
            var yeni = new Array(poli.length + 1).fill(0);
            for (var j = 0; j < poli.length; j++) {
                yeni[j] ^= carp(poli[j], GALOIS_EXP[i]);
                yeni[j + 1] ^= poli[j];
            }
            poli = yeni;
        }
        return poli;
    }

    function hataDuzeltme(veri, ecUzunluk) {
        var uretec = uretecPolinomu(ecUzunluk);
        var kalan = veri.concat(new Array(ecUzunluk).fill(0));

        for (var i = 0; i < veri.length; i++) {
            var katsayi = kalan[i];
            if (katsayi === 0) {
                continue;
            }
            for (var j = 0; j < uretec.length; j++) {
                kalan[i + j] ^= carp(uretec[j], katsayi);
            }
        }

        return kalan.slice(veri.length);
    }

    // Sürüm başına [toplam kod sözcüğü, EC kod sözcüğü/blok, blok sayısı] — seviye M.
    var SURUMLER = [
        null,
        [26, 10, 1], [44, 16, 1], [70, 26, 1], [100, 18, 2], [134, 24, 2],
        [172, 16, 4], [196, 18, 4], [242, 22, 4], [292, 22, 5], [346, 26, 5],
        [404, 30, 5], [466, 22, 8], [532, 22, 9], [581, 24, 9], [655, 24, 10]
    ];

    var HIZALAMA = [
        [], [], [6, 18], [6, 22], [6, 26], [6, 30], [6, 34],
        [6, 22, 38], [6, 24, 42], [6, 26, 46], [6, 28, 50],
        [6, 30, 54], [6, 32, 58], [6, 34, 62], [6, 26, 46, 66], [6, 26, 48, 70]
    ];

    function surumSec(baytSayisi) {
        for (var surum = 1; surum < SURUMLER.length; surum++) {
            var bilgi = SURUMLER[surum];
            var ecToplam = bilgi[1] * bilgi[2];
            var veriKapasitesi = bilgi[0] - ecToplam;
            var baslikBiti = surum < 10 ? 8 : 16;
            var gerekli = Math.ceil((4 + baslikBiti + baytSayisi * 8) / 8);
            if (veriKapasitesi >= gerekli) {
                return surum;
            }
        }
        return -1;
    }

    function bitYaz(bitler, deger, uzunluk) {
        for (var i = uzunluk - 1; i >= 0; i--) {
            bitler.push((deger >> i) & 1);
        }
    }

    function veriKodSozcukleri(baytlar, surum) {
        var bilgi = SURUMLER[surum];
        var ecToplam = bilgi[1] * bilgi[2];
        var kapasite = bilgi[0] - ecToplam;

        var bitler = [];
        bitYaz(bitler, 4, 4);
        bitYaz(bitler, baytlar.length, surum < 10 ? 8 : 16);
        for (var i = 0; i < baytlar.length; i++) {
            bitYaz(bitler, baytlar[i], 8);
        }

        var bitKapasite = kapasite * 8;
        var sonlandirma = Math.min(4, bitKapasite - bitler.length);
        for (var t = 0; t < sonlandirma; t++) {
            bitler.push(0);
        }
        while (bitler.length % 8 !== 0) {
            bitler.push(0);
        }

        var sozcukler = [];
        for (var b = 0; b < bitler.length; b += 8) {
            var deger = 0;
            for (var k = 0; k < 8; k++) {
                deger = (deger << 1) | bitler[b + k];
            }
            sozcukler.push(deger);
        }

        var dolgu = [0xec, 0x11];
        var d = 0;
        while (sozcukler.length < kapasite) {
            sozcukler.push(dolgu[d++ % 2]);
        }

        return sozcukler;
    }

    function bloklaVeBirlestir(veri, surum) {
        var bilgi = SURUMLER[surum];
        var blokSayisi = bilgi[2];
        var ecUzunluk = bilgi[1];

        var temelUzunluk = Math.floor(veri.length / blokSayisi);
        var fazla = veri.length % blokSayisi;

        var veriBloklari = [];
        var ecBloklari = [];
        var konum = 0;

        for (var i = 0; i < blokSayisi; i++) {
            var uzunluk = temelUzunluk + (i >= blokSayisi - fazla ? 1 : 0);
            var blok = veri.slice(konum, konum + uzunluk);
            konum += uzunluk;
            veriBloklari.push(blok);
            ecBloklari.push(hataDuzeltme(blok, ecUzunluk));
        }

        var sonuc = [];
        var enUzun = Math.max.apply(null, veriBloklari.map(function (b) { return b.length; }));

        for (var s = 0; s < enUzun; s++) {
            for (var v = 0; v < veriBloklari.length; v++) {
                if (s < veriBloklari[v].length) {
                    sonuc.push(veriBloklari[v][s]);
                }
            }
        }

        for (var e = 0; e < ecUzunluk; e++) {
            for (var c = 0; c < ecBloklari.length; c++) {
                sonuc.push(ecBloklari[c][e]);
            }
        }

        return sonuc;
    }

    function matrisOlustur(boyut) {
        var matris = [];
        for (var i = 0; i < boyut; i++) {
            matris.push(new Array(boyut).fill(null));
        }
        return matris;
    }

    function desenYerlestir(matris, boyut, surum) {
        function bulucu(satir, sutun) {
            for (var r = -1; r <= 7; r++) {
                for (var c = -1; c <= 7; c++) {
                    var y = satir + r;
                    var x = sutun + c;
                    if (y < 0 || y >= boyut || x < 0 || x >= boyut) {
                        continue;
                    }
                    var kenar = r === 0 || r === 6 || c === 0 || c === 6;
                    var ic = r >= 2 && r <= 4 && c >= 2 && c <= 4;
                    matris[y][x] = kenar || ic ? 1 : 0;
                }
            }
        }

        bulucu(0, 0);
        bulucu(0, boyut - 7);
        bulucu(boyut - 7, 0);

        for (var i = 8; i < boyut - 8; i++) {
            var deger = i % 2 === 0 ? 1 : 0;
            if (matris[6][i] === null) {
                matris[6][i] = deger;
            }
            if (matris[i][6] === null) {
                matris[i][6] = deger;
            }
        }

        var merkezler = HIZALAMA[surum] || [];
        for (var a = 0; a < merkezler.length; a++) {
            for (var b = 0; b < merkezler.length; b++) {
                var satir = merkezler[a];
                var sutun = merkezler[b];
                if (matris[satir][sutun] !== null) {
                    continue;
                }
                for (var dy = -2; dy <= 2; dy++) {
                    for (var dx = -2; dx <= 2; dx++) {
                        var dis = Math.max(Math.abs(dy), Math.abs(dx));
                        matris[satir + dy][sutun + dx] = dis !== 1 ? 1 : 0;
                    }
                }
            }
        }

        matris[boyut - 8][8] = 1;
    }

    function formatBilgisiYaz(matris, boyut, maskeNo) {
        var veri = (0x00 << 3) | maskeNo;
        var kalan = veri << 10;
        for (var i = 14; i >= 10; i--) {
            if ((kalan >> i) & 1) {
                kalan ^= 0x537 << (i - 10);
            }
        }
        var bicim = ((veri << 10) | kalan) ^ 0x5412;

        for (var b = 0; b < 15; b++) {
            var bit = (bicim >> b) & 1;

            if (b < 6) {
                matris[b][8] = bit;
            } else if (b < 8) {
                matris[b + 1][8] = bit;
            } else if (b === 8) {
                matris[8][7] = bit;
            } else {
                matris[8][14 - b] = bit;
            }

            if (b < 8) {
                matris[8][boyut - 1 - b] = bit;
            } else {
                matris[boyut - 15 + b][8] = bit;
            }
        }
    }

    function maske(satir, sutun, no) {
        switch (no) {
            case 0: return (satir + sutun) % 2 === 0;
            case 1: return satir % 2 === 0;
            case 2: return sutun % 3 === 0;
            case 3: return (satir + sutun) % 3 === 0;
            case 4: return (Math.floor(satir / 2) + Math.floor(sutun / 3)) % 2 === 0;
            case 5: return ((satir * sutun) % 2) + ((satir * sutun) % 3) === 0;
            case 6: return (((satir * sutun) % 2) + ((satir * sutun) % 3)) % 2 === 0;
            default: return (((satir + sutun) % 2) + ((satir * sutun) % 3)) % 2 === 0;
        }
    }

    function veriYerlestir(matris, boyut, sozcukler, maskeNo) {
        var bitler = [];
        for (var i = 0; i < sozcukler.length; i++) {
            for (var b = 7; b >= 0; b--) {
                bitler.push((sozcukler[i] >> b) & 1);
            }
        }

        var indeks = 0;
        var yukari = true;

        for (var sutun = boyut - 1; sutun > 0; sutun -= 2) {
            if (sutun === 6) {
                sutun--;
            }

            for (var adim = 0; adim < boyut; adim++) {
                var satir = yukari ? boyut - 1 - adim : adim;

                for (var k = 0; k < 2; k++) {
                    var x = sutun - k;
                    if (matris[satir][x] !== null) {
                        continue;
                    }

                    var bit = indeks < bitler.length ? bitler[indeks++] : 0;
                    if (maske(satir, x, maskeNo)) {
                        bit ^= 1;
                    }
                    matris[satir][x] = bit;
                }
            }

            yukari = !yukari;
        }
    }

    function uret(metin) {
        var baytlar = [];
        var kodlanmis = unescape(encodeURIComponent(metin));
        for (var i = 0; i < kodlanmis.length; i++) {
            baytlar.push(kodlanmis.charCodeAt(i));
        }

        var surum = surumSec(baytlar.length);
        if (surum < 0) {
            throw new Error("Metin QR kapasitesi için çok uzun.");
        }

        var boyut = 17 + surum * 4;
        var matris = matrisOlustur(boyut);

        desenYerlestir(matris, boyut, surum);

        var veri = veriKodSozcukleri(baytlar, surum);
        var tumSozcukler = bloklaVeBirlestir(veri, surum);

        var maskeNo = 0;
        veriYerlestir(matris, boyut, tumSozcukler, maskeNo);
        formatBilgisiYaz(matris, boyut, maskeNo);

        return matris;
    }

    function canvasaCiz(canvas, metin, modulBoyutu, kenar) {
        var matris = uret(metin);
        var boyut = matris.length;
        var mb = modulBoyutu || 4;
        var kb = kenar === undefined ? 4 : kenar;
        var piksel = (boyut + kb * 2) * mb;

        canvas.width = piksel;
        canvas.height = piksel;

        var ctx = canvas.getContext("2d");
        ctx.fillStyle = "#ffffff";
        ctx.fillRect(0, 0, piksel, piksel);
        ctx.fillStyle = "#000000";

        for (var satir = 0; satir < boyut; satir++) {
            for (var sutun = 0; sutun < boyut; sutun++) {
                if (matris[satir][sutun]) {
                    ctx.fillRect((sutun + kb) * mb, (satir + kb) * mb, mb, mb);
                }
            }
        }
    }

    global.GarajimQR = {
        uret: uret,
        canvasaCiz: canvasaCiz
    };
})(window);
