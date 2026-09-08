(function () {
    "use strict";

    var TOKEN_KEY = "garajim_token";
    var USER_KEY = "garajim_user";

    var state = {
        token: null,
        user: null,
        vehicles: [],
        selectedVehicleId: null,
        kazaRehberi: null,
        kazaDosyaId: null,
        kazaTetikleyici: null,
        dogrulanacakEposta: null,
        dogrulaSayac: null,
        duzenlenenAracId: null,
        duzenlenenBakimId: null,
        duzenlenenYakitId: null,
        duzenlenenMasrafId: null,
        duzenlenenEvrakId: null,
        duzenlenenYolculukId: null,
        kilitAc: null,
        duzenlenenAracKm: null,
        ustaGonderiyor: false,
        pwaIstemi: null,
        turSirasi: 0,
        turGosterildi: false,
        sifirlanacakEposta: null,
        hasarAdim: 1,
        hasarYeniKayit: false,
        hasarDosyaId: null,
        degerChart: null,
        documentRecordId: null,
        receiptDraft: null,
        chart: null,
        maliyetChart: null,
        tuketimChart: null
    };

    var KURULUM_ADIMLARI = [
        ["aracVar", "Aracını ekle", "Araç ekle"],
        ["ilkKayitVar", "İlk kaydını gir", "Yakıt ekle"],
        ["evrakVar", "Evrakını tanımla", "Evrak ekle"]
    ];

    var TUR_ADIMLARI = [
        ["#vehicle-select", "Araç kartın", "Üstteki listeden aracını seçersin. Bakım, yakıt ve masraf kayıtlarının tamamı seçili araca yazılır."],
        ["#receipt-btn", "Fiş yükle", "Fişin fotoğrafını çek; tarih, tutar ve litre otomatik dolsun. Kontrol edip onaylarsın."],
        ["[data-tab=\"bakim\"]", "Bakım ve parça", "Yapılan işi parçalarıyla birlikte yazarsın; aynı parça yenilenince hatırlatma çıkar."],
        ["[data-tab=\"evrak\"]", "Evrak takvimi", "Muayene, sigorta ve kaskonun bitişini gir; süresi yaklaşınca e-posta ve takvim uyarısı gelir."],
        ["#karne-btn", "Karne paylaş", "Aracın bakım geçmişini tek bağlantıyla paylaşırsın; satarken alıcı geçmişi görür."],
        ["#ayarlar-btn", "Ayarlar", "Şifre, takvim aboneliği, plan ve ekip işlerinin tamamı burada."]
    ];

    var BOS_DURUMLAR = {
        arac: ["Araçlarını buraya ekler, bütün kayıtları tek yerde toplarsın.", "İlk aracını ekle", "#add-vehicle-btn"],
        yakit: ["Her dolumu yazarsan ortalama tüketimini ve yakıt maliyetini görürsün.", "İlk dolumu ekle", "#fuel-date"],
        bakim: ["Yapılan işi parçalarıyla yazarsan aracın bakım geçmişi belgeli olur.", "İlk bakımı ekle", "#maintenance-date"],
        masraf: ["Sigorta, muayene, otopark gibi harcamalar burada toplanır.", "İlk masrafı ekle", "#expense-date"],
        fis: ["Fişin fotoğrafını çekersen tarih, tutar ve litre otomatik dolar.", "Fiş yükle", "#receipt-file"],
        evrak: ["Muayene ve sigorta bitişlerini girersen süresi yaklaşınca uyarı gelir.", "İlk evrakı ekle", "#evrak-tur"],
        lastik: ["Yaz ve kış setlerini kaydedersen değişim zamanı ve set ömrü izlenir.", "İlk lastik setini ekle", "#lastik-tarih"],
        hasar: ["Kaza ve hasarları fotoğraflarıyla dosyalarsan tutanak çıktısı hazır olur.", "Hasar dosyası aç", "#hasar-yeni"],
        yolculuk: ["Görev ve özel yolculukları ayırırsan iş kilometresi raporlanabilir.", "İlk yolculuğu ekle", "#yolculuk-tarih"],
        karne: ["Aracın bakım geçmişini tek bağlantıyla paylaşırsın; satarken alıcı geçmişi görür.", "Bağlantı oluştur", "#karne-olustur"],
        ekip: ["Yönetici ve sürücü ekleyip araçları zimmetleyebilirsin.", "İlk üyeyi ekle", "#team-name"]
    };

    var TEAM_ROLES = [
        ["Manager", "Yönetici"],
        ["Driver", "Sürücü"],
        ["Owner", "Sahip"]
    ];

    var MAINTENANCE_TYPES = [
        ["PeriyodikBakim", "Periyodik bakım"],
        ["YagDegisimi", "Yağ değişimi"],
        ["FrenBakimi", "Fren bakımı"],
        ["LastikDegisimi", "Lastik değişimi"],
        ["AkuDegisimi", "Akü değişimi"],
        ["Diger", "Diğer"]
    ];

    var FUEL_TYPES = [
        ["Benzin", "Benzin"],
        ["Dizel", "Dizel"],
        ["Lpg", "LPG"],
        ["Hibrit", "Hibrit"],
        ["Elektrik", "Elektrik"]
    ];

    var EXPENSE_CATEGORIES = [
        ["TrafikSigortasi", "Trafik sigortası"],
        ["Kasko", "Kasko"],
        ["Mtv", "MTV"],
        ["Muayene", "Muayene"],
        ["EgzozEmisyon", "Egzoz emisyon"],
        ["Otopark", "Otopark"],
        ["Kopru", "Köprü / otoyol"],
        ["TrafikCezasi", "Trafik cezası"],
        ["Yikama", "Yıkama"],
        ["Diger", "Diğer"]
    ];

    var REMINDER_TYPES = [
        ["Muayene", "Muayene"],
        ["TrafikSigortasi", "Trafik sigortası"],
        ["Kasko", "Kasko"],
        ["EgzozEmisyon", "Egzoz emisyon"],
        ["Mtv", "MTV"],
        ["PeriyodikBakim", "Periyodik bakım"],
        ["LastikDegisimi", "Lastik değişimi"],
        ["Diger", "Diğer"]
    ];

    var PRICE_FUEL = ["Benzin", "Dizel", "LPG & Benzin", "Hibrit", "Elektrik"];
    var PRICE_GEAR = ["Düz", "Otomatik", "Yarı Otomatik"];
    var PRICE_BODY = ["Sedan", "Hatchback/5", "Hatchback/3", "Station wagon", "MPV", "Coupe", "SUV", "Cabrio", "Roadster", "Pick-up"];

    var VITES_TIPLERI = [
        ["", "Seçiniz"],
        ["Otomatik", "Otomatik"],
        ["Düz", "Düz"],
        ["Yarı Otomatik", "Yarı otomatik"]
    ];

    var KULLANIM_TURLERI = [
        ["Hususi", "Hususi"],
        ["Ticari", "Ticari"]
    ];

    var KASA_TIPLERI = [
        ["", "Seçiniz"],
        ["Sedan", "Sedan"],
        ["Hatchback5", "Hatchback (5 kapı)"],
        ["Hatchback3", "Hatchback (3 kapı)"],
        ["StationWagon", "Station wagon"],
        ["Mpv", "MPV"],
        ["Coupe", "Coupe"],
        ["Suv", "SUV"],
        ["Cabrio", "Cabrio"],
        ["Roadster", "Roadster"],
        ["PickUp", "Pick-up"]
    ];

    var moneyFormat = new Intl.NumberFormat("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    var wholeFormat = new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 0 });
    var literFormat = new Intl.NumberFormat("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    function mutlakAdres(adres) {
        var deger = String(adres || "").trim();

        if (deger.length === 0) {
            return "";
        }

        if (/^https?:\/\//i.test(deger)) {
            return deger;
        }

        return window.location.origin + (deger.charAt(0) === "/" ? deger : "/" + deger);
    }

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

    function labelOf(pairs, value) {
        for (var i = 0; i < pairs.length; i++) {
            if (pairs[i][0] === value) {
                return pairs[i][1];
            }
        }
        return value === null || value === undefined ? "" : String(value);
    }

    function fillSelect(select, pairs) {
        clear(select);
        pairs.forEach(function (pair) {
            var option = document.createElement("option");
            option.value = pair[0];
            option.textContent = pair[1];
            select.appendChild(option);
        });
    }

    function fillSimpleSelect(select, values) {
        fillSelect(select, values.map(function (value) { return [value, value]; }));
    }

    function sayiOku(deger) {
        var metin = String(deger === null || deger === undefined ? "" : deger).trim().replace(/\s/g, "");

        if (metin === "") {
            return NaN;
        }

        var noktali = metin.lastIndexOf(".");
        var virgullu = metin.lastIndexOf(",");

        if (noktali >= 0 && virgullu >= 0) {
            metin = virgullu > noktali
                ? metin.replace(/\./g, "").replace(",", ".")
                : metin.replace(/,/g, "");
        } else if (virgullu >= 0) {
            metin = metin.replace(/\./g, "").replace(",", ".");
        } else if (noktali >= 0) {
            var kuyruk = metin.length - noktali - 1;
            if (metin.indexOf(".") !== noktali || kuyruk === 3) {
                metin = metin.replace(/\./g, "");
            }
        }

        return /^-?\d*(\.\d+)?$/.test(metin) ? Number(metin) : NaN;
    }

    function sayiAlan(id) {
        return sayiOku(el(id).value);
    }

    function yuzde(deger, basamak) {
        var sayi = Number(deger);

        if (!isFinite(sayi)) {
            sayi = 0;
        }

        return "%" + new Intl.NumberFormat("tr-TR", {
            minimumFractionDigits: basamak || 0,
            maximumFractionDigits: basamak || 0
        }).format(sayi);
    }

    var ARSIV_NEDENLERI = {
        Satildi: "Satıldı",
        Hurda: "Hurda",
        Diger: "Diğer"
    };

    function money(value) {

        var number = Number(value);
        if (!isFinite(number)) {
            return "-";
        }
        return moneyFormat.format(number) + " TL";
    }

    function km(value) {
        var number = Number(value);
        if (!isFinite(number)) {
            return "-";
        }
        return wholeFormat.format(number) + " km";
    }

    function formatDate(value) {
        if (!value) {
            return "-";
        }
        var date = new Date(value);
        if (isNaN(date.getTime())) {
            return "-";
        }
        return date.toLocaleDateString("tr-TR");
    }

    function todayInput() {
        var now = new Date();
        var month = String(now.getMonth() + 1).padStart(2, "0");
        var day = String(now.getDate()).padStart(2, "0");
        return now.getFullYear() + "-" + month + "-" + day;
    }

    function gonderimKilitle(dugmeId, kilitli, bekleyenMetin) {
        var dugme = el(dugmeId);
        if (!dugme) {
            return;
        }

        if (kilitli) {
            if (!dugme.dataset.eskiMetin) {
                dugme.dataset.eskiMetin = dugme.textContent;
            }
            dugme.disabled = true;
            dugme.textContent = bekleyenMetin || "Gönderiliyor…";
            return;
        }

        dugme.disabled = false;
        if (dugme.dataset.eskiMetin) {
            dugme.textContent = dugme.dataset.eskiMetin;
            delete dugme.dataset.eskiMetin;
        }
    }

    var mesajSayaclari = {};

    function showMessage(node, text, isOk) {
        node.textContent = text || "";
        node.className = isOk ? "message ok" : "message";

        if (node.id) {
            window.clearTimeout(mesajSayaclari[node.id]);

            if (text) {
                mesajSayaclari[node.id] = window.setTimeout(function () {
                    if (node.textContent === text) {
                        node.textContent = "";
                        node.className = "message";
                    }
                }, isOk ? 6000 : 12000);
            }
        }

        if (text && node.id === "app-message" && typeof node.scrollIntoView === "function") {
            node.scrollIntoView({ block: "nearest", behavior: "smooth" });
        }
    }

    function clearMessages() {
        showMessage(el("auth-message"), "");
        showMessage(el("app-message"), "");
    }

    function saveSession(token, user) {
        state.token = token;
        state.user = user;
        localStorage.setItem(TOKEN_KEY, token);
        localStorage.setItem(USER_KEY, JSON.stringify(user));
    }

    function clearSession() {
        state.token = null;
        state.user = null;
        state.vehicles = [];
        state.selectedVehicleId = null;
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        localStorage.removeItem(ACIL_KART_ANAHTARI);
        localStorage.removeItem(KAZA_KUYRUK_ANAHTARI);
        localStorage.removeItem(KAZA_REHBER_ANAHTARI);
    }

    function readSession() {
        var token = localStorage.getItem(TOKEN_KEY);
        if (!token) {
            return false;
        }
        state.token = token;
        try {
            state.user = JSON.parse(localStorage.getItem(USER_KEY) || "null");
        } catch (error) {
            state.user = null;
        }
        if (!state.user || !state.user.role) {
            clearSession();
            return false;
        }
        return true;
    }

    function grafikleriTemizle() {
        ["chart", "maliyetChart", "tuketimChart", "degerChart"].forEach(function (ad) {
            if (state[ad]) {
                state[ad].destroy();
                state[ad] = null;
            }
        });
    }

    function goToLogin(message) {
        clearSession();
        grafikleriTemizle();
        el("app-screen").classList.add("hidden");
        el("auth-screen").classList.remove("hidden");
        showMessage(el("auth-message"), message || "");
    }

    function formuKilitle(form) {
        if (!form) {
            return function () { };
        }

        var dugmeler = Array.prototype.filter.call(
            form.querySelectorAll("button"),
            function (d) { return d.type !== "button"; });

        dugmeler.forEach(function (d) {
            d.dataset.eskiMetin = d.textContent;
            d.disabled = true;
            d.setAttribute("aria-busy", "true");
            d.textContent = "Kaydediliyor…";
        });

        var acildi = false;

        var ac = function () {
            if (acildi) {
                return;
            }

            acildi = true;

            dugmeler.forEach(function (d) {
                d.disabled = false;
                d.removeAttribute("aria-busy");

                if (d.dataset.eskiMetin) {
                    d.textContent = d.dataset.eskiMetin;
                    delete d.dataset.eskiMetin;
                }
            });
        };

        state.kilitAc = ac;

        return ac;
    }

    var OTURUM_KUYRUK_SINIRI = 3;
    var TASLAK_ONEKI = "garajim-taslak-";
    var TASLAK_FORMLARI = ["maintenance-form", "fuel-form", "expense-form", "reminder-form", "evrak-form", "yolculuk-form"];

    var oturumKuyrugu = [];
    var oturumModaliAcik = false;

    function oturumKuyruguna(path, options) {
        taslaklariKaydet();

        var yontem = ((options && options.method) || "GET").toUpperCase();

        if (yontem === "GET") {
            oturumModaliAc();
            return Promise.reject(new Error("Oturum gerekli."));
        }

        return new Promise(function (coz, reddet) {
            while (oturumKuyrugu.length >= OTURUM_KUYRUK_SINIRI) {
                var dusen = oturumKuyrugu.shift();
                dusen.reddet(new Error("Oturum gerekli."));
            }

            oturumKuyrugu.push({ path: path, options: options, coz: coz, reddet: reddet });
            oturumModaliAc();
        });
    }

    function oturumModaliAc() {
        if (oturumModaliAcik) {
            return;
        }

        oturumModaliAcik = true;

        var kutu = el("oturum-modal");
        if (!kutu) {
            return;
        }

        el("oturum-eposta").value = (state.user && state.user.email) || "";
        el("oturum-sifre").value = "";
        el("oturum-mesaj").textContent = "";

        kutu.classList.remove("hidden");
        document.addEventListener("keydown", oturumModaliKlavye);
        el("oturum-sifre").focus();
    }

    function oturumModaliKlavye(olay) {
        if (olay.key === "Escape") {
            oturumModaliKapat();
        }
    }

    function oturumKuyrugunuOynat() {
        var bekleyen = oturumKuyrugu.slice();
        oturumKuyrugu = [];

        bekleyen.forEach(function (istek) {
            api(istek.path, istek.options).then(istek.coz).catch(istek.reddet);
        });
    }

    function oturumKuyrugunuBosalt() {
        var bekleyen = oturumKuyrugu.slice();
        oturumKuyrugu = [];

        bekleyen.forEach(function (istek) {
            istek.reddet(new Error("Oturum gerekli."));
        });
    }

    function oturumModaliKapat() {
        var kutu = el("oturum-modal");

        oturumModaliAcik = false;
        oturumKuyrugunuBosalt();

        if (kutu) {
            kutu.classList.add("hidden");
        }

        document.removeEventListener("keydown", oturumModaliKlavye);
    }

    function taslakAnahtari(formId) {
        return TASLAK_ONEKI + formId;
    }

    function taslaklariKaydet() {
        TASLAK_FORMLARI.forEach(function (formId) {
            var form = el(formId);
            if (!form) {
                return;
            }

            var degerler = {};
            var doluMu = false;

            [].forEach.call(form.querySelectorAll("input, select, textarea"), function (alan) {
                if (!alan.id || alan.type === "password" || alan.type === "file") {
                    return;
                }

                var deger = alan.type === "checkbox" ? alan.checked : alan.value;

                if (deger !== "" && deger !== false) {
                    doluMu = true;
                }

                degerler[alan.id] = deger;
            });

            try {
                if (doluMu) {
                    sessionStorage.setItem(taslakAnahtari(formId), JSON.stringify(degerler));
                } else {
                    sessionStorage.removeItem(taslakAnahtari(formId));
                }
            } catch (hata) {
                return;
            }
        });
    }

    function taslaklariGeriYukle() {
        TASLAK_FORMLARI.forEach(function (formId) {
            var form = el(formId);
            if (!form) {
                return;
            }

            var ham = null;

            try {
                ham = sessionStorage.getItem(taslakAnahtari(formId));
            } catch (hata) {
                return;
            }

            if (!ham) {
                return;
            }

            var degerler = null;

            try {
                degerler = JSON.parse(ham);
            } catch (hata) {
                return;
            }

            Object.keys(degerler || {}).forEach(function (alanId) {
                var alan = el(alanId);
                if (!alan) {
                    return;
                }

                if (alan.type === "checkbox") {
                    alan.checked = degerler[alanId] === true;
                    return;
                }

                alan.value = degerler[alanId];
            });
        });
    }

    function taslaklariTemizle() {
        TASLAK_FORMLARI.forEach(function (formId) {
            try {
                sessionStorage.removeItem(taslakAnahtari(formId));
            } catch (hata) {
                return;
            }
        });
    }

    function bindOturumModali() {
        var form = el("oturum-form");
        if (!form) {
            return;
        }

        form.addEventListener("submit", function (olay) {
            olay.preventDefault();

            var eposta = el("oturum-eposta").value;
            var sifre = el("oturum-sifre").value;

            el("oturum-mesaj").textContent = "Giriş yapılıyor…";

            api("/api/Auth/login", { method: "POST", body: { email: eposta, password: sifre } })
                .then(function (sonuc) {
                    var veri = sonuc && sonuc.data;

                    if (!veri || !veri.token) {
                        el("oturum-mesaj").textContent = "Giriş yapılamadı.";
                        return;
                    }

                    saveSession(veri.token, veri);

                    oturumModaliAcik = false;
                    el("oturum-modal").classList.add("hidden");
                    document.removeEventListener("keydown", oturumModaliKlavye);
                    el("oturum-sifre").value = "";
                    el("oturum-mesaj").textContent = "";

                    taslaklariGeriYukle();
                    oturumKuyrugunuOynat();
                })
                .catch(function (hata) {
                    el("oturum-mesaj").textContent = (hata && hata.message) || "Giriş yapılamadı.";
                });
        });

        el("oturum-cikis").addEventListener("click", function () {
            oturumModaliKapat();
            taslaklariTemizle();
            goToLogin("Oturum süreniz doldu, lütfen tekrar giriş yapın.");
        });
    }

    function kmTazelikMetni(sonGuncelleme) {
        if (!sonGuncelleme) {
            return "hiç güncellenmedi";
        }

        var gun = Math.floor((Date.now() - new Date(sonGuncelleme).getTime()) / 86400000);

        if (gun <= 0) {
            return "bugün";
        }

        if (gun === 1) {
            return "dün";
        }

        return gun + " gün önce";
    }

    function kmBayatMi(sonGuncelleme) {
        if (!sonGuncelleme) {
            return true;
        }

        return (Date.now() - new Date(sonGuncelleme).getTime()) / 86400000 >= 30;
    }

    function kmRozetiniTazele() {
        var rozet = el("km-rozet");
        if (!rozet) {
            return;
        }

        var arac = seciliArac();

        if (!arac) {
            rozet.classList.add("hidden");
            return;
        }

        rozet.classList.remove("hidden");
        rozet.textContent = km(arac.currentKm) + " · " + kmTazelikMetni(arac.sonKmGuncelleme);
        rozet.classList.toggle("bayat", kmBayatMi(arac.sonKmGuncelleme));

        var duzenlenebilir = canManage() && !arac.arsivli;

        rozet.disabled = !duzenlenebilir;
        rozet.title = duzenlenebilir ? "Kilometreyi güncelle" : "Kilometre bilgisi";
    }

    function kmModaliAc() {
        var arac = seciliArac();
        if (!arac || !canManage()) {
            return;
        }

        el("km-modal-aciklama").textContent = arac.plate + " · kayıtlı kilometre " + km(arac.currentKm)
            + " (" + kmTazelikMetni(arac.sonKmGuncelleme) + ")";
        el("km-modal-deger").value = arac.currentKm;
        el("km-modal-mesaj").textContent = "";

        el("km-modal").classList.remove("hidden");
        document.addEventListener("keydown", kmModaliKlavye);
        el("km-modal-deger").focus();
    }

    function kmModaliKapat() {
        el("km-modal").classList.add("hidden");
        document.removeEventListener("keydown", kmModaliKlavye);
    }

    function kmModaliKlavye(olay) {
        if (olay.key === "Escape") {
            kmModaliKapat();
        }
    }

    function kmModaliKaydet() {
        var arac = seciliArac();
        if (!arac) {
            return;
        }

        var yeni = Number(el("km-modal-deger").value);

        if (!yeni || yeni < arac.currentKm) {
            el("km-modal-mesaj").textContent = "Kilometre azaltılamaz; kayıtlı değer " + km(arac.currentKm) + ".";
            return;
        }

        el("km-modal-mesaj").textContent = "Kaydediliyor…";

        api("/api/Vehicles/" + arac.id + "/km", { method: "PUT", body: { currentKm: yeni } })
            .then(function (sonuc) {
                kmModaliKapat();
                showMessage(el("app-message"), (sonuc && sonuc.message) || "Kilometre güncellendi.", true);
                return loadVehicles();
            })
            .catch(function (hata) {
                el("km-modal-mesaj").textContent = (hata && hata.message) || "Kilometre güncellenemedi.";
            });
    }

    function bindKmRozeti() {
        var rozet = el("km-rozet");
        if (!rozet) {
            return;
        }

        rozet.addEventListener("click", kmModaliAc);
        el("km-modal-vazgec").addEventListener("click", kmModaliKapat);

        el("km-modal-form").addEventListener("submit", function (olay) {
            olay.preventDefault();
            kmModaliKaydet();
        });
    }

    function api(path, options) {
        var settings = options || {};
        var headers = { "Accept": "application/json" };
        if (state.token) {
            headers.Authorization = "Bearer " + state.token;
        }
        var init = { method: settings.method || "GET", headers: headers };
        if (settings.body !== undefined) {
            if (typeof FormData !== "undefined" && settings.body instanceof FormData) {
                init.body = settings.body;
            } else {
                headers["Content-Type"] = "application/json";
                init.body = JSON.stringify(settings.body);
            }
        }

        var isAuthCall = path.indexOf("/api/Auth/") === 0;

        return fetch(path, init).then(function (response) {
            if (response.status === 401 && !isAuthCall) {
                return oturumKuyruguna(path, options);
            }
            return response.text().then(function (text) {
                var payload = null;
                if (text) {
                    try {
                        payload = JSON.parse(text);
                    } catch (error) {
                        payload = null;
                    }
                }
                if (payload && payload.success === false) {
                    var hata = new Error(payload.message || "İşlem başarısız.");
                    hata.kod = payload.kod || null;
                    hata.durum = response.status;
                    throw hata;
                }
                if (!response.ok) {
                    throw new Error(readProblem(payload) || "Sunucu hatası (" + response.status + ").");
                }
                return payload;
            });
        }).finally(function () {
            if (typeof state.kilitAc === "function") {
                state.kilitAc();
                state.kilitAc = null;
            }
        });
    }

    function readProblem(payload) {
        if (!payload) {
            return null;
        }
        if (payload.errors) {
            var keys = Object.keys(payload.errors);
            if (keys.length > 0) {
                var first = payload.errors[keys[0]];
                if (Array.isArray(first) && first.length > 0) {
                    return "Geçersiz alan: " + keys[0];
                }
            }
        }
        return payload.title || payload.message || null;
    }

    function handleError(node, error) {
        var text = error && error.message ? error.message : "Beklenmeyen bir hata oluştu.";
        if (text === "Oturum gerekli.") {
            return;
        }
        if (text === "Failed to fetch") {
            text = "Sunucuya ulaşılamadı.";
        }
        showMessage(node, text);
    }

    function switchAuthTab(showLogin) {
        el("tab-login").classList.toggle("active", showLogin);
        el("tab-register").classList.toggle("active", !showLogin);
        el("login-form").classList.toggle("hidden", !showLogin);
        el("register-form").classList.toggle("hidden", showLogin);
        showMessage(el("auth-message"), "");
    }

    function currentRole() {
        return (state.user && state.user.role) || "Owner";
    }

    function isOwner() {
        return currentRole() === "Owner";
    }

    function canManage() {
        var role = currentRole();
        return role === "Owner" || role === "Manager";
    }

    function ustaSekmesiniGizle() {
        var sekme = document.querySelector('.tab-btn[data-tab="usta"]');

        if (sekme) {
            sekme.classList.add("hidden");

            if (sekme.classList.contains("active")) {
                selectTab("bakim");
            }
        }

        el("usta-onay-kutusu").classList.add("hidden");
        el("usta-govde").classList.add("hidden");
    }

    function ustaDurumunuUygula() {
        return api("/api/Saglik/ozellikler").then(function (result) {
            var acik = !!(result && result.data && result.data.ustaAcik);
            var sekme = document.querySelector('.tab-btn[data-tab="usta"]');

            if (!acik) {
                ustaSekmesiniGizle();
                return;
            }

            if (sekme) {
                sekme.classList.remove("hidden");
            }
        }).catch(function () {
            ustaSekmesiniGizle();
        });
    }

    function applyRole() {
        el("add-vehicle-btn").classList.toggle("hidden", !canManage());
        el("edit-vehicle-btn").classList.toggle("hidden", !canManage());
        el("team-btn").classList.toggle("hidden", !canManage());
        el("team-form").classList.toggle("hidden", !isOwner());
        el("plan-bolumu").classList.toggle("hidden", !isOwner());
        el("davet-bolumu").classList.toggle("hidden", !isOwner());
        el("import-bolumu").classList.toggle("hidden", !canManage());
        el("evrak-form").classList.toggle("hidden", !canManage());
        el("karne-btn").classList.toggle("hidden", !canManage());
        el("arsiv-btn").classList.toggle("hidden", !canManage());

        el("ornek-ekle").classList.toggle("hidden", !canManage());
        el("ornek-sil").classList.toggle("hidden", !canManage());
        el("hesap-sil-aciklama").textContent = isOwner()
            ? "Şirket sahibi hesabı silerse şirket, araçlar, kayıtlar ve belgeler 7 gün sonra kalıcı olarak silinir. Bu süre içinde iptal edebilirsiniz."
            : "Hesabını silersen kişisel bilgilerin kaldırılır; şirketin kayıtları yerinde kalır.";

        var ornekBolumu = el("ornek-bolumu");

        if (ornekBolumu) {
            ornekBolumu.classList.toggle("hidden", !canManage());
        }

        el("hesap-sil-kod").classList.toggle("hidden", !isOwner());
        el("uye-hesap-sil").classList.toggle("hidden", isOwner());

        var zimmetTab = document.querySelector('.tab-btn[data-manager-only="true"]');
        if (zimmetTab) {
            zimmetTab.classList.toggle("hidden", !canManage());
            if (!canManage() && zimmetTab.classList.contains("active")) {
                selectTab("bakim");
            }
        }
        if (!canManage()) {
            el("team-box").classList.add("hidden");
        }
        if (!canManage()) {
            el("karne-box").classList.add("hidden");
        }
    }

    function fisDugmesiniTazele() {
        var dugme = el("receipt-btn");

        if (!dugme) {
            return;
        }

        var aracVar = (state.vehicles || []).length > 0;
        var bekleyenVar = (state.bekleyenFisSayisi || 0) > 0;
        var gorunur = aracVar || bekleyenVar;

        dugme.classList.toggle("hidden", !gorunur);

        if (!gorunur) {
            el("receipt-box").classList.add("hidden");
        }
    }

    function ustSeritEtiketi(user) {
        var ad = ((user && user.fullName) || "").trim();
        var sirket = ((user && user.companyName) || "").trim();

        if (!sirket) {
            return ad;
        }

        if (!ad) {
            return sirket;
        }

        if (ad.toLocaleLowerCase("tr") === sirket.toLocaleLowerCase("tr")) {
            return ad;
        }

        return ad + " · " + sirket;
    }

    function enterApp() {
        el("auth-screen").classList.add("hidden");
        el("app-screen").classList.remove("hidden");
        var user = state.user || {};
        el("user-label").textContent = ustSeritEtiketi(user);
        geciciSifreUyarisi(user);
        hesapDurumunuYukle();
        applyRole();
        profiliYukle();
        loadVehicles();
        loadPendingReceipts();
    }

    function loadVehicles() {
        loadPanelUyarisi();
        kurulumDurumunuYukle();
        return api("/api/Vehicles").then(function (result) {
            state.vehicles = (result && result.data) || [];
            var select = el("vehicle-select");
            clear(select);

            state.vehicles.forEach(function (vehicle) {
                var option = document.createElement("option");
                option.value = String(vehicle.id);
                option.textContent = vehicle.plate + " - " + vehicle.brand + " " + vehicle.model + (vehicle.ornek ? " · Örnek" : "");
                select.appendChild(option);
            });

            var hasVehicles = state.vehicles.length > 0;
            renderEmptyState(hasVehicles);
            el("empty-state").classList.toggle("hidden", hasVehicles);
            el("workspace").classList.toggle("hidden", !hasVehicles);
            el("kaza-ani").classList.toggle("hidden", !hasVehicles);
            select.classList.toggle("hidden", !hasVehicles);

            if (!hasVehicles) {
                state.selectedVehicleId = null;
                kmRozetiniTazele();
                fisDugmesiniTazele();
                return;
            }

            var stillThere = state.vehicles.some(function (vehicle) {
                return vehicle.id === state.selectedVehicleId;
            });
            if (!stillThere) {
                state.selectedVehicleId = state.vehicles[0].id;
            }
            select.value = String(state.selectedVehicleId);
            ustaDurumunuUygula();
            kmRozetiniTazele();
            fisDugmesiniTazele();
            kmSeridiniGuncelle();
            katalogUyarisiniGuncelle();
            tescilUyarisiniGuncelle();
            acilKartiSakla();
            kuyrugoBosalt().then(kuyrukRozetiniGuncelle);
            loadActiveTab();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function renderEmptyState(hasVehicles) {
        if (hasVehicles) {
            return;
        }
        var kap = el("empty-kutu");
        clear(kap);
        el("empty-ornek").classList.toggle("hidden", !canManage());

        if (canManage()) {
            el("empty-title").textContent = "Henüz aracınız yok";
            kap.appendChild(bosDurumKutusu("arac"));
        } else {
            el("empty-title").textContent = "Size zimmetli araç yok";
            kap.appendChild(make("p", "Bir araç zimmetlendiğinde kayıtları burada görürsünüz. Zimmet için şirket yöneticinize başvurun."));
        }
    }

    var GERI_BILDIRIM_TURLERI = [
        ["Hata", "Hata bildir"],
        ["Oneri", "Öneri"],
        ["Diger", "Diğer"]
    ];

    function geriBildirimSayfasi() {
        if (el("app-screen").classList.contains("hidden")) {
            return "giris";
        }

        var acikKutu = ["receipt-box", "ayarlar-box", "karne-box", "team-box", "arsiv-box"].filter(function (id) {
            var n = el(id);
            return n && !n.classList.contains("hidden");
        })[0];

        return acikKutu ? acikKutu.replace("-box", "") : activeTab();
    }

    function geriBildirimGonder(event) {
        event.preventDefault();

        var acKilit = formuKilitle(event.target);
        var kutu = el("geri-bildirim-mesaji");

        api("/api/GeriBildirim", {
            method: "POST",
            body: {
                tur: el("geri-bildirim-tur").value,
                mesaj: el("geri-bildirim-mesaj").value,
                sayfa: geriBildirimSayfasi(),
                surum: document.documentElement.dataset.surum || ""
            }
        }).then(function (sonuc) {
            showMessage(kutu, (sonuc && sonuc.message) || "Geri bildirimin alındı.", true);
            el("geri-bildirim-mesaj").value = "";
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (hata) {
            handleError(kutu, hata);
        });
    }

    var IOS_IPUCU_ANAHTARI = "garajim_ios_ipucu";
    var IOS_IPUCU_GUN = 30;

    function pwaSeridiniAc(metin, yuklemeVar) {
        el("pwa-serit-metin").textContent = metin;
        el("pwa-yukle").classList.toggle("hidden", !yuklemeVar);
        el("pwa-serit").classList.remove("hidden");
    }

    function pwaSeridiniKapat() {
        el("pwa-serit").classList.add("hidden");
        yerelYaz(IOS_IPUCU_ANAHTARI, String(Date.now()));
    }

    function ipucuErtelendiMi() {
        var son = Number(yerelOku(IOS_IPUCU_ANAHTARI) || 0);
        if (!son) {
            return false;
        }

        return Date.now() - son < IOS_IPUCU_GUN * 24 * 60 * 60 * 1000;
    }

    function iosMu() {
        if (/iphone|ipad|ipod/i.test(navigator.userAgent)) {
            return true;
        }

        return /macintosh/i.test(navigator.userAgent) && navigator.maxTouchPoints > 1;
    }

    function iosSeridiniDegerlendir() {
        if (!iosMu() || ipucuErtelendiMi()) {
            return;
        }

        var kurulu = window.navigator.standalone === true ||
            (window.matchMedia && window.matchMedia("(display-mode: standalone)").matches);

        if (kurulu) {
            return;
        }

        pwaSeridiniAc("Garajım'ı ana ekranına ekle: Paylaş → Ana Ekrana Ekle.", false);
    }

    function bindKurulumIpucu() {
        el("pwa-kapat").addEventListener("click", pwaSeridiniKapat);

        el("pwa-yukle").addEventListener("click", function () {
            if (!state.pwaIstemi) {
                return;
            }

            state.pwaIstemi.prompt();
            state.pwaIstemi = null;
            el("pwa-serit").classList.add("hidden");
        });

        window.addEventListener("beforeinstallprompt", function (olay) {
            olay.preventDefault();

            if (ipucuErtelendiMi()) {
                return;
            }

            state.pwaIstemi = olay;
            pwaSeridiniAc("Garajım'ı cihazına uygulama olarak kurabilirsin.", true);
        });

        window.addEventListener("appinstalled", function () {
            el("pwa-serit").classList.add("hidden");
            state.pwaIstemi = null;
        });

        iosSeridiniDegerlendir();
    }

    function profiliYukle() {
        return api("/api/Account/profil").then(function (sonuc) {
            var veri = (sonuc && sonuc.data) || {};

            el("profil-ad").value = veri.fullName || "";
            el("profil-eposta").value = veri.email || "";
            el("profil-bildirim-evrak").checked = veri.bildirimEvrak !== false;
            el("profil-bildirim-hatirlatma").checked = veri.bildirimHatirlatma !== false;
        }).catch(function () { });
    }

    function profiliKaydet(event) {
        event.preventDefault();

        var acKilit = formuKilitle(event.target);

        api("/api/Account/profil", {
            method: "PUT",
            body: {
                fullName: el("profil-ad").value,
                bildirimEvrak: el("profil-bildirim-evrak").checked,
                bildirimHatirlatma: el("profil-bildirim-hatirlatma").checked
            }
        }).then(function (sonuc) {
            showMessage(el("profil-mesaj"), (sonuc && sonuc.message) || "Profil güncellendi.", true);

            if (state.user) {
                state.user.fullName = el("profil-ad").value.trim();
                saveSession(state.token, state.user);
                enterAppEtiketiTazele();
            }
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (hata) {
            handleError(el("profil-mesaj"), hata);
        });
    }

    function enterAppEtiketiTazele() {
        el("user-label").textContent = ustSeritEtiketi(state.user || {});
    }

    function epostaKoduIste(event) {
        event.preventDefault();

        var acKilit = formuKilitle(event.target);

        api("/api/Account/eposta-degistir-kod", {
            method: "POST",
            body: { yeniEposta: el("eposta-yeni").value }
        }).then(function (sonuc) {
            showMessage(el("eposta-mesaj"), (sonuc && sonuc.message) || "Kod gönderildi.", true);
            el("eposta-degistir-form").classList.remove("hidden");
            el("eposta-kod").focus();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (hata) {
            handleError(el("eposta-mesaj"), hata);
        });
    }

    function epostayiDegistir(event) {
        event.preventDefault();

        var acKilit = formuKilitle(event.target);

        api("/api/Account/eposta-degistir", {
            method: "POST",
            body: { kod: el("eposta-kod").value }
        }).then(function (sonuc) {
            showMessage(el("eposta-mesaj"), (sonuc && sonuc.message) || "E-posta değiştirildi.", true);
            el("eposta-degistir-form").classList.add("hidden");
            el("eposta-yeni").value = "";
            el("eposta-kod").value = "";
            profiliYukle();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (hata) {
            handleError(el("eposta-mesaj"), hata);
        });
    }

    function bindProfil() {
        el("profil-form").addEventListener("submit", profiliKaydet);
        el("eposta-kod-form").addEventListener("submit", epostaKoduIste);
        el("eposta-degistir-form").addEventListener("submit", epostayiDegistir);
    }

    function bindGeriBildirim() {
        fillSelect(el("geri-bildirim-tur"), GERI_BILDIRIM_TURLERI);

        el("geri-bildirim-btn").addEventListener("click", function () {
            var kutu = el("geri-bildirim-box");
            var acilacak = kutu.classList.contains("hidden");

            kutu.classList.toggle("hidden", !acilacak);

            if (acilacak) {
                showMessage(el("geri-bildirim-mesaji"), "");
                el("geri-bildirim-mesaj").focus();
            }
        });

        el("geri-bildirim-kapat").addEventListener("click", function () {
            el("geri-bildirim-box").classList.add("hidden");
        });

        el("geri-bildirim-form").addEventListener("submit", geriBildirimGonder);
    }

    function ornekAracOlustur() {
        clearMessages();

        return api("/api/Vehicles/ornek", { method: "POST" }).then(function (sonuc) {
            showMessage(el("app-message"), (sonuc && sonuc.message) || "Örnek araç oluşturuldu.", true);
            el("ayarlar-box").classList.add("hidden");
            state.selectedVehicleId = sonuc && sonuc.data ? sonuc.data.id : state.selectedVehicleId;
            loadVehicles();
        }).catch(function (hata) {
            handleError(el("app-message"), hata);
        });
    }

    function ornekAraciSil() {
        clearMessages();

        return api("/api/Vehicles/ornek", { method: "DELETE" }).then(function (sonuc) {
            showMessage(el("app-message"), (sonuc && sonuc.message) || "Örnek araç kaldırıldı.", true);
            state.selectedVehicleId = null;
            loadVehicles();
        }).catch(function (hata) {
            handleError(el("app-message"), hata);
        });
    }

    function ornekDugmeleriniBagla() {
        ["kurulum-ornek", "empty-ornek", "ornek-ekle"].forEach(function (id) {
            var dugme = el(id);
            if (dugme) {
                dugme.addEventListener("click", ornekAracOlustur);
            }
        });

        el("ornek-sil").addEventListener("click", ornekAraciSil);
    }

    function turHedefi(sira) {
        var adim = TUR_ADIMLARI[sira];
        if (!adim) {
            return null;
        }

        var hedef = document.querySelector(adim[0]);
        if (!hedef || hedef.classList.contains("hidden") || hedef.offsetParent === null) {
            return null;
        }

        return hedef;
    }

    function turSonrakiGorunur(sira, yon) {
        var i = sira;
        while (i >= 0 && i < TUR_ADIMLARI.length) {
            if (turHedefi(i)) {
                return i;
            }
            i += yon;
        }
        return -1;
    }

    function turAdimiCiz() {
        var sira = state.turSirasi;
        var adim = TUR_ADIMLARI[sira];
        var hedef = turHedefi(sira);

        if (!adim || !hedef) {
            turKapat(true);
            return;
        }

        var kutu = el("tur-kutu");
        var isik = el("tur-isik");

        if (typeof hedef.scrollIntoView === "function") {
            hedef.scrollIntoView({ block: "nearest", inline: "nearest" });
        }

        var alan = hedef.getBoundingClientRect();

        el("tur-baslik").textContent = adim[1];
        el("tur-metin").textContent = adim[2];
        el("tur-sayac").textContent = (sira + 1) + " / " + TUR_ADIMLARI.length;
        el("tur-geri").disabled = turSonrakiGorunur(sira - 1, -1) < 0;
        el("tur-ileri").textContent = turSonrakiGorunur(sira + 1, 1) < 0 ? "Bitir" : "İleri";

        isik.style.top = (alan.top - 6) + "px";
        isik.style.left = (alan.left - 6) + "px";
        isik.style.width = (alan.width + 12) + "px";
        isik.style.height = (alan.height + 12) + "px";

        var genislik = Math.min(320, window.innerWidth - 24);
        kutu.style.width = genislik + "px";

        var sol = Math.min(Math.max(12, alan.left), window.innerWidth - genislik - 12);
        var altBosluk = window.innerHeight - alan.bottom;
        var yukseklik = kutu.offsetHeight || 160;
        var ust = altBosluk > yukseklik + 24 ? alan.bottom + 12 : Math.max(12, alan.top - yukseklik - 12);

        kutu.style.left = sol + "px";
        kutu.style.top = ust + "px";
    }

    function turKlavye(olay) {
        if (olay.key === "Escape") {
            olay.preventDefault();
            turKapat(true);
            return;
        }

        if (olay.key === "ArrowRight") {
            olay.preventDefault();
            turIlerle(1);
            return;
        }

        if (olay.key === "ArrowLeft") {
            olay.preventDefault();
            turIlerle(-1);
        }
    }

    function turIlerle(yon) {
        var sonraki = turSonrakiGorunur(state.turSirasi + yon, yon);

        if (sonraki < 0) {
            if (yon > 0) {
                turKapat(true);
            }
            return;
        }

        state.turSirasi = sonraki;
        turAdimiCiz();
    }

    function turBaslat() {
        var ilk = turSonrakiGorunur(0, 1);
        if (ilk < 0) {
            return;
        }

        state.turSirasi = ilk;
        el("tur-katman").classList.remove("hidden");
        document.addEventListener("keydown", turKlavye);
        window.addEventListener("resize", turAdimiCiz);
        turAdimiCiz();
        el("tur-ileri").focus();
    }

    function turKapat(isaretle) {
        if (el("tur-katman").classList.contains("hidden")) {
            return;
        }

        el("tur-katman").classList.add("hidden");
        document.removeEventListener("keydown", turKlavye);
        window.removeEventListener("resize", turAdimiCiz);

        if (isaretle) {
            api("/api/Kurulum/tur-tamam", { method: "POST" }).catch(function () { });
        }
    }

    function turBagla() {
        el("tur-ileri").addEventListener("click", function () { turIlerle(1); });
        el("tur-geri").addEventListener("click", function () { turIlerle(-1); });
        el("tur-kapat").addEventListener("click", function () { turKapat(true); });
        el("tur-katman").addEventListener("click", function (olay) {
            if (olay.target === el("tur-katman")) {
                turKapat(true);
            }
        });
        el("tur-tekrar").addEventListener("click", function () {
            el("ayarlar-box").classList.add("hidden");
            turBaslat();
        });
    }

    function kurulumAdimiAc(anahtar) {
        if (anahtar === "aracVar") {
            aracFormunuAc(null);
            return;
        }

        if (!state.selectedVehicleId) {
            showMessage(el("app-message"), "Önce bir araç ekleyin.");
            return;
        }

        selectTab(anahtar === "evrakVar" ? "evrak" : "yakit");
    }

    function kurulumDurumunuYukle() {
        var cubuk = el("kurulum-cubugu");
        if (!cubuk) {
            return Promise.resolve();
        }

        if (!canManage()) {
            cubuk.classList.add("hidden");
            return Promise.resolve();
        }

        return api("/api/Kurulum").then(function (sonuc) {
            var durum = (sonuc && sonuc.data) || null;
            kurulumCubuguCiz(durum);

            if (durum && !durum.turTamamlandi && !state.turGosterildi) {
                state.turGosterildi = true;
                window.setTimeout(turBaslat, 400);
            }
        }).catch(function () {
            cubuk.classList.add("hidden");
        });
    }

    function kurulumCubuguCiz(durum) {
        var cubuk = el("kurulum-cubugu");
        var liste = el("kurulum-adimlar");

        if (!durum || durum.gizlendi) {
            cubuk.classList.add("hidden");
            return;
        }

        var tamam = durum.yuzde >= 100;

        el("kurulum-ornek").classList.toggle("hidden", durum.aracVar);

        el("kurulum-baslik").textContent = tamam
            ? "Kurulum tamam"
            : "Kurulumu tamamla — %" + durum.yuzde;
        el("kurulum-gizle").textContent = tamam ? "Kapat" : "Gizle";
        el("kurulum-dolgu").style.width = durum.yuzde + "%";

        clear(liste);

        KURULUM_ADIMLARI.forEach(function (adim, sira) {
            var bitti = durum[adim[0]] === true;
            var li = document.createElement("li");
            li.className = bitti ? "kurulum-adim bitti" : "kurulum-adim";

            li.appendChild(make("span", bitti ? "✓" : String(sira + 1), "kurulum-no"));
            li.appendChild(make("span", adim[1], "kurulum-metin"));

            if (!bitti) {
                var dugme = make("button", adim[2], "ghost compact");
                dugme.type = "button";
                dugme.addEventListener("click", function () { kurulumAdimiAc(adim[0]); });
                li.appendChild(dugme);
            }

            liste.appendChild(li);
        });

        cubuk.classList.remove("hidden");
    }

    function kurulumuGizle() {
        el("kurulum-cubugu").classList.add("hidden");
        api("/api/Kurulum/gizle", { method: "POST" }).catch(function () { });
    }

    function activeTab() {
        var button = document.querySelector(".tab-btn.active");
        return button ? button.getAttribute("data-tab") : "bakim";
    }

    function loadActiveTab() {
        if (!state.selectedVehicleId) {
            return;
        }
        var tab = activeTab();
        if (tab === "bakim") {
            loadMaintenance();
        } else if (tab === "yakit") {
            yakitAlanlariniAyarla();
            loadFuel();
        } else if (tab === "masraf") {
            loadExpenses();
        } else if (tab === "hatirlatma") {
            loadReminders();
        } else if (tab === "rapor") {
            loadSummary();
            loadFuelStats();
            loadMonthly();
            loadMaliyet();
            loadFiloMaliyet();
        } else if (tab === "zimmet") {
            loadAssignments();
        } else if (tab === "yolculuk") {
            loadYolculuk();
        } else if (tab === "lastik") {
            loadLastik();
        } else if (tab === "hasar") {
            loadHasar();
        } else if (tab === "deger") {
            loadDeger();
        } else if (tab === "usta") {
            loadUsta();
        } else if (tab === "parca") {
            loadPartMemory();
        } else if (tab === "evrak") {
            loadEvrak();
        } else if (tab === "tahmin") {
            fiyatFormunuHazirla();
            tahminFormunuAractanDoldur();
        }
    }

    function bosDurumHedefi(secici) {
        var hedef = document.querySelector(secici);
        if (!hedef) {
            return;
        }

        if (hedef.tagName === "BUTTON") {
            hedef.click();
            return;
        }

        hedef.scrollIntoView({ block: "center" });
        hedef.focus();
    }

    function bosDurumKutusu(anahtar) {
        var tanim = BOS_DURUMLAR[anahtar];
        if (!tanim) {
            return null;
        }

        var kutu = document.createElement("div");
        kutu.className = "bos-durum";
        kutu.appendChild(make("p", tanim[0]));

        var dugme = make("button", tanim[1], "primary compact");
        dugme.type = "button";
        dugme.addEventListener("click", function () { bosDurumHedefi(tanim[2]); });
        kutu.appendChild(dugme);

        return kutu;
    }

    function bosSatir(tbody, sutun, anahtar) {
        var kutu = bosDurumKutusu(anahtar);
        if (!kutu) {
            return;
        }

        var satir = document.createElement("tr");
        satir.className = "empty-row";

        var hucre = document.createElement("td");
        hucre.colSpan = sutun;
        hucre.appendChild(kutu);

        satir.appendChild(hucre);
        tbody.appendChild(satir);
    }

    function bosKutuyaCiz(kapId, anahtar) {
        var kap = el(kapId);
        if (!kap) {
            return;
        }

        clear(kap);

        var kutu = bosDurumKutusu(anahtar);
        if (kutu) {
            kap.appendChild(kutu);
        }
    }

    function emptyRow(tbody, columns, text) {
        var row = document.createElement("tr");
        row.className = "empty-row";
        var cell = make("td", text);
        cell.colSpan = columns;
        row.appendChild(cell);
        tbody.appendChild(row);
    }

    function deleteButton(onClick) {
        var cell = document.createElement("td");
        var button = make("button", "Sil", "link-btn");
        button.type = "button";
        button.addEventListener("click", onClick);
        cell.appendChild(button);
        return cell;
    }

    var LISTE_TARIH_ARALIKLARI = [
        { anahtar: "tum", etiket: "Tümü", gun: 0 },
        { anahtar: "30", etiket: "Son 30 gün", gun: 30 },
        { anahtar: "90", etiket: "Son 90 gün", gun: 90 },
        { anahtar: "365", etiket: "Son 1 yıl", gun: 365 }
    ];

    function listeDurumuOku(anahtar) {
        try {
            var ham = sessionStorage.getItem("garajim-liste-" + anahtar);
            if (ham) {
                return JSON.parse(ham);
            }
        } catch (hata) {
            return {};
        }
        return {};
    }

    function listeDurumuYaz(anahtar, durum) {
        try {
            sessionStorage.setItem("garajim-liste-" + anahtar, JSON.stringify(durum));
        } catch (hata) {
            return;
        }
    }

    function darEkran() {
        return typeof window.matchMedia === "function" && window.matchMedia("(max-width: 767px)").matches;
    }

    function listeDenetimi(ayar) {
        var kaydedilen = listeDurumuOku(ayar.anahtar);
        var durum = {
            q: kaydedilen.q || "",
            alan: kaydedilen.alan || ayar.varsayilanAlan,
            artan: kaydedilen.artan === undefined ? ayar.artanVarsayilan === true : kaydedilen.artan === true,
            aralik: kaydedilen.aralik || "tum",
            sayfa: 1,
            boyut: ayar.boyut || 25
        };

        var zamanlayici = null;
        var toplam = 0;
        var yukleniyor = false;
        var kap = null;
        var sayacMetni = null;
        var aramaKutusu = null;
        var sayfaCubugu = null;

        function tarihAraligi() {
            var secili = null;

            LISTE_TARIH_ARALIKLARI.forEach(function (aralik) {
                if (aralik.anahtar === durum.aralik) {
                    secili = aralik;
                }
            });

            if (!secili || secili.gun === 0) {
                return "";
            }

            var bas = new Date();
            bas.setDate(bas.getDate() - secili.gun);

            return "&baslangic=" + bas.toISOString().slice(0, 10);
        }

        function sorgu() {
            var parcalar = "sayfa=" + durum.sayfa + "&boyut=" + durum.boyut;

            if (durum.q) {
                parcalar += "&q=" + encodeURIComponent(durum.q);
            }

            if (durum.alan) {
                parcalar += "&sirala=" + encodeURIComponent(durum.alan + ":" + (durum.artan ? "asc" : "desc"));
            }

            return parcalar + tarihAraligi();
        }

        function durumuKaydet() {
            listeDurumuYaz(ayar.anahtar, { q: durum.q, alan: durum.alan, artan: durum.artan, aralik: durum.aralik });
        }

        function aramaAlani() {
            var sarmal = make("div", null, "liste-arama");
            var kimlik = ayar.anahtar + "-liste-arama";
            var etiket = make("label", ayar.aramaEtiketi || "Ara");

            etiket.setAttribute("for", kimlik);

            aramaKutusu = document.createElement("input");
            aramaKutusu.id = kimlik;
            aramaKutusu.type = "search";
            aramaKutusu.value = durum.q;
            aramaKutusu.placeholder = ayar.aramaIpucu || "Ara";

            aramaKutusu.addEventListener("input", function () {
                if (zamanlayici) {
                    clearTimeout(zamanlayici);
                }

                zamanlayici = setTimeout(function () {
                    durum.q = aramaKutusu.value.trim();
                    durum.sayfa = 1;
                    durumuKaydet();
                    yukle();
                }, 300);
            });

            sarmal.appendChild(etiket);
            sarmal.appendChild(aramaKutusu);

            return sarmal;
        }

        function aralikDugmeleri() {
            var sarmal = make("div", null, "liste-aralik");

            sarmal.setAttribute("role", "group");
            sarmal.setAttribute("aria-label", "Tarih aralığı");

            LISTE_TARIH_ARALIKLARI.forEach(function (aralik) {
                var dugme = make("button", aralik.etiket, "chip");

                dugme.type = "button";
                dugme.setAttribute("aria-pressed", aralik.anahtar === durum.aralik ? "true" : "false");

                dugme.addEventListener("click", function () {
                    durum.aralik = aralik.anahtar;
                    durum.sayfa = 1;
                    durumuKaydet();
                    cubuguTazele();
                    yukle();
                });

                sarmal.appendChild(dugme);
            });

            return sarmal;
        }

        function cubuguTazele() {
            if (!kap) {
                return;
            }

            var dugmeler = kap.querySelectorAll(".liste-aralik button");

            for (var i = 0; i < dugmeler.length; i++) {
                dugmeler[i].setAttribute("aria-pressed", LISTE_TARIH_ARALIKLARI[i].anahtar === durum.aralik ? "true" : "false");
            }

            basliklariTazele();
        }

        function basliklariTazele() {
            (ayar.siralamalar || []).forEach(function (sutun) {
                var baslik = el(sutun.baslikId);

                if (!baslik) {
                    return;
                }

                baslik.setAttribute("aria-sort", sutun.alan !== durum.alan
                    ? "none"
                    : (durum.artan ? "ascending" : "descending"));
            });
        }

        function basliklariBagla() {
            (ayar.siralamalar || []).forEach(function (sutun) {
                var baslik = el(sutun.baslikId);

                if (!baslik || baslik.dataset.siralamaBagli === "1") {
                    return;
                }

                baslik.dataset.siralamaBagli = "1";
                baslik.classList.add("siralanabilir");

                var dugme = make("button", baslik.textContent, "liste-baslik");
                dugme.type = "button";

                dugme.addEventListener("click", function () {
                    durum.artan = durum.alan === sutun.alan ? !durum.artan : true;
                    durum.alan = sutun.alan;
                    durum.sayfa = 1;
                    durumuKaydet();
                    basliklariTazele();
                    yukle();
                });

                clear(baslik);
                baslik.appendChild(dugme);
            });

            basliklariTazele();
        }

        function sayfaCubuguCiz() {
            if (!sayfaCubugu) {
                return;
            }

            clear(sayfaCubugu);

            var sonSayfa = Math.max(1, Math.ceil(toplam / durum.boyut));

            if (sonSayfa <= 1) {
                return;
            }

            if (darEkran()) {
                if (durum.sayfa >= sonSayfa) {
                    return;
                }

                var dahaFazla = make("button", "Daha fazla yükle", "ghost compact");
                dahaFazla.type = "button";

                dahaFazla.addEventListener("click", function () {
                    durum.sayfa += 1;
                    yukle(true);
                });

                sayfaCubugu.appendChild(dahaFazla);
                return;
            }

            var onceki = make("button", "Önceki", "ghost compact");
            onceki.type = "button";
            onceki.disabled = durum.sayfa <= 1;
            onceki.addEventListener("click", function () {
                durum.sayfa -= 1;
                yukle();
            });

            var bilgi = make("span", "Sayfa " + durum.sayfa + " / " + sonSayfa, "liste-sayfa-bilgi");

            var sonraki = make("button", "Sonraki", "ghost compact");
            sonraki.type = "button";
            sonraki.disabled = durum.sayfa >= sonSayfa;
            sonraki.addEventListener("click", function () {
                durum.sayfa += 1;
                yukle();
            });

            sayfaCubugu.appendChild(onceki);
            sayfaCubugu.appendChild(bilgi);
            sayfaCubugu.appendChild(sonraki);
        }

        function sayaciTazele() {
            if (!sayacMetni) {
                return;
            }

            sayacMetni.textContent = toplam === 0 ? "Kayıt yok" : "Toplam " + toplam + " kayıt";
        }

        function kur() {
            kap = el(ayar.cubukId);

            if (!kap) {
                return;
            }

            if (kap.dataset.kuruldu === "1") {
                basliklariBagla();
                return;
            }

            kap.dataset.kuruldu = "1";

            if (ayar.aramaKapali !== true) {
                kap.appendChild(aramaAlani());
            }

            if (ayar.tarihSuzgeci !== false) {
                kap.appendChild(aralikDugmeleri());
            }

            sayacMetni = make("span", "", "liste-sayac");
            sayacMetni.setAttribute("aria-live", "polite");
            kap.appendChild(sayacMetni);

            sayfaCubugu = make("div", null, "liste-sayfalar");
            kap.appendChild(sayfaCubugu);

            basliklariBagla();
        }

        function yukle(ekle) {
            if (yukleniyor) {
                return Promise.resolve();
            }

            yukleniyor = true;

            var govde = el(ayar.govdeId);
            var adres = ayar.uc();

            return api(adres + (adres.indexOf("?") >= 0 ? "&" : "?") + sorgu()).then(function (sonuc) {
                var veri = (sonuc && sonuc.data) || {};
                var kayitlar = veri.kayitlar || [];

                toplam = veri.toplam || 0;

                if (!ekle) {
                    clear(govde);
                }

                if (kayitlar.length === 0 && !ekle) {
                    if (durum.q || durum.aralik !== "tum") {
                        var satir = document.createElement("tr");
                        var hucre = make("td", "Sonuç bulunamadı. Aramayı ya da tarih aralığını değiştirin.");

                        hucre.colSpan = ayar.sutunSayisi;
                        satir.className = "empty-row";
                        satir.appendChild(hucre);
                        govde.appendChild(satir);
                    } else if (ayar.bosAnahtar) {
                        bosSatir(govde, ayar.sutunSayisi, ayar.bosAnahtar);
                    } else {
                        emptyRow(govde, ayar.sutunSayisi, ayar.bosMetin || "Kayıt yok.");
                    }
                }

                kayitlar.forEach(function (kayit) {
                    govde.appendChild(ayar.satir(kayit));
                });

                sayaciTazele();
                sayfaCubuguCiz();
            }).catch(function (hata) {
                handleError(el("app-message"), hata);
            }).finally(function () {
                yukleniyor = false;
                if (typeof acKilit === "function") { acKilit(); }
            });
        }

        function tazele() {
            durum.sayfa = 1;
            return yukle();
        }

        return { kur: kur, yukle: yukle, tazele: tazele };
    }

    function listeDenetimiKur(denetim) {
        denetim.kur();
        return denetim.tazele();
    }

    var bakimDenetimi = listeDenetimi({
        anahtar: "bakim",
        cubukId: "maintenance-liste-araclar",
        govdeId: "maintenance-rows",
        bosAnahtar: "bakim",
        sutunSayisi: 8,
        varsayilanAlan: "tarih",
        aramaEtiketi: "Bakımlarda ara",
        aramaIpucu: "Servis, not ya da parça",
        siralamalar: [
            { baslikId: "maintenance-bas-tarih", alan: "tarih" },
            { baslikId: "maintenance-bas-km", alan: "km" },
            { baslikId: "maintenance-bas-tutar", alan: "tutar" },
            { baslikId: "maintenance-bas-servis", alan: "servis" }
        ],
        uc: function () { return "/api/Maintenance?vehicleId=" + state.selectedVehicleId; },
        satir: function (item) {
            var tr = document.createElement("tr");
            tr.appendChild(make("td", formatDate(item.date)));
            tr.appendChild(make("td", labelOf(MAINTENANCE_TYPES, item.type)));
            tr.appendChild(make("td", km(item.km)));
            tr.appendChild(make("td", money(item.cost)));
            tr.appendChild(make("td", item.serviceName || "-"));
            tr.appendChild(documentButton(item.id));
            tr.appendChild(duzenleButonu(function () { bakimiDuzenle(item); }));
            tr.appendChild(deleteButton(function () { removeRecord("/api/Maintenance/" + item.id, loadMaintenance); }));
            return tr;
        }
    });

    function loadMaintenance() {
        return listeDenetimiKur(bakimDenetimi);
    }

    function duzenleButonu(islev) {
        var hucre = document.createElement("td");
        var dugme = make("button", "Düzenle", "link-btn");
        dugme.type = "button";
        dugme.addEventListener("click", islev);
        hucre.appendChild(dugme);
        return hucre;
    }

    function bakimiDuzenle(kayit) {
        state.duzenlenenBakimId = kayit.id;

        el("maintenance-type").value = kayit.type;
        el("maintenance-date").value = String(kayit.date).slice(0, 10);
        el("maintenance-km").value = kayit.km;
        el("maintenance-cost").value = kayit.cost;
        el("maintenance-service").value = kayit.serviceName || "";
        el("maintenance-note").value = kayit.note || "";

        var kutu = el("maintenance-parts");
        clear(kutu);
        (kayit.parcalar || []).forEach(function (parca) { addPartRow(kutu, parca); });

        bakimFormModu();
    }

    function bakimFormModu() {
        var duzenleme = state.duzenlenenBakimId !== null;

        el("maintenance-submit").textContent = duzenleme ? "Bakımı güncelle" : "Bakım ekle";
        el("maintenance-vazgec").classList.toggle("hidden", !duzenleme);
    }

    function bakimFormunuSifirla() {
        state.duzenlenenBakimId = null;
        el("maintenance-form").reset();
        clear(el("maintenance-parts"));
        el("maintenance-date").value = todayInput();
        bakimFormModu();
    }

    function documentButton(recordId) {
        var cell = document.createElement("td");
        var button = make("button", "Belgeler", "link-btn");
        button.type = "button";
        button.addEventListener("click", function () { openDocuments(recordId); });
        cell.appendChild(button);
        return cell;
    }

    function openDocuments(recordId) {
        state.documentRecordId = recordId;
        el("document-box").classList.remove("hidden");
        el("document-title").textContent = "Bakım kaydı belgeleri";
        el("document-form").reset();
        loadDocuments();
    }

    function closeDocuments() {
        state.documentRecordId = null;
        el("document-box").classList.add("hidden");
    }

    var onizlemeSirasi = [];
    var onizlemeIndeks = 0;

    function onizlenebilirMi(item) {
        var tip = (item.contentType || "").toLowerCase();

        return tip.indexOf("image/") === 0 || tip === "application/pdf";
    }

    function onizlemeAc(item, liste) {
        onizlemeSirasi = (liste || []).filter(onizlenebilirMi);
        onizlemeIndeks = 0;

        onizlemeSirasi.forEach(function (kayit, sira) {
            if (kayit.id === item.id) {
                onizlemeIndeks = sira;
            }
        });

        if (onizlemeSirasi.length === 0) {
            onizlemeSirasi = [item];
            onizlemeIndeks = 0;
        }

        el("onizleme-modal").classList.remove("hidden");
        document.addEventListener("keydown", onizlemeKlavye);

        onizlemeGoster();
    }

    function onizlemeKlavye(olay) {
        if (olay.key === "Escape") {
            onizlemeModaliKapat();
            return;
        }

        if (olay.key === "ArrowLeft") {
            onizlemeKaydir(-1);
        }

        if (olay.key === "ArrowRight") {
            onizlemeKaydir(1);
        }
    }

    function onizlemeKaydir(yon) {
        if (onizlemeSirasi.length < 2) {
            return;
        }

        onizlemeIndeks = (onizlemeIndeks + yon + onizlemeSirasi.length) % onizlemeSirasi.length;
        onizlemeGoster();
    }

    function onizlemeGoster() {
        var item = onizlemeSirasi[onizlemeIndeks];
        var govde = el("onizleme-govde");

        clear(govde);

        el("onizleme-bilgi").textContent = item.originalName + " · " + fileSize(item.sizeBytes)
            + (onizlemeSirasi.length > 1 ? " · " + (onizlemeIndeks + 1) + "/" + onizlemeSirasi.length : "");

        el("onizleme-onceki").classList.toggle("hidden", onizlemeSirasi.length < 2);
        el("onizleme-sonraki").classList.toggle("hidden", onizlemeSirasi.length < 2);

        var tip = (item.contentType || "").toLowerCase();

        if (tip === "application/pdf") {
            govde.appendChild(make("p", "PDF belgeler tarayıcı içinde açılmaz; indirerek görüntüleyin.", "hint"));

            var indir = make("button", "İndir", "primary compact");
            indir.type = "button";
            indir.addEventListener("click", function () { downloadDocument(item); });
            govde.appendChild(indir);

            return;
        }

        govde.appendChild(make("p", "Yükleniyor…", "hint"));

        var headers = state.token ? { Authorization: "Bearer " + state.token } : {};

        fetch("/api/Documents/" + item.id + "/onizleme", { headers: headers }).then(function (cevap) {
            if (!cevap.ok) {
                throw new Error("Önizleme açılamadı.");
            }

            return cevap.blob();
        }).then(function (blob) {
            return new Promise(function (coz, reddet) {
                var okuyucu = new FileReader();

                okuyucu.onload = function () { coz(okuyucu.result); };
                okuyucu.onerror = function () { reddet(new Error("Önizleme açılamadı.")); };
                okuyucu.readAsDataURL(blob);
            });
        }).then(function (adres) {
            if (onizlemeSirasi[onizlemeIndeks].id !== item.id) {
                return;
            }

            clear(govde);

            var gorsel = document.createElement("img");
            gorsel.src = adres;
            gorsel.alt = item.originalName;

            govde.appendChild(gorsel);
        }).catch(function (hata) {
            clear(govde);
            govde.appendChild(make("p", (hata && hata.message) || "Önizleme açılamadı.", "hint"));
        });
    }

    function onizlemeModaliKapat() {
        el("onizleme-modal").classList.add("hidden");
        clear(el("onizleme-govde"));
        document.removeEventListener("keydown", onizlemeKlavye);
    }

    function belgeyiKaydaBagla(belgeId, kayitId) {
        return api("/api/Documents/" + belgeId + "/bagla", {
            method: "PUT",
            body: { maintenanceRecordId: kayitId }
        }).then(function (sonuc) {
            showMessage(el("app-message"), (sonuc && sonuc.message) || "Belge kayda bağlandı.", true);
            loadDocuments();
        }).catch(function (hata) {
            handleError(el("app-message"), hata);
        });
    }

    function bagsizAracBelgeleriniSor() {
        if (!state.documentRecordId || !state.selectedVehicleId) {
            return Promise.resolve();
        }

        return api("/api/Documents?vehicleId=" + state.selectedVehicleId).then(function (sonuc) {
            var bagsiz = ((sonuc && sonuc.data) || []).filter(function (belge) {
                return !belge.maintenanceRecordId;
            });

            if (bagsiz.length === 0) {
                showMessage(el("app-message"), "Bu araçta bağlanmamış belge yok.");
                return null;
            }

            var secenekler = bagsiz.map(function (belge) {
                return [String(belge.id), belge.originalName + " · " + fileSize(belge.sizeBytes)];
            });

            return girdiSor("Belgeyi kayda bağla", "Araca yüklenmiş belgelerden birini bu bakım kaydına bağlayın.", {
                secenekler: secenekler
            });
        }).then(function (secilen) {
            if (!secilen) {
                return null;
            }

            return belgeyiKaydaBagla(Number(secilen), state.documentRecordId);
        }).catch(function (hata) {
            handleError(el("app-message"), hata);
        });
    }

    function bindOnizleme() {
        var kapat = el("onizleme-kapat");
        if (!kapat) {
            return;
        }

        kapat.addEventListener("click", onizlemeModaliKapat);
        el("document-bagla").addEventListener("click", bagsizAracBelgeleriniSor);
        el("onizleme-onceki").addEventListener("click", function () { onizlemeKaydir(-1); });
        el("onizleme-sonraki").addEventListener("click", function () { onizlemeKaydir(1); });
    }

    function loadDocuments() {
        var list = el("document-list");
        if (!state.documentRecordId) {
            clear(list);
            return;
        }
        api("/api/Documents?maintenanceRecordId=" + state.documentRecordId).then(function (result) {
            var rows = (result && result.data) || [];
            clear(list);
            if (rows.length === 0) {
                list.appendChild(make("li", "Bu kayda bağlı belge yok."));
                return;
            }
            rows.forEach(function (item) {
                var li = document.createElement("li");
                li.appendChild(make("span", item.originalName + " · " + fileSize(item.sizeBytes)));

                var actions = make("span", "", "row-actions");

                if (onizlenebilirMi(item)) {
                    var onizle = make("button", "Önizle", "link-btn");
                    onizle.type = "button";
                    onizle.addEventListener("click", function () { onizlemeAc(item, rows); });
                    actions.appendChild(onizle);
                }

                var download = make("button", "İndir", "link-btn");
                download.type = "button";
                download.addEventListener("click", function () { downloadDocument(item); });
                actions.appendChild(download);

                var remove = make("button", "Sil", "link-btn");
                remove.type = "button";
                remove.addEventListener("click", function () { removeDocument(item.id); });
                actions.appendChild(remove);

                li.appendChild(actions);
                list.appendChild(li);
            });
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function fileSize(bytes) {
        var value = Number(bytes) || 0;
        if (value < 1024) {
            return value + " B";
        }
        if (value < 1024 * 1024) {
            return Math.round(value / 1024) + " KB";
        }
        return (value / (1024 * 1024)).toFixed(1) + " MB";
    }

    function downloadDocument(item) {
        var headers = state.token ? { Authorization: "Bearer " + state.token } : {};
        fetch("/api/Documents/" + item.id + "/download", { headers: headers }).then(function (response) {
            if (!response.ok) {
                throw new Error("Belge indirilemedi.");
            }
            return response.blob();
        }).then(function (blob) {
            var url = URL.createObjectURL(blob);
            var link = document.createElement("a");
            link.href = url;
            link.download = item.originalName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function removeDocument(id) {
        api("/api/Documents/" + id, { method: "DELETE" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Belge silindi.", true);
            loadDocuments();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function loadTeam() {
        loadBelgeler();
        var tbody = el("team-rows");
        api("/api/Team").then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);
            if (rows.length === 0) {
                bosSatir(tbody, 5, "ekip");
                return;
            }
            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", item.fullName));
                tr.appendChild(make("td", item.email));

                var roleCell = document.createElement("td");
                if (!isOwner() || (state.user && item.email === state.user.email)) {
                    roleCell.textContent = labelOf(TEAM_ROLES, item.role);
                } else {
                    var select = document.createElement("select");
                    fillSelect(select, TEAM_ROLES);
                    select.value = item.role;
                    select.addEventListener("change", function () { changeRole(item.id, select.value); });
                    roleCell.appendChild(select);
                }
                tr.appendChild(roleCell);
                tr.appendChild(make("td", item.isActive ? "Aktif" : "Pasif"));

                var actionCell = document.createElement("td");
                var kendisi = state.user && item.email === state.user.email;
                if (isOwner() && item.isActive && !kendisi) {
                    var button = make("button", "Pasifleştir", "link-btn");
                    button.type = "button";
                    button.addEventListener("click", function () { deactivateMember(item.id); });
                    actionCell.appendChild(button);
                }
                tr.appendChild(actionCell);
                tbody.appendChild(tr);
            });
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function changeRole(id, role) {
        api("/api/Team/" + id + "/role", { method: "PUT", body: { role: role } }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Rol güncellendi.", true);
            loadTeam();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            loadTeam();
        });
    }

    function deactivateMember(id) {
        api("/api/Team/" + id + "/deactivate", { method: "PUT" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Üye pasifleştirildi.", true);
            loadTeam();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function loadAssignments() {
        if (!state.selectedVehicleId) {
            return;
        }
        var tbody = el("assignment-rows");
        api("/api/Assignments?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);

            var aktif = null;
            rows.forEach(function (item) {
                if (item.isActive) {
                    aktif = item;
                }
            });
            el("assignment-current").textContent = aktif
                ? "Şu an zimmetli: " + aktif.userFullName + " (" + formatDate(aktif.startDate) + " tarihinden beri)"
                : "Bu araç şu an kimseye zimmetli değil.";
            el("assignment-submit").textContent = aktif ? "Devret" : "Zimmetle";
            el("assignment-end").classList.toggle("hidden", !aktif);

            if (rows.length === 0) {
                emptyRow(tbody, 3, "Zimmet geçmişi yok.");
            } else {
                rows.forEach(function (item) {
                    var tr = document.createElement("tr");
                    tr.appendChild(make("td", item.userFullName));
                    tr.appendChild(make("td", formatDate(item.startDate)));
                    tr.appendChild(make("td", item.endDate ? formatDate(item.endDate) : "Devam ediyor"));
                    tbody.appendChild(tr);
                });
            }

            return loadAssignableUsers();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function loadAssignableUsers() {
        return api("/api/Team").then(function (result) {
            var rows = (result && result.data) || [];
            var select = el("assignment-user");
            clear(select);
            rows.filter(function (item) {
                return item.isActive;
            }).forEach(function (item) {
                var option = document.createElement("option");
                option.value = String(item.id);
                option.textContent = item.fullName + " (" + labelOf(TEAM_ROLES, item.role) + ")";
                select.appendChild(option);
            });
        });
    }

    var yakitDenetimi = listeDenetimi({
        anahtar: "yakit",
        cubukId: "fuel-liste-araclar",
        govdeId: "fuel-rows",
        bosAnahtar: "yakit",
        sutunSayisi: 6,
        varsayilanAlan: "tarih",
        aramaKapali: true,
        siralamalar: [
            { baslikId: "fuel-bas-tarih", alan: "tarih" },
            { baslikId: "fuel-bas-km", alan: "km" },
            { baslikId: "fuel-bas-litre", alan: "litre" },
            { baslikId: "fuel-bas-tutar", alan: "tutar" }
        ],
        uc: function () { return "/api/Fuel?vehicleId=" + state.selectedVehicleId; },
        satir: function (item) {
            var tr = document.createElement("tr");
            var tarihHucre = make("td", formatDate(item.date));
            if (item.supheliKm) {
                var rozet = make("span", "şüpheli", "rozet-supheli");
                var supheliAciklama = "Bu aralıkta hesaplanan tüketim beklenen sınırların dışında; ortalamaya katılmıyor.";
                rozet.title = supheliAciklama;
                rozet.setAttribute("aria-label", supheliAciklama);
                rozet.tabIndex = 0;
                tarihHucre.appendChild(rozet);
            }
            if (!item.tamDolum) {
                tarihHucre.appendChild(make("span", "kısmi", "rozet-kismi"));
            }
            tr.appendChild(tarihHucre);
            tr.appendChild(make("td", km(item.km)));
            tr.appendChild(make("td", Number(item.liters) > 0 ? literFormat.format(Number(item.liters)) + " L" : "-"));
            tr.appendChild(make("td", item.kwh === null ? "-" : literFormat.format(Number(item.kwh)) + " kWh" + (item.sarjTuru ? " (" + labelOf(SARJ_TURU, item.sarjTuru) + ")" : "")));
            tr.appendChild(make("td", money(item.totalCost)));
            tr.appendChild(yakitIslemHucresi(item));
            return tr;
        }
    });

    function yakitIslemHucresi(item) {
        var hucre = make("td", "", "row-actions");

        var duzenle = make("button", "Düzenle", "link-btn");
        duzenle.type = "button";
        duzenle.addEventListener("click", function () { yakitiDuzenle(item); });
        hucre.appendChild(duzenle);

        var sil = make("button", "Sil", "link-btn");
        sil.type = "button";
        sil.addEventListener("click", function () { removeRecord("/api/Fuel/" + item.id, loadFuel); });
        hucre.appendChild(sil);

        return hucre;
    }

    function yakitiDuzenle(kayit) {
        state.duzenlenenYakitId = kayit.id;

        el("fuel-date").value = String(kayit.date).slice(0, 10);
        el("fuel-km").value = kayit.km;
        el("fuel-liters").value = Number(kayit.liters) > 0 ? kayit.liters : "";
        el("fuel-kwh").value = kayit.kwh === null || kayit.kwh === undefined ? "" : kayit.kwh;
        el("fuel-sarj").value = kayit.sarjTuru || "";
        el("fuel-cost").value = kayit.totalCost;
        el("fuel-tam-dolum").checked = kayit.tamDolum !== false;

        yakitFormModu();
        el("fuel-form").scrollIntoView({ block: "start" });
    }

    function yakitFormModu() {
        var duzenleme = state.duzenlenenYakitId !== null;

        el("fuel-submit").textContent = duzenleme ? "Yakıtı güncelle" : "Yakıt ekle";
        el("fuel-vazgec").classList.toggle("hidden", !duzenleme);
    }

    function yakitFormunuSifirla() {
        state.duzenlenenYakitId = null;
        el("fuel-form").reset();
        el("fuel-date").value = todayInput();
        yakitFormModu();
    }

    function masrafIslemHucresi(item) {
        var hucre = make("td", "", "row-actions");

        var duzenle = make("button", "Düzenle", "link-btn");
        duzenle.type = "button";
        duzenle.addEventListener("click", function () { masrafiDuzenle(item); });
        hucre.appendChild(duzenle);

        var sil = make("button", "Sil", "link-btn");
        sil.type = "button";
        sil.addEventListener("click", function () { removeRecord("/api/Expenses/" + item.id, loadExpenses); });
        hucre.appendChild(sil);

        return hucre;
    }

    function masrafiDuzenle(kayit) {
        state.duzenlenenMasrafId = kayit.id;

        el("expense-category").value = kayit.category;
        el("expense-date").value = String(kayit.date).slice(0, 10);
        el("expense-amount").value = kayit.amount;
        el("expense-note").value = kayit.note || "";

        masrafFormModu();
        el("expense-form").scrollIntoView({ block: "start" });
    }

    function masrafFormModu() {
        var duzenleme = state.duzenlenenMasrafId !== null;

        el("expense-submit").textContent = duzenleme ? "Masrafı güncelle" : "Masraf ekle";
        el("expense-vazgec").classList.toggle("hidden", !duzenleme);
    }

    function masrafFormunuSifirla() {
        state.duzenlenenMasrafId = null;
        el("expense-form").reset();
        el("expense-date").value = todayInput();
        masrafFormModu();
    }

    function loadFuel() {
        return listeDenetimiKur(yakitDenetimi);
    }

    var masrafDenetimi = listeDenetimi({
        anahtar: "masraf",
        cubukId: "expense-liste-araclar",
        govdeId: "expense-rows",
        bosAnahtar: "masraf",
        sutunSayisi: 5,
        varsayilanAlan: "tarih",
        aramaEtiketi: "Masraflarda ara",
        aramaIpucu: "Not",
        siralamalar: [
            { baslikId: "expense-bas-tarih", alan: "tarih" },
            { baslikId: "expense-bas-kategori", alan: "kategori" },
            { baslikId: "expense-bas-tutar", alan: "tutar" }
        ],
        uc: function () { return "/api/Expenses?vehicleId=" + state.selectedVehicleId; },
        satir: function (item) {
            var tr = document.createElement("tr");
            tr.appendChild(make("td", formatDate(item.date)));
            tr.appendChild(make("td", labelOf(EXPENSE_CATEGORIES, item.category)));
            tr.appendChild(make("td", money(item.amount)));
            tr.appendChild(make("td", item.note || "-"));
            tr.appendChild(masrafIslemHucresi(item));
            return tr;
        }
    });

    function loadExpenses() {
        return listeDenetimiKur(masrafDenetimi);
    }

    function tekrarMetni(kayit) {
        var parcalar = [];

        if (kayit.tekrarAy) {
            parcalar.push(kayit.tekrarAy + " ayda bir");
        }

        if (kayit.tekrarKm) {
            parcalar.push(km(kayit.tekrarKm) + "'de bir");
        }

        return parcalar.length === 0 ? "-" : parcalar.join(" · ");
    }

    var hatirlatmaDenetimi = listeDenetimi({
        anahtar: "hatirlatma",
        cubukId: "reminder-liste-araclar",
        govdeId: "reminder-rows",
        bosMetin: "Hatırlatma yok.",
        sutunSayisi: 6,
        varsayilanAlan: "tarih",
        artanVarsayilan: true,
        tarihSuzgeci: false,
        aramaEtiketi: "Hatırlatmalarda ara",
        aramaIpucu: "Not",
        siralamalar: [
            { baslikId: "reminder-bas-tarih", alan: "tarih" },
            { baslikId: "reminder-bas-km", alan: "km" },
            { baslikId: "reminder-bas-durum", alan: "durum" }
        ],
        uc: function () { return "/api/Reminders?vehicleId=" + state.selectedVehicleId; },
        satir: function (item) {
            var tr = document.createElement("tr");
            tr.appendChild(make("td", labelOf(REMINDER_TYPES, item.type)));
            tr.appendChild(make("td", item.dueDate ? formatDate(item.dueDate) : "-"));
            tr.appendChild(make("td", item.dueKm ? km(item.dueKm) : "-"));
            tr.appendChild(make("td", tekrarMetni(item)));

            var statusCell = document.createElement("td");
            statusCell.appendChild(make("span", item.isCompleted ? "Tamamlandı" : "Bekliyor", item.isCompleted ? "badge done" : "badge"));
            tr.appendChild(statusCell);

            var actionCell = document.createElement("td");
            if (!item.isCompleted) {
                var completeButton = make("button", "Tamamla", "link-btn");
                completeButton.type = "button";
                completeButton.addEventListener("click", function () { completeReminder(item.id); });
                actionCell.appendChild(completeButton);
            }
            var deleteLink = make("button", "Sil", "link-btn");
            deleteLink.type = "button";
            deleteLink.addEventListener("click", function () { removeRecord("/api/Reminders/" + item.id, loadReminders); });
            actionCell.appendChild(deleteLink);
            tr.appendChild(actionCell);

            return tr;
        }
    });

    function loadReminders() {
        var sonuc = listeDenetimiKur(hatirlatmaDenetimi);
        loadUpcoming();
        return sonuc;
    }

    function loadUpcoming() {
        var list = el("upcoming-list");
        api("/api/Reminders/upcoming?days=30").then(function (result) {
            var rows = (result && result.data) || [];
            clear(list);
            if (rows.length === 0) {
                list.appendChild(make("li", "Önümüzdeki 30 günde hatırlatma yok."));
                return;
            }
            rows.forEach(function (item) {
                var li = document.createElement("li");
                var left = document.createElement("div");
                left.appendChild(make("div", labelOf(REMINDER_TYPES, item.type) + " - " + item.plate));
                left.appendChild(make("div", formatDate(item.dueDate), "muted"));
                li.appendChild(left);

                var button = make("button", "Tamamla", "ghost");
                button.type = "button";
                button.addEventListener("click", function () { completeReminder(item.id); });
                li.appendChild(button);

                list.appendChild(li);
            });
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function completeReminder(id) {
        api("/api/Reminders/" + id + "/complete", { method: "PUT" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Hatırlatma tamamlandı.", true);
            loadReminders();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function removeRecord(path, reload) {
        if (!window.confirm("Bu kayıt kalıcı olarak silinecek. Devam edilsin mi?")) {
            return;
        }

        api(path, { method: "DELETE" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Kayıt silindi.", true);
            reload();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function card(label, value, accent) {
        var box = make("div", null, accent ? "card accent" : "card");
        box.appendChild(make("span", label, "card-label"));
        box.appendChild(make("span", value, "card-value"));
        return box;
    }

    function loadSummary() {
        var start = el("report-start").value;
        var end = el("report-end").value;
        var cards = el("summary-cards");
        var tbody = el("category-rows");

        return api("/api/Reports/summary?vehicleId=" + state.selectedVehicleId + "&start=" + start + "&end=" + end).then(function (result) {
            var data = (result && result.data) || {};
            clear(cards);
            cards.appendChild(card("Yakıt", money(data.totalFuel)));
            cards.appendChild(card("Bakım", money(data.totalMaintenance)));
            cards.appendChild(card("Diğer masraf", money(data.totalOtherExpense)));
            cards.appendChild(card("Genel toplam", money(data.grandTotal), true));

            clear(tbody);
            var categories = data.categories || [];
            if (categories.length === 0) {
                emptyRow(tbody, 2, "Bu aralıkta masraf yok.");
                return;
            }
            categories.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", labelOf(EXPENSE_CATEGORIES, item.category)));
                tr.appendChild(make("td", money(item.total)));
                tbody.appendChild(tr);
            });
        });
    }

    function loadFuelStats() {
        var cards = el("fuel-cards");
        return api("/api/Reports/fuel-stats?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var data = (result && result.data) || {};
            clear(cards);

            if (data.elektrikli) {
                cards.appendChild(card("Ortalama tüketim",
                    data.averageKwhPer100Km === null || data.averageKwhPer100Km === undefined
                        ? "—"
                        : literFormat.format(Number(data.averageKwhPer100Km)) + " kWh/100km", true));
            } else {
                cards.appendChild(card("Ortalama tüketim",
                    literFormat.format(Number(data.averageConsumptionPer100Km)) + " L/100km", true));
            }

            cards.appendChild(card("Km başına maliyet", money(data.costPerKm), true));
            cards.appendChild(card("Ölçülen mesafe", km(data.totalKm)));

            cards.appendChild(data.elektrikli
                ? card("Ölçülen şarj",
                    data.totalKwh === null || data.totalKwh === undefined
                        ? "—"
                        : literFormat.format(Number(data.totalKwh)) + " kWh")
                : card("Ölçülen yakıt", literFormat.format(Number(data.totalLiters)) + " L"));

            cards.appendChild(card("Ölçülen tutar", money(data.totalCost)));
        }).catch(function (error) {
            clear(cards);
            cards.appendChild(card("Yakıt istatistiği", error && error.message ? error.message : "Hesaplanamadı."));
        });
    }

    function loadMonthly() {
        return api("/api/Reports/monthly?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            grafikKitapligi().then(function () { drawChart(rows); }).catch(function () { });
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    var grafikSozu = null;

    function grafikKitapligi() {
        if (window.Chart) {
            return Promise.resolve(window.Chart);
        }

        if (grafikSozu) {
            return grafikSozu;
        }

        grafikSozu = new Promise(function (cozumle, reddet) {
            var etiket = document.createElement("script");
            etiket.src = "https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js";
            etiket.defer = true;
            etiket.addEventListener("load", function () { cozumle(window.Chart); });
            etiket.addEventListener("error", function () {
                grafikSozu = null;
                reddet(new Error("Grafik kitaplığı yüklenemedi."));
            });

            document.head.appendChild(etiket);
        });

        return grafikSozu;
    }

    function drawChart(rows) {

        var fallback = el("chart-fallback");
        var canvas = el("monthly-chart");

        if (typeof Chart === "undefined") {
            fallback.textContent = "Grafik kitaplığı yüklenemedi (CDN erişimi yok). Aylık toplamlar tabloda görünmeye devam eder.";
            return;
        }

        fallback.textContent = rows.length === 0 ? "Grafik için henüz veri yok." : "";

        var labels = rows.map(function (item) {
            return String(item.month).padStart(2, "0") + "." + item.year;
        });
        var values = rows.map(function (item) { return Number(item.total); });

        if (state.chart) {
            state.chart.destroy();
        }

        state.chart = new Chart(canvas, {
            type: "bar",
            data: {
                labels: labels,
                datasets: [{
                    label: "Aylık toplam (TL)",
                    data: values,
                    backgroundColor: "rgba(255, 122, 26, 0.65)",
                    borderColor: "#ff7a1a",
                    borderWidth: 1,
                    borderRadius: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { labels: { color: "#99a1ad" } }
                },
                scales: {
                    x: { ticks: { color: "#99a1ad" }, grid: { color: "#262b33" } },
                    y: { ticks: { color: "#99a1ad" }, grid: { color: "#262b33" } }
                }
            }
        });
    }

    function raporAraligi() {
        return "baslangic=" + el("report-start").value + "&bitis=" + el("report-end").value;
    }

    function loadMaliyet() {
        var cards = el("maliyet-cards");
        return api("/api/Vehicles/" + state.selectedVehicleId + "/maliyet?" + raporAraligi()).then(function (result) {
            var data = (result && result.data) || {};

            clear(cards);
            cards.appendChild(card("Toplam maliyet", money(data.toplamMaliyet), true));
            cards.appendChild(card("Km başına maliyet", data.maliyetKmBasi === null ? "—" : money(data.maliyetKmBasi), true));
            cards.appendChild(card("Ortalama tüketim", data.litre100Km === null ? "—" : literFormat.format(Number(data.litre100Km)) + " L/100km"));
            if (data.kwh100Km !== null || Number(data.toplamKwh) > 0) {
                cards.appendChild(card("Şarj tüketimi", data.kwh100Km === null ? "—" : literFormat.format(Number(data.kwh100Km)) + " kWh/100km"));
                cards.appendChild(card("Toplam şarj", literFormat.format(Number(data.toplamKwh)) + " kWh"));
            }
            cards.appendChild(card("Mesafe", km(data.mesafeKm)));
            cards.appendChild(card("Yakıt", money(data.toplamYakit)));
            cards.appendChild(card("Bakım", money(data.toplamBakim)));
            cards.appendChild(card("Masraf", money(data.toplamMasraf)));
            if (data.sahiplikMaliyeti !== null && data.sahiplikMaliyeti !== undefined) {
                cards.appendChild(card("Değer kaybı", money(data.donemDegerKaybi)));
                cards.appendChild(card("Sahiplik maliyeti", money(data.sahiplikMaliyeti), true));
            }

            grafikKitapligi().then(function () {
                drawMaliyetChart(data.aylikSeri || []);
                drawTuketimChart(data.tuketimSeri || []);
            }).catch(function () { });
        }).catch(function (error) {
            clear(cards);
            cards.appendChild(card("Maliyet", error && error.message ? error.message : "Hesaplanamadı."));
        });
    }

    function loadFiloMaliyet() {
        var tbody = el("filo-rows");
        return api("/api/Reports/filo-maliyet?" + raporAraligi()).then(function (result) {
            var araclar = (result && result.data && result.data.araclar) || [];
            clear(tbody);

            if (araclar.length === 0) {
                emptyRow(tbody, 5, "Bu aralıkta filo verisi yok.");
                return;
            }

            araclar.forEach(function (satir) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", satir.plaka + " - " + satir.marka + " " + satir.model));
                tr.appendChild(make("td", money(satir.toplamMaliyet)));
                tr.appendChild(make("td", km(satir.mesafeKm)));
                tr.appendChild(make("td", satir.maliyetKmBasi === null ? "—" : money(satir.maliyetKmBasi)));
                tr.appendChild(make("td", satir.litre100Km === null ? "—" : literFormat.format(Number(satir.litre100Km))));
                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            clear(tbody);
            emptyRow(tbody, 5, error && error.message ? error.message : "Filo karşılaştırması alınamadı.");
        });
    }

    function ayEtiketi(kalem) {
        return String(kalem.ay).padStart(2, "0") + "." + kalem.yil;
    }

    function grafikSecenekleri(yiginli) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { labels: { color: "#99a1ad" } } },
            scales: {
                x: { stacked: yiginli, ticks: { color: "#99a1ad" }, grid: { color: "#262b33" } },
                y: { stacked: yiginli, ticks: { color: "#99a1ad" }, grid: { color: "#262b33" } }
            }
        };
    }

    function drawMaliyetChart(seri) {
        var fallback = el("maliyet-fallback");

        if (typeof Chart === "undefined") {
            fallback.textContent = "Grafik kitaplığı yüklenemedi (CDN erişimi yok). Maliyet kartları görünmeye devam eder.";
            return;
        }

        var dolu = seri.some(function (kalem) { return Number(kalem.toplam) > 0; });
        fallback.textContent = dolu ? "" : "Grafik için henüz veri yok.";

        if (state.maliyetChart) {
            state.maliyetChart.destroy();
        }

        state.maliyetChart = new Chart(el("maliyet-chart"), {
            type: "bar",
            data: {
                labels: seri.map(ayEtiketi),
                datasets: [
                    { label: "Yakıt", data: seri.map(function (k) { return Number(k.yakit); }), backgroundColor: "rgba(255, 122, 26, 0.75)" },
                    { label: "Bakım", data: seri.map(function (k) { return Number(k.bakim); }), backgroundColor: "rgba(90, 160, 255, 0.75)" },
                    { label: "Masraf", data: seri.map(function (k) { return Number(k.masraf); }), backgroundColor: "rgba(140, 200, 140, 0.75)" }
                ]
            },
            options: grafikSecenekleri(true)
        });
    }

    function drawTuketimChart(seri) {
        var fallback = el("tuketim-fallback");

        if (typeof Chart === "undefined") {
            fallback.textContent = "";
            return;
        }

        fallback.textContent = seri.length === 0 ? "Tüketim grafiği için en az iki yakıt kaydı gerekir." : "";

        if (state.tuketimChart) {
            state.tuketimChart.destroy();
        }

        state.tuketimChart = new Chart(el("tuketim-chart"), {
            type: "line",
            data: {
                labels: seri.map(ayEtiketi),
                datasets: [{
                    label: "Tüketim (L/100km)",
                    data: seri.map(function (k) { return Number(k.litre100Km); }),
                    borderColor: "#ff7a1a",
                    backgroundColor: "rgba(255, 122, 26, 0.25)",
                    tension: 0.25,
                    fill: true
                }]
            },
            options: grafikSecenekleri(false)
        });
    }

    var YOLCULUK_AMAC = [
        ["Is", "İş"],
        ["Ozel", "Özel"]
    ];

    var BELGE_DURUM = {
        Gecti: "Süresi geçti",
        Yaklasiyor: "Yaklaşıyor",
        Iyi: "İyi"
    };

    function yolculukAraligi() {
        var bas = el("report-start").value;
        var son = el("report-end").value;
        var parcalar = [];
        if (bas) {
            parcalar.push("baslangic=" + bas);
        }
        if (son) {
            parcalar.push("bitis=" + son);
        }
        return parcalar.join("&");
    }

    function yolculukSatiri(kayit) {
        var tr = document.createElement("tr");
        tr.appendChild(make("td", formatDate(kayit.tarih)));
        tr.appendChild(make("td", labelOf(YOLCULUK_AMAC, kayit.amac)));
        tr.appendChild(make("td", km(kayit.baslangicKm)));
        tr.appendChild(make("td", km(kayit.bitisKm)));
        tr.appendChild(make("td", km(kayit.mesafeKm)));

        var guzergah = [kayit.nereden, kayit.nereye].filter(Boolean).join(" → ");
        tr.appendChild(make("td", guzergah || "-"));
        tr.appendChild(make("td", kayit.surucuAdi || "-"));

        tr.appendChild(duzenleButonu(function () { yolculugoDuzenle(kayit); }));
        tr.appendChild(deleteButton(function () { yolculukSil(kayit.id); }));

        return tr;
    }

    function renderYolculukRows(kayitlar) {
        var tbody = el("yolculuk-rows");
        clear(tbody);

        if (kayitlar.length === 0) {
            bosSatir(tbody, 9, "yolculuk");
            return;
        }

        kayitlar.forEach(function (kayit) {
            tbody.appendChild(yolculukSatiri(kayit));
        });
    }

    var yolculukDenetimi = listeDenetimi({
        anahtar: "yolculuk",
        cubukId: "yolculuk-liste-araclar",
        govdeId: "yolculuk-rows",
        bosAnahtar: "yolculuk",
        sutunSayisi: 9,
        varsayilanAlan: "tarih",
        tarihSuzgeci: false,
        aramaEtiketi: "Yolculuklarda ara",
        aramaIpucu: "Nereden, nereye ya da not",
        siralamalar: [
            { baslikId: "yolculuk-bas-tarih", alan: "tarih" },
            { baslikId: "yolculuk-bas-amac", alan: "amac" },
            { baslikId: "yolculuk-bas-mesafe", alan: "mesafe" }
        ],
        uc: function () {
            var yol = "/api/Yolculuk?vehicleId=" + state.selectedVehicleId;
            var aralik = yolculukAraligi();
            return aralik ? yol + "&" + aralik : yol;
        },
        satir: yolculukSatiri
    });

    function yolculugoDuzenle(kayit) {
        state.duzenlenenYolculukId = kayit.id;

        el("yolculuk-tarih").value = String(kayit.tarih).slice(0, 10);
        el("yolculuk-bas-km").value = kayit.baslangicKm;
        el("yolculuk-bitis-km").value = kayit.bitisKm;
        el("yolculuk-amac").value = kayit.amac;
        el("yolculuk-nereden").value = kayit.nereden || "";
        el("yolculuk-nereye").value = kayit.nereye || "";
        el("yolculuk-not").value = kayit.not || "";

        yolculukFormModu();
    }

    function yolculukFormModu() {
        var duzenleme = state.duzenlenenYolculukId !== null;

        el("yolculuk-submit").textContent = duzenleme ? "Yolculuğu güncelle" : "Yolculuk ekle";
        el("yolculuk-vazgec").classList.toggle("hidden", !duzenleme);
    }

    function yolculukFormunuSifirla() {
        state.duzenlenenYolculukId = null;
        el("yolculuk-form").reset();
        el("yolculuk-tarih").value = todayInput();
        yolculukFormModu();
    }

    function loadYolculuk() {
        if (!state.selectedVehicleId) {
            clear(el("yolculuk-rows"));
            clear(el("yolculuk-cards"));
            return Promise.resolve();
        }

        var sorgu = "vehicleId=" + state.selectedVehicleId;
        var aralik = yolculukAraligi();
        if (aralik) {
            sorgu += "&" + aralik;
        }

        return listeDenetimiKur(yolculukDenetimi).then(function () {
            return api("/api/Yolculuk/ozet?" + sorgu);
        }).then(function (result) {
            var ozet = (result && result.data) || {};
            var cards = el("yolculuk-cards");
            clear(cards);
            cards.appendChild(card("Toplam mesafe", km(ozet.toplamKm), true));
            cards.appendChild(card("İş", km(ozet.isKm)));
            cards.appendChild(card("Özel", km(ozet.ozelKm)));
            cards.appendChild(card("İş oranı", yuzde(ozet.isOrani, 1), true));
            cards.appendChild(card("Yolculuk", String(ozet.yolculukSayisi || 0)));
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function yolculukSil(id) {
        clearMessages();
        api("/api/Yolculuk/" + id, { method: "DELETE" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Kayıt silindi.", true);
            loadYolculuk();
            loadVehicles();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function bindYolculuk() {
        el("yolculuk-vazgec").addEventListener("click", yolculukFormunuSifirla);

        el("yolculuk-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();

            if (!state.selectedVehicleId) {
                showMessage(el("app-message"), "Önce bir araç seçin.", false);
                return;
            }

            var duzenleme = state.duzenlenenYolculukId !== null;

            api(duzenleme ? "/api/Yolculuk/" + state.duzenlenenYolculukId : "/api/Yolculuk", {
                method: duzenleme ? "PUT" : "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    tarih: el("yolculuk-tarih").value,
                    baslangicKm: Number(el("yolculuk-bas-km").value),
                    bitisKm: Number(el("yolculuk-bitis-km").value),
                    amac: el("yolculuk-amac").value,
                    nereden: el("yolculuk-nereden").value,
                    nereye: el("yolculuk-nereye").value,
                    not: el("yolculuk-not").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Yolculuk eklendi.", true);
                yolculukFormunuSifirla();
                loadYolculuk();
                loadVehicles();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });
    }

    function renderBelgeRows(uyeler) {
        var tbody = el("belge-rows");
        clear(tbody);

        if (uyeler.length === 0) {
            emptyRow(tbody, 4, "Ekip üyesi yok.");
            return;
        }

        uyeler.forEach(function (uye) {
            var tr = document.createElement("tr");
            tr.appendChild(make("td", uye.adSoyad));
            tr.appendChild(make("td", labelOf(TEAM_ROLES, uye.rol)));
            tr.appendChild(make("td", BELGE_DURUM[uye.enKotuDurum] || uye.enKotuDurum, "durum-" + uye.enKotuDurum.toLowerCase()));

            var belgeler = (uye.belgeler || []).map(function (belge) {
                return belge.evrakAdi + " (" + formatDate(belge.bitisTarihi) + ")";
            });
            tr.appendChild(make("td", belgeler.length === 0 ? "-" : belgeler.join(", ")));

            tbody.appendChild(tr);
        });
    }

    function loadBelgeler() {
        return api("/api/Team/belgeler").then(function (result) {
            renderBelgeRows((result && result.data) || []);
        }).catch(function () {
            clear(el("belge-rows"));
            emptyRow(el("belge-rows"), 4, "Ekip belgeleri görüntülenemedi.");
        });
    }

    function exportIndir(tur) {
        clearMessages();

        var parcalar = [];
        if (!el("export-tum-araclar").checked && state.selectedVehicleId) {
            parcalar.push("vehicleId=" + state.selectedVehicleId);
        }
        var aralik = yolculukAraligi();
        if (aralik) {
            parcalar.push(aralik);
        }

        var yol = "/api/Export/" + tur + ".csv" + (parcalar.length > 0 ? "?" + parcalar.join("&") : "");

        fetch(yol, { headers: { Authorization: "Bearer " + state.token } }).then(function (response) {
            if (!response.ok) {
                throw new Error("Dosya indirilemedi.");
            }
            return response.blob();
        }).then(function (blob) {
            var url = URL.createObjectURL(blob);
            var link = document.createElement("a");
            link.href = url;
            link.download = "garajim-" + tur + ".csv";
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
            showMessage(el("app-message"), "CSV indirildi.", true);
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function bindExport() {
        ["yakit", "bakim", "masraf", "evrak"].forEach(function (tur) {
            el("export-" + tur).addEventListener("click", function () {
                exportIndir(tur);
            });
        });
    }

    var LASTIK_MEVSIM = [
        ["Yaz", "Yaz"],
        ["Kis", "Kış"],
        ["DortMevsim", "Dört mevsim"]
    ];

    var SARJ_TURU = [
        ["", "-"],
        ["Ev", "Ev"],
        ["Isyeri", "İş yeri"],
        ["HizliSarj", "Hızlı şarj"]
    ];

    function arsivPaneliniAc() {
        el("arsiv-box").classList.remove("hidden");
        arsiviYukle();
    }

    function arsiviYukle() {
        var tbody = el("arsiv-tbody");
        clear(tbody);

        return api("/api/Vehicles?arsiv=true").then(function (result) {
            var rows = (result && result.data) || [];

            if (rows.length === 0) {
                emptyRow(tbody, 5, "Arşivde araç yok.");
                return;
            }

            rows.forEach(function (arac) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", arac.plate));
                tr.appendChild(make("td", arac.brand + " " + arac.model));
                tr.appendChild(make("td", ARSIV_NEDENLERI[arac.arsivNedeni] || arac.arsivNedeni || "-"));
                tr.appendChild(make("td", formatDate(arac.arsivTarihi)));

                var islem = document.createElement("td");

                var geri = make("button", "Arşivden çıkar", "ghost");
                geri.type = "button";
                geri.addEventListener("click", function () {
                    api("/api/Vehicles/" + arac.id + "/arsivden-al", { method: "POST" })
                        .then(function () { arsiviYukle(); loadVehicles(); })
                        .catch(function (error) { handleError(el("arsiv-mesaj"), error); });
                });
                islem.appendChild(geri);

                var sil = make("button", "Kalıcı sil", "ghost");
                sil.type = "button";
                sil.addEventListener("click", function () { araciKaliciSil(arac); });
                islem.appendChild(sil);

                tr.appendChild(islem);
                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            clear(tbody);
            emptyRow(tbody, 5, "Arşiv okunamadı.");
            handleError(el("arsiv-mesaj"), error);
        });
    }

    function girdiSor(baslik, aciklama, alan) {
        return new Promise(function (cozumle) {
            var katman = el("girdi-modal");
            var form = el("girdi-form");
            var kutu = el("girdi-deger");
            var secim = el("girdi-secim");

            el("girdi-baslik").textContent = baslik;
            el("girdi-aciklama").textContent = aciklama || "";

            var secenekliMi = !!(alan && alan.secenekler);

            secim.classList.toggle("hidden", !secenekliMi);
            kutu.classList.toggle("hidden", secenekliMi);

            if (secenekliMi) {
                fillSelect(secim, alan.secenekler);
                secim.value = alan.varsayilan || alan.secenekler[0][0];
            } else {
                kutu.type = "text";
                kutu.inputMode = alan && alan.sayisal ? "decimal" : "text";
                kutu.value = (alan && alan.varsayilan) || "";
                kutu.placeholder = (alan && alan.ipucu) || "";
            }

            katman.classList.remove("hidden");
            document.body.classList.add("kaza-modal-acik");
            (secenekliMi ? secim : kutu).focus();

            function kapat(sonuc) {
                katman.classList.add("hidden");
                document.body.classList.remove("kaza-modal-acik");
                form.removeEventListener("submit", gonder);
                el("girdi-vazgec").removeEventListener("click", vazgec);
                document.removeEventListener("keydown", klavye);
                cozumle(sonuc);
            }

            function gonder(olay) {
                olay.preventDefault();
                kapat(secenekliMi ? secim.value : kutu.value);
            }

            function vazgec() {
                kapat(null);
            }

            function klavye(olay) {
                if (olay.key === "Escape") {
                    olay.preventDefault();
                    kapat(null);
                }
            }

            form.addEventListener("submit", gonder);
            el("girdi-vazgec").addEventListener("click", vazgec);
            document.addEventListener("keydown", klavye);
        });
    }

    function araciKaliciSil(arac) {

        girdiSor("Aracı kalıcı sil",
            "Bu araç ve tüm kayıtları kalıcı olarak silinecek. Onaylamak için plakayı yazın: " + arac.plate,
            { ipucu: arac.plate }).then(function (yazilan) {

        if (yazilan === null) {
            return;
        }

        if (yazilan.trim().toUpperCase() !== String(arac.plate).toUpperCase()) {
            showMessage(el("arsiv-mesaj"), "Plaka eşleşmedi, araç silinmedi.", false);
            return;
        }

        api("/api/Vehicles/" + arac.id, { method: "DELETE" }).then(function () {
            showMessage(el("arsiv-mesaj"), "Araç silindi.", true);
            arsiviYukle();
            loadVehicles();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("arsiv-mesaj"), error);
        });

        });
    }

    function arsivSecenegiIleArsivle() {
        var arac = duzenlenenArac();
        if (!arac) {
            return;
        }

        girdiSor(arac.plate + " arşive alınacak", "Arşivleme nedenini seçin.", {
            secenekler: [["Satildi", "Satıldı"], ["Hurda", "Hurda"], ["Diger", "Diğer"]],
            varsayilan: "Satildi"
        }).then(function (secilen) {
            if (secilen === null) {
                return;
            }

            api("/api/Vehicles/" + arac.id + "/arsiv", { method: "POST", body: { neden: secilen } })
                .then(function () {
                    showMessage(el("app-message"), arac.plate + " arşive alındı.", true);
                    loadVehicles();
                })
                .catch(function (error) { handleError(el("app-message"), error); });
        });
    }

    var KM_BAYATLIK_GUNU = 60;

    function tescilUyarisiniGuncelle() {
        var uyari = el("evrak-tescil-uyari");
        var arac = seciliArac();
        var tur = el("evrak-tur").value;

        var gerekli = arac && tur === "Muayene"
            && (arac.kullanimTuru || "Hususi") === "Hususi"
            && !arac.ilkTescilTarihi;

        uyari.classList.toggle("hidden", !gerekli);

        if (gerekli) {
            uyari.textContent = "Muayene tarihi önerilebilmesi için aracın ilk tescil tarihi gerekiyor. "
                + "Düzenle ekranından girin ya da bitiş tarihini kendiniz yazın.";
        }
    }

    function kmSeridiniGuncelle() {
        var serit = el("km-serit");
        var arac = seciliArac();

        if (!arac || !canManage() || !arac.sonKmGuncelleme) {
            serit.classList.add("hidden");
            return;
        }

        var gun = Math.floor((Date.now() - new Date(arac.sonKmGuncelleme).getTime()) / 86400000);

        if (gun < KM_BAYATLIK_GUNU) {
            serit.classList.add("hidden");
            return;
        }

        el("km-serit-metin").textContent =
            arac.plate + " için kilometre " + gun + " gündür güncellenmedi.";
        el("km-hizli").value = arac.currentKm;
        serit.classList.remove("hidden");
    }

    function hizliKmKaydet() {
        var arac = seciliArac();
        var yeni = Number(el("km-hizli").value);

        if (!arac || !isFinite(yeni) || yeni < arac.currentKm) {
            showMessage(el("app-message"), "Kilometre mevcut değerden küçük olamaz; düşürmek için araç düzenlemeyi kullanın.", false);
            return;
        }

        api("/api/Vehicles/" + arac.id + "/km", { method: "PUT", body: { currentKm: yeni } })
            .then(function () {
                el("km-serit").classList.add("hidden");
                loadVehicles();
            })
            .catch(function (error) { handleError(el("app-message"), error); });
    }

    var EN_ESKI_YIL = 1950;

    var KATALOG_TR_GRUBU = "Türkiye'de satılan";
    var KATALOG_DIGER_GRUBU = "Diğer";

    function katalogSayfasi(yol) {
        return api(yol).then(function (result) {
            var veri = (result && result.data) || {};

            return {
                kayitlar: veri.kayitlar || [],
                dahaVar: !!veri.dahaVar,
                toplam: veri.toplam || 0
            };
        });
    }

    function katalogSorguEki(q, sayfa) {
        var ek = "sayfa=" + (sayfa || 1);

        if (q) {
            ek += "&q=" + encodeURIComponent(q);
        }

        return ek;
    }

    function katalogMarkalari(q, sayfa) {
        return katalogSayfasi("/api/Katalog/markalar?" + katalogSorguEki(q, sayfa));
    }

    function katalogSerileri(marka, q, sayfa) {
        if (!marka) {
            return Promise.resolve({ kayitlar: [], dahaVar: false, toplam: 0 });
        }

        return katalogSayfasi("/api/Katalog/seriler?marka=" + encodeURIComponent(marka)
            + "&" + katalogSorguEki(q, sayfa));
    }

    function katalogGrubu(select, ad) {
        var cocuklar = select.children;

        for (var i = 0; i < cocuklar.length; i++) {
            if (cocuklar[i].nodeName === "OPTGROUP" && cocuklar[i].label === ad) {
                return cocuklar[i];
            }
        }

        var kutu = document.createElement("optgroup");
        kutu.label = ad;
        select.appendChild(kutu);
        return kutu;
    }

    function katalogSecenekEkle(hedef, ad) {
        var secenek = document.createElement("option");
        secenek.value = ad;
        secenek.textContent = ad;
        hedef.appendChild(secenek);
    }

    function katalogSecenekleri(select, kayitlar, bosMetin, ekle) {
        if (!ekle) {
            clear(select);

            var bos = document.createElement("option");
            bos.value = "";
            bos.textContent = bosMetin;
            select.appendChild(bos);

            select.katalogGruplu = kayitlar.some(function (kayit) { return !kayit.tr; });
        }

        kayitlar.forEach(function (kayit) {
            if (!select.katalogGruplu) {
                katalogSecenekEkle(select, kayit.ad);
                return;
            }

            katalogSecenekEkle(katalogGrubu(select, kayit.tr ? KATALOG_TR_GRUBU : KATALOG_DIGER_GRUBU), kayit.ad);
        });
    }

    function katalogSecenekVar(select, deger) {
        var secenekler = select.querySelectorAll("option");

        for (var i = 0; i < secenekler.length; i++) {
            if (secenekler[i].value === deger) {
                return true;
            }
        }

        return false;
    }

    function katalogSeciliyiKoru(select, secili) {
        if (!secili || katalogSecenekVar(select, secili)) {
            return;
        }

        var secenek = document.createElement("option");
        secenek.value = secili;
        secenek.textContent = secili;
        secenek.dataset.korunan = "1";
        select.insertBefore(secenek, select.children[1] || null);
    }

    function katalogDurumuYaz(select, sonuc, q, sayfa) {
        select.katalogSorgu = q || "";
        select.katalogSayfa = sayfa || 1;

        var daha = document.getElementById(select.id + "-daha");

        if (daha) {
            daha.classList.toggle("hidden", !sonuc.dahaVar);
        }
    }

    function katalogAramaKutusu(select) {
        return document.getElementById(select.id + "-ara");
    }

    function katalogIlkSecenegiSec(select) {
        var secenekler = select.querySelectorAll("option");
        var yedek = "";

        for (var i = 0; i < secenekler.length; i++) {
            if (!secenekler[i].value) {
                continue;
            }

            if (secenekler[i].dataset.korunan === "1") {
                if (!yedek) {
                    yedek = secenekler[i].value;
                }

                continue;
            }

            select.value = secenekler[i].value;
            select.dispatchEvent(new Event("change"));
            return;
        }

        if (yedek) {
            select.value = yedek;
            select.dispatchEvent(new Event("change"));
        }
    }

    function katalogSeciciKur(select, doldur) {
        var ara = katalogAramaKutusu(select);
        var daha = document.getElementById(select.id + "-daha");
        var zaman = null;

        function calistir(q, sayfa, ekle) {
            doldur(select.value, q, sayfa, ekle)
                .catch(function (error) { handleError(el("app-message"), error); });
        }

        if (ara) {
            ara.addEventListener("input", function () {
                if (zaman) {
                    window.clearTimeout(zaman);
                }

                zaman = window.setTimeout(function () { calistir(ara.value.trim(), 1, false); }, 250);
            });

            ara.addEventListener("keydown", function (event) {
                if (event.key === "Enter") {
                    event.preventDefault();
                    katalogIlkSecenegiSec(select);
                    select.focus();
                } else if (event.key === "ArrowDown") {
                    event.preventDefault();
                    select.focus();
                }
            });
        }

        if (daha) {
            daha.addEventListener("click", function () {
                calistir(select.katalogSorgu, (select.katalogSayfa || 1) + 1, true);
            });
        }
    }

    function yillariDoldur(select, secili) {
        var enYeni = new Date().getFullYear() + 1;
        var yillar = [];

        for (var yil = enYeni; yil >= EN_ESKI_YIL; yil--) {
            yillar.push(String(yil));
        }

        fillSimpleSelect(select, yillar);
        select.value = String(secili || new Date().getFullYear());
    }

    function markaSecenekleriniDoldur(select, secili, q, sayfa, ekle) {
        if (!ekle && q === undefined) {
            var ara = katalogAramaKutusu(select);

            if (ara) {
                ara.value = "";
            }
        }

        return katalogMarkalari(q, sayfa).then(function (sonuc) {
            katalogSecenekleri(select, sonuc.kayitlar, "Marka seçin", !!ekle);
            katalogDurumuYaz(select, sonuc, q, sayfa);
            katalogSeciliyiKoru(select, secili);
            select.value = secili && katalogSecenekVar(select, secili) ? secili : "";
            return select.value;
        });
    }

    function seriSecenekleriniDoldur(markaSelect, seriSelect, secili, q, sayfa, ekle) {
        if (!ekle && q === undefined) {
            var ara = katalogAramaKutusu(seriSelect);

            if (ara) {
                ara.value = "";
            }
        }

        return katalogSerileri(markaSelect.value, q, sayfa).then(function (sonuc) {
            var bos = markaSelect.value ? "Seri seçin" : "Önce marka seçin";

            katalogSecenekleri(seriSelect, sonuc.kayitlar, bos, !!ekle);
            katalogDurumuYaz(seriSelect, sonuc, q, sayfa);
            katalogSeciliyiKoru(seriSelect, secili);
            seriSelect.disabled = !markaSelect.value;
            seriSelect.value = secili && katalogSecenekVar(seriSelect, secili) ? secili : "";
            return seriSelect.value;
        });
    }

    function listedeYokDurumu() {
        var acik = el("vehicle-model-listede-yok").checked;

        el("vehicle-model").classList.toggle("hidden", acik);
        el("vehicle-model").required = !acik;
        el("vehicle-model-serbest").classList.toggle("hidden", !acik);
        el("vehicle-model-serbest").required = acik;
        el("vehicle-model-ipucu").classList.toggle("hidden", !acik);
    }

    var KASA_TAHMIN_ESLEME = {
        Sedan: "Sedan",
        Hatchback5: "Hatchback/5",
        Hatchback3: "Hatchback/3",
        StationWagon: "Station wagon",
        Mpv: "MPV",
        Coupe: "Coupe",
        Suv: "SUV",
        Cabrio: "Cabrio",
        Roadster: "Roadster",
        PickUp: "Pick-up"
    };

    var YAKIT_TAHMIN_ESLEME = {
        Benzin: "Benzin",
        Dizel: "Dizel",
        Lpg: "LPG & Benzin",
        Hibrit: "Hibrit",
        Elektrik: "Elektrik"
    };

    function tahminFormunuAractanDoldur() {
        var arac = seciliArac();

        if (!arac) {
            return;
        }

        markaSecenekleriniDoldur(el("price-marka"), arac.brand);
        seriSecenekleriniDoldur(el("price-marka"), el("price-seri"), arac.model);

        el("price-yil").value = arac.year || new Date().getFullYear();
        el("price-km").value = arac.currentKm || "";
        el("price-yakit").value = YAKIT_TAHMIN_ESLEME[arac.fuelType] || "Benzin";

        if (arac.vites) {
            el("price-vites").value = arac.vites;
        }

        if (arac.kasaTipi && KASA_TAHMIN_ESLEME[arac.kasaTipi]) {
            el("price-kasa").value = KASA_TAHMIN_ESLEME[arac.kasaTipi];
        }
    }

    function fiyatFormunuHazirla() {

        if (el("price-marka").options.length > 1) {
            return;
        }

        yillariDoldur(el("price-yil"), null);

        markaSecenekleriniDoldur(el("price-marka"), "").then(function () {
            return seriSecenekleriniDoldur(el("price-marka"), el("price-seri"), "");
        }).catch(function (error) { handleError(el("app-message"), error); });
    }

    function katalogUyarisiniGuncelle() {
        var serit = el("katalog-serit");
        var arac = seciliArac();

        if (!arac || !canManage() || !arac.modelEslesmedi) {
            serit.classList.add("hidden");
            return;
        }

        el("katalog-serit-metin").textContent =
            arac.plate + " için model katalogda yok; değer tahmini için listeden seçin.";
        serit.classList.remove("hidden");
    }

    function duzenlenenArac() {
        if (state.duzenlenenAracId === null) {
            return null;
        }

        return state.vehicles.filter(function (v) { return v.id === state.duzenlenenAracId; })[0] || null;
    }

    function yakitAlanlariniAyarla() {
        var arac = seciliArac();
        var tur = arac ? arac.fuelType : null;
        var elektrikli = tur === "Elektrik";
        var hibrit = tur === "Hibrit";

        el("fuel-kwh-box").classList.toggle("hidden", !(elektrikli || hibrit));
        el("fuel-sarj-box").classList.toggle("hidden", !(elektrikli || hibrit));

        var litre = el("fuel-liters");
        litre.required = !elektrikli;
        litre.disabled = elektrikli;
        if (elektrikli) {
            litre.value = "";
        }

        el("fuel-kwh").required = elektrikli;
    }

    function renderLastikRows(setler) {
        var tbody = el("lastik-rows");
        clear(tbody);

        if (setler.length === 0) {
            bosSatir(tbody, 8, "lastik");
            return;
        }

        setler.forEach(function (set) {
            var tr = document.createElement("tr");
            tr.appendChild(make("td", set.ad + (set.takili ? " (takılı)" : "")));
            tr.appendChild(make("td", labelOf(LASTIK_MEVSIM, set.mevsim)));
            tr.appendChild(make("td", [set.marka, set.ebat].filter(Boolean).join(" / ") || "-"));
            tr.appendChild(make("td", formatDate(set.takilmaTarihi) + " · " + km(set.takilmaKm)));
            tr.appendChild(make("td", set.sokulmeTarihi ? formatDate(set.sokulmeTarihi) + " · " + km(set.sokulmeKm) : "-"));
            tr.appendChild(make("td", set.takili ? "-" : km(set.toplamKm)));
            tr.appendChild(make("td", set.disDerinligiMm === null ? "-" : set.disDerinligiMm + " mm"));

            var islem = document.createElement("td");
            if (set.takili) {
                var sok = make("button", "Sök", "link-btn");
                sok.type = "button";
                sok.addEventListener("click", function () { lastikSok(set); });
                islem.appendChild(sok);
            }
            var sil = make("button", "Sil", "link-btn");
            sil.type = "button";
            sil.addEventListener("click", function () { lastikSil(set.id); });
            islem.appendChild(sil);
            tr.appendChild(islem);

            tbody.appendChild(tr);
        });
    }

    function loadLastik() {
        if (!state.selectedVehicleId) {
            clear(el("lastik-rows"));
            el("lastik-uyari").textContent = "";
            return Promise.resolve();
        }

        return api("/api/Lastik?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var durum = (result && result.data) || {};
            el("lastik-uyari").textContent = durum.uyari || (durum.kisLastigiDonemi ? "Kış lastiği dönemindesiniz." : "");
            renderLastikRows(durum.setler || []);
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function lastikSok(set) {
        var arac = seciliArac();
        var varsayilanKm = arac ? arac.currentKm : set.takilmaKm;
        girdiSor("Lastik setini sök", "Sökülme kilometresini girin.",
            { sayisal: true, varsayilan: String(varsayilanKm) }).then(function (girilen) {

            if (girilen === null) {
                return;
            }

            var kmDegeri = sayiOku(girilen);

            if (isNaN(kmDegeri)) {
                showMessage(el("app-message"), "Kilometre sayı olmalı.", false);
                return;
            }

            clearMessages();
            api("/api/Lastik/" + set.id + "/sok", {
                method: "PUT",
                body: {
                    sokulmeTarihi: todayInput(),
                    sokulmeKm: Math.round(kmDegeri)
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Set söküldü.", true);
                loadLastik();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });
    }

    function lastikSil(id) {
        clearMessages();
        api("/api/Lastik/" + id, { method: "DELETE" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Set silindi.", true);
            loadLastik();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function kazaListesiCiz(kap, baslik, maddeler) {
        if (!maddeler || maddeler.length === 0) {
            return;
        }

        var bolum = make("div", null, "kaza-liste");
        bolum.appendChild(make("h3", baslik));
        var ul = document.createElement("ul");
        maddeler.forEach(function (madde) {
            ul.appendChild(make("li", madde));
        });
        bolum.appendChild(ul);
        kap.appendChild(bolum);
    }

    function kazaRehberiCiz(rehber) {
        el("kaza-ozet").textContent = rehber.ozet || "";
        el("kaza-bildirim").textContent = rehber.bildirimSuresi || "";
        el("kaza-kaynak").textContent = rehber.kaynak || "";

        var adimlar = el("kaza-adimlar");
        adimlar.textContent = "";
        (rehber.adimlar || []).forEach(function (adim) {
            var kutu = make("div", null, "kaza-adim");
            kutu.appendChild(make("h3", adim.baslik));
            var ul = document.createElement("ul");
            (adim.maddeler || []).forEach(function (madde) {
                ul.appendChild(make("li", madde));
            });
            kutu.appendChild(ul);
            adimlar.appendChild(kutu);
        });

        var listeler = el("kaza-listeler");
        listeler.textContent = "";
        kazaListesiCiz(listeler, "Anlaşmalı tutanak koşulları", rehber.anlasmaliTutanakKosullari);
        kazaListesiCiz(listeler, "Polis çağrılması gereken haller", rehber.polisGerekliHaller);
        kazaListesiCiz(listeler, "Çekilecek fotoğraflar", rehber.fotografListesi);
        kazaListesiCiz(listeler, "Alınacak bilgiler", rehber.alinacakBilgiler);
    }

    var KAZA_KUYRUK_ANAHTARI = "garajim_hasar_kuyrugu";
    var KAZA_REHBER_ANAHTARI = "garajim_kaza_rehberi";
    var ACIL_KART_ANAHTARI = "garajim_acil_kart";
    var KAZA_SYNC_ETIKETI = "garajim-hasar-kuyruk";

    function yerelOku(anahtar) {
        try {
            var ham = localStorage.getItem(anahtar);
            return ham ? JSON.parse(ham) : null;
        } catch (error) {
            return null;
        }
    }

    function yerelYaz(anahtar, deger) {
        try {
            localStorage.setItem(anahtar, JSON.stringify(deger));
        } catch (error) {
            return;
        }
    }

    function kuyrugoOku() {
        var kuyruk = yerelOku(KAZA_KUYRUK_ANAHTARI);
        return Array.isArray(kuyruk) ? kuyruk : [];
    }

    var KAZA_KUYRUK_SINIRI = 20;

    function kuyrugaEkle(govde) {
        var kuyruk = kuyrugoOku();
        var imza = JSON.stringify(govde);

        var ayni = kuyruk.some(function (kayit) {
            return JSON.stringify(kayit.govde) === imza;
        });

        if (ayni || kuyruk.length >= KAZA_KUYRUK_SINIRI) {
            kuyrukRozetiniTazele();
            return;
        }

        kuyruk.push({ govde: govde, eklenme: new Date().toISOString() });
        yerelYaz(KAZA_KUYRUK_ANAHTARI, kuyruk);
        kuyrukRozetiniTazele();

        if ("serviceWorker" in navigator && "SyncManager" in window) {
            navigator.serviceWorker.ready.then(function (kayit) {
                return kayit.sync.register(KAZA_SYNC_ETIKETI);
            }).catch(function () { });
        }
    }

    function kuyrukRozetiniTazele() {
        var sayi = kuyrugoOku().length;
        var kutu = el("kaza-kuyruk");
        if (!kutu) {
            return;
        }
        kutu.textContent = sayi === 0 ? "" : sayi + " hasar dosyası bağlantı gelince gönderilecek.";
        kutu.classList.toggle("hidden", sayi === 0);
    }

    function kuyrugaAlVeBildir(govde) {
        kuyrugaEkle(govde);
        kuyrukRozetiniGuncelle();
        el("kaza-modal").classList.add("hidden");
        showMessage(el("app-message"), "Kaydınız kuyruğa alındı, bağlantı gelince gönderilecek.", true);
    }

    function kuyrukRozetiniGuncelle() {
        var adet = kuyrugoOku().length;
        var serit = el("kuyruk-serit");

        serit.classList.toggle("hidden", adet === 0);

        if (adet > 0) {
            el("kuyruk-serit-metin").textContent =
                adet + " hasar kaydı gönderilmeyi bekliyor; bağlantı gelince gönderilecek.";
        }
    }

    function kuyrugoBosalt() {
        var kuyruk = kuyrugoOku();
        if (kuyruk.length === 0 || !state.token) {
            return Promise.resolve(0);
        }

        var kalan = [];
        var gonderilen = 0;
        var sira = Promise.resolve();

        kuyruk.forEach(function (kayit) {
            sira = sira.then(function () {
                return api("/api/Hasar", { method: "POST", body: kayit.govde })
                    .then(function () { gonderilen++; })
                    .catch(function (error) {
                        if (error && error.durum) {
                            return;
                        }
                        kalan.push(kayit);
                    });
            });
        });

        return sira.then(function () {
            yerelYaz(KAZA_KUYRUK_ANAHTARI, kalan);
            kuyrukRozetiniTazele();

            if (gonderilen > 0) {
                showMessage(el("app-message"), gonderilen + " bekleyen hasar dosyası gönderildi.", true);
                if (activeTab() === "hasar") {
                    loadHasar();
                }
            }

            return gonderilen;
        });
    }

    function acilKartiCiz() {
        var kart = yerelOku(ACIL_KART_ANAHTARI);
        var kutu = el("kaza-acil");
        if (!kutu) {
            return;
        }

        clear(kutu);

        if (!kart) {
            kutu.classList.add("hidden");
            return;
        }

        kutu.classList.remove("hidden");
        kutu.appendChild(make("h3", "Acil kart — " + kart.plaka));

        var dl = document.createElement("dl");
        [["Araç", [kart.marka, kart.model, kart.yil].filter(Boolean).join(" ")],
         ["Acil durumda aranacak", kart.acilKisiAd || "-"],
         ["Telefon", kart.acilKisiTelefon || "-"],
         ["Not", kart.acilNot || "-"]].forEach(function (satir) {
            dl.appendChild(make("dt", satir[0]));
            dl.appendChild(make("dd", satir[1]));
        });

        kutu.appendChild(dl);
    }

    function acilKartiSakla() {
        var arac = state.vehicles.filter(function (v) { return v.id === state.selectedVehicleId; })[0];
        if (!arac) {
            return;
        }

        yerelYaz(ACIL_KART_ANAHTARI, {
            plaka: arac.plate,
            marka: arac.brand,
            model: arac.model,
            yil: arac.year,
            acilKisiAd: arac.acilKisiAd,
            acilKisiTelefon: arac.acilKisiTelefon,
            acilNot: arac.acilNot
        });
    }

    function kazaRehberiniAc() {
        state.kazaTetikleyici = document.activeElement === document.body ? null : document.activeElement;
        el("kaza-modal").classList.remove("hidden");
        document.body.classList.add("kaza-modal-acik");
        document.addEventListener("keydown", kazaModaliKlavye);
        el("kaza-kapat").focus();
        showMessage(el("kaza-durum"), "");
        state.kazaDosyaId = null;
        acilKartiCiz();
        kuyrukRozetiniTazele();

        if (state.kazaRehberi) {
            kazaRehberiCiz(state.kazaRehberi);
            return;
        }

        var saklanan = yerelOku(KAZA_REHBER_ANAHTARI);
        if (saklanan) {
            state.kazaRehberi = saklanan;
            kazaRehberiCiz(saklanan);
        }

        api("/api/Hasar/rehber").then(function (result) {
            state.kazaRehberi = result.data;
            yerelYaz(KAZA_REHBER_ANAHTARI, result.data);
            kazaRehberiCiz(result.data);
        }).catch(function (error) {
            if (!saklanan) {
                handleError(el("kaza-durum"), error);
            }
        });
    }

    function kazaModaliKapat() {
        el("kaza-modal").classList.add("hidden");
        document.body.classList.remove("kaza-modal-acik");
        document.removeEventListener("keydown", kazaModaliKlavye);

        var tetikleyici = state.kazaTetikleyici;
        state.kazaTetikleyici = null;

        if (tetikleyici && typeof tetikleyici.focus === "function" && !tetikleyici.classList.contains("hidden")) {
            tetikleyici.focus();
            return;
        }

        var acilis = el("kaza-ani");

        if (acilis) {
            acilis.focus();
        }
    }

    function kazaModaliKlavye(olay) {
        if (el("kaza-modal").classList.contains("hidden")) {
            return;
        }

        if (olay.key === "Escape") {
            olay.preventDefault();
            kazaModaliKapat();
            return;
        }

        if (olay.key !== "Tab") {
            return;
        }

        var odaklanabilir = Array.prototype.filter.call(
            el("kaza-modal").querySelectorAll("a[href], button:not([disabled]), input:not([disabled]), select, textarea"),
            function (oge) { return oge.offsetParent !== null; });

        if (odaklanabilir.length === 0) {
            return;
        }

        var ilk = odaklanabilir[0];
        var son = odaklanabilir[odaklanabilir.length - 1];

        if (olay.shiftKey && document.activeElement === ilk) {
            olay.preventDefault();
            son.focus();
            return;
        }

        if (!olay.shiftKey && document.activeElement === son) {
            olay.preventDefault();
            ilk.focus();
        }
    }

    function kazaDosyasiAc() {

        if (!state.selectedVehicleId) {
            showMessage(el("kaza-durum"), "Önce bir araç seçin.", false);
            return;
        }

        if (state.kazaDosyaId) {
            el("kaza-foto").click();
            return;
        }

        var govde = {
            vehicleId: state.selectedVehicleId,
            olayTarihi: todayInput(),
            tur: "Kaza",
            aciklama: "Kaza anı rehberinden açıldı, ayrıntı sonra eklenecek.",
            tutanakTuru: "Yok"
        };

        if (!navigator.onLine) {
            kuyrugaAlVeBildir(govde);
            return;
        }

        showMessage(el("kaza-durum"), "Hasar dosyası açılıyor…", true);

        api("/api/Hasar", {
            method: "POST",
            body: govde
        }).then(function (result) {
            state.kazaDosyaId = result.data.id;
            showMessage(el("kaza-durum"), "Dosya açıldı. Şimdi fotoğrafları çekin.", true);
            el("kaza-foto").click();
        }).catch(function (error) {
            if (error && error.durum) {
                handleError(el("kaza-durum"), error);
                return;
            }

            kuyrugaAlVeBildir(govde);
        });
    }

    function kazaFotoYukle(dosyalar) {
        if (!state.kazaDosyaId || dosyalar.length === 0) {
            return;
        }

        var sira = Promise.resolve();
        var yuklenen = 0;

        Array.prototype.forEach.call(dosyalar, function (dosya) {
            sira = sira.then(function () {
                var form = new FormData();
                form.append("file", dosya);
                form.append("etiket", "Genel");
                return api("/api/Hasar/" + state.kazaDosyaId + "/foto", { method: "POST", body: form })
                    .then(function () {
                        yuklenen++;
                        showMessage(el("kaza-durum"), yuklenen + " fotoğraf yüklendi.", true);
                    });
            });
        });

        sira.then(function () {
            showMessage(el("kaza-durum"), yuklenen + " fotoğraf yüklendi. Ayrıntıları Hasar sekmesinden tamamlayabilirsiniz.", true);
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("kaza-durum"), error);
        });
    }

    function bindKaza() {
        el("kaza-ani").addEventListener("click", kazaRehberiniAc);

        window.addEventListener("online", function () { kuyrugoBosalt().then(kuyrukRozetiniGuncelle); });

        if ("serviceWorker" in navigator) {
            navigator.serviceWorker.addEventListener("message", function (event) {
                if (event.data && event.data.tur === "hasar-kuyrugu-bosalt") {
                    kuyrugoBosalt();
                }
            });
        }

        el("kaza-kapat").addEventListener("click", kazaModaliKapat);

        el("kaza-modal").addEventListener("click", function (event) {
            if (event.target === el("kaza-modal")) {
                el("kaza-modal").classList.add("hidden");
            }
        });

        el("kaza-dosya-ac").addEventListener("click", kazaDosyasiAc);

        el("kaza-foto").addEventListener("change", function (event) {
            kazaFotoYukle(event.target.files);
            event.target.value = "";
        });
    }

    var HASAR_TUR = [
        ["Kaza", "Kaza"],
        ["Hasar", "Hasar"],
        ["Cam", "Cam"],
        ["Dolu", "Dolu"],
        ["Hirsizlik", "Hırsızlık"],
        ["Diger", "Diğer"]
    ];

    var HASAR_TUTANAK = [
        ["Anlasmali", "Anlaşmalı tutanak"],
        ["Polis", "Polis/jandarma tutanağı"],
        ["Yok", "Tutanak yok"]
    ];

    var HASAR_DURUM = [
        ["Acik", "Açık"],
        ["SigortaIslemde", "Sigorta işlemde"],
        ["Kapandi", "Kapandı"]
    ];

    var HASAR_ETIKET = [
        ["Genel", "Genel görünüm"],
        ["HasarYakin", "Hasar yakın çekim"],
        ["KarsiArac", "Karşı araç"],
        ["Plakalar", "Plakalar"],
        ["Yol", "Yol ve işaretler"],
        ["Belge", "Belge"],
        ["Tutanak", "Tutanak"]
    ];

    var DEGER_KAYNAK = [
        ["Beyan", "Beyan"],
        ["Ekspertiz", "Ekspertiz"],
        ["Ilan", "İlan"]
    ];

    var HASAR_ADIM_BASLIKLARI = [
        "1/3 · Olay",
        "2/3 · Fotoğraflar",
        "3/3 · Karşı taraf ve tutanak"
    ];

    function hasarAdimGoster(adim) {
        state.hasarAdim = adim;

        [1, 2, 3].forEach(function (no) {
            el("hasar-adim-" + no).classList.toggle("hidden", no !== adim);
        });

        el("hasar-adim-basligi").textContent = HASAR_ADIM_BASLIKLARI[adim - 1];
        el("hasar-geri").classList.toggle("hidden", adim === 1);
        el("hasar-ileri").classList.toggle("hidden", adim === 3);
        el("hasar-bitir").classList.toggle("hidden", adim !== 3);
    }

    function hasarSihirbaziniAc(dosya) {
        el("hasar-form").classList.remove("hidden");
        el("hasar-yeni").classList.add("hidden");
        showMessage(el("hasar-foto-durum"), "");
        clear(el("hasar-foto-listesi"));

        state.hasarDosyaId = dosya ? dosya.id : null;
        state.hasarYeniKayit = !dosya;

        el("hasar-tarih").value = dosya ? String(dosya.olayTarihi).slice(0, 10) : todayInput();
        el("hasar-tur").value = dosya ? dosya.tur : "Kaza";
        el("hasar-konum").value = (dosya && dosya.konum) || "";
        el("hasar-km").value = dosya && dosya.olayKm !== null && dosya.olayKm !== undefined ? dosya.olayKm : "";
        el("hasar-aciklama").value = (dosya && dosya.aciklama) || "";
        el("hasar-tutanak").value = dosya ? dosya.tutanakTuru : "Anlasmali";
        el("hasar-durum").value = dosya ? dosya.durum : "Acik";
        el("hasar-karsi-plaka").value = (dosya && dosya.karsiTarafPlaka) || "";
        el("hasar-karsi-sigorta").value = (dosya && dosya.karsiTarafSigorta) || "";
        el("hasar-karsi-police").value = (dosya && dosya.karsiTarafPoliceNo) || "";
        el("hasar-sigorta-dosya").value = (dosya && dosya.sigortaDosyaNo) || "";
        el("hasar-bedel").value = dosya && dosya.hasarBedeli !== null && dosya.hasarBedeli !== undefined ? dosya.hasarBedeli : "";

        if (dosya) {
            hasarFotolariniCiz(dosya.fotograflar || []);
        }

        hasarAdimGoster(1);
    }

    function hasarSihirbaziniKapat() {
        el("hasar-form").classList.add("hidden");
        el("hasar-yeni").classList.remove("hidden");
        state.hasarDosyaId = null;
        state.hasarYeniKayit = false;

        if (activeTab() === "hasar") {
            loadHasar();
        }
    }

    function hasarSihirbaziniIptalEt() {
        var id = state.hasarDosyaId;

        if (!id || !state.hasarYeniKayit) {
            hasarSihirbaziniKapat();
            return;
        }

        if (!window.confirm("Bu hasar dosyası kaydedilmişti. Vazgeçerseniz silinecek. Devam edilsin mi?")) {
            return;
        }

        state.hasarDosyaId = null;
        state.hasarYeniKayit = false;

        api("/api/Hasar/" + id, { method: "DELETE" }).then(function () {
            showMessage(el("app-message"), "Hasar dosyası silindi.", true);
        }).catch(function (error) {
            handleError(el("app-message"), error);
        }).finally(function () {
            hasarSihirbaziniKapat();
        });
    }

    function hasarGovdesi() {
        var km = el("hasar-km").value;
        var bedel = el("hasar-bedel").value.trim();
        var bedelSayi = bedel === "" ? NaN : sayiOku(bedel);

        return {
            vehicleId: state.selectedVehicleId,
            olayTarihi: el("hasar-tarih").value,
            tur: el("hasar-tur").value,
            konum: el("hasar-konum").value,
            aciklama: el("hasar-aciklama").value,
            olayKm: km === "" ? null : Number(km),
            tutanakTuru: el("hasar-tutanak").value,
            karsiTarafPlaka: el("hasar-karsi-plaka").value,
            karsiTarafSigorta: el("hasar-karsi-sigorta").value,
            karsiTarafPoliceNo: el("hasar-karsi-police").value,
            sigortaDosyaNo: el("hasar-sigorta-dosya").value,
            hasarBedeli: isNaN(bedelSayi) ? null : bedelSayi,
            durum: el("hasar-durum").value
        };
    }

    function hasarDosyasiniKaydet() {
        var govde = hasarGovdesi();

        if (state.hasarDosyaId) {
            return api("/api/Hasar/" + state.hasarDosyaId, { method: "PUT", body: govde });
        }

        return api("/api/Hasar", { method: "POST", body: govde }).then(function (result) {
            state.hasarDosyaId = result.data.id;
            return result;
        });
    }

    function hasarFotolariniCiz(fotolar) {
        var liste = el("hasar-foto-listesi");
        clear(liste);

        fotolar.forEach(function (foto) {
            var li = document.createElement("li");
            li.appendChild(make("span", foto.etiketAdi + " · " + (foto.dosyaAdi || "-")));

            var sil = make("button", "Sil", "ghost");
            sil.type = "button";
            sil.addEventListener("click", function () { hasarFotoSil(foto.id); });
            li.appendChild(sil);

            liste.appendChild(li);
        });

        showMessage(el("hasar-foto-durum"), fotolar.length + " / 20 fotoğraf", true);
    }

    function hasarDosyasiniTazele() {
        if (!state.hasarDosyaId) {
            return Promise.resolve();
        }

        return api("/api/Hasar/" + state.hasarDosyaId).then(function (result) {
            hasarFotolariniCiz(result.data.fotograflar || []);
        });
    }

    function hasarFotoSil(fotoId) {
        api("/api/Hasar/" + state.hasarDosyaId + "/foto/" + fotoId, { method: "DELETE" })
            .then(hasarDosyasiniTazele)
            .catch(function (error) { handleError(el("hasar-foto-durum"), error); });
    }

    function hasarFotoYukle(dosyalar) {
        if (dosyalar.length === 0) {
            return;
        }

        var etiket = el("hasar-foto-etiket").value;
        var sira = Promise.resolve();

        Array.prototype.forEach.call(dosyalar, function (dosya) {
            sira = sira.then(function () {
                var form = new FormData();
                form.append("file", dosya);
                form.append("etiket", etiket);
                return api("/api/Hasar/" + state.hasarDosyaId + "/foto", { method: "POST", body: form });
            });
        });

        sira.then(hasarDosyasiniTazele).catch(function (error) {
            handleError(el("hasar-foto-durum"), error);
            hasarDosyasiniTazele();
        });
    }

    function loadHasar() {
        var tbody = el("hasar-rows");

        return api("/api/Vehicles/" + state.selectedVehicleId + "/hasar").then(function (result) {
            var liste = (result && result.data) || [];
            clear(tbody);

            if (liste.length === 0) {
                bosSatir(tbody, 7, "hasar");
                return;
            }

            liste.forEach(function (dosya) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", formatDate(dosya.olayTarihi)));
                tr.appendChild(make("td", dosya.turAdi));

                var durum = document.createElement("td");
                durum.appendChild(make("span", dosya.durumAdi, "rozet rozet-" + dosya.durum.toLowerCase()));
                tr.appendChild(durum);

                tr.appendChild(make("td", dosya.konum || "-"));
                tr.appendChild(make("td", String(dosya.fotoSayisi)));
                tr.appendChild(make("td", dosya.hasarBedeli === null || dosya.hasarBedeli === undefined ? "-" : money(dosya.hasarBedeli)));

                var islem = document.createElement("td");

                if (canManage()) {
                    var duzenle = make("button", "Düzenle", "ghost");
                    duzenle.type = "button";
                    duzenle.addEventListener("click", function () {
                        api("/api/Hasar/" + dosya.id).then(function (tam) {
                            hasarSihirbaziniAc(tam.data);
                        }).catch(function (error) { handleError(el("app-message"), error); });
                    });
                    islem.appendChild(duzenle);
                }

                var tutanak = make("button", "Tutanak", "ghost");
                tutanak.type = "button";
                tutanak.addEventListener("click", function () { hasarTutanagiAc(dosya.id); });
                islem.appendChild(tutanak);

                if (canManage()) {
                    var sil = make("button", "Sil", "ghost");
                    sil.type = "button";
                    sil.addEventListener("click", function () {
                        removeRecord("/api/Hasar/" + dosya.id, loadHasar);
                    });
                    islem.appendChild(sil);
                }

                tr.appendChild(islem);
                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            clear(tbody);
            emptyRow(tbody, 7, error && error.message ? error.message : "Hasar dosyaları alınamadı.");
        });
    }

    function hasarTutanagiAc(dosyaId) {
        var pencere = window.open("", "_blank");

        if (!pencere) {
            showMessage(el("app-message"),
                "Tarayıcı yeni sekmeyi engelledi. Bu site için açılır pencerelere izin verin.", false);
            return;
        }

        pencere.document.write("<!DOCTYPE html><html lang=\"tr\"><head><meta charset=\"utf-8\">" +
            "<title>Tutanak</title></head><body>Tutanak hazırlanıyor…</body></html>");

        fetch("/api/Hasar/" + dosyaId + "/tutanak.html", {
            headers: { Authorization: "Bearer " + state.token }
        }).then(function (response) {
            if (!response.ok) {
                throw new Error("Tutanak alınamadı.");
            }
            return response.text();
        }).then(function (html) {
            pencere.document.open();
            pencere.document.write(html);
            pencere.document.close();

            var dugme = pencere.document.querySelector(".yazdir");

            if (dugme) {
                dugme.addEventListener("click", function () { pencere.print(); });
            }
        }).catch(function (error) {
            pencere.close();
            handleError(el("app-message"), error);
        });
    }

    function bindHasar() {
        el("hasar-yeni").addEventListener("click", function () { hasarSihirbaziniAc(null); });
        el("hasar-vazgec").addEventListener("click", hasarSihirbaziniIptalEt);

        el("hasar-geri").addEventListener("click", function () {
            hasarAdimGoster(Math.max(1, state.hasarAdim - 1));
        });

        el("hasar-ileri").addEventListener("click", function () {
            clearMessages();

            if (state.hasarAdim === 1) {
                if (!el("hasar-tarih").value || !el("hasar-aciklama").value.trim()) {
                    showMessage(el("app-message"), "Olay tarihi ve açıklama zorunlu.", false);
                    return;
                }

                hasarDosyasiniKaydet().then(function () {
                    hasarAdimGoster(2);
                    return hasarDosyasiniTazele();
                }).catch(function (error) { handleError(el("app-message"), error); });
                return;
            }

            hasarAdimGoster(3);
        });

        el("hasar-foto").addEventListener("change", function (event) {
            hasarFotoYukle(event.target.files);
            event.target.value = "";
        });

        el("hasar-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();

            var yeniMi = state.hasarYeniKayit;

            hasarDosyasiniKaydet().then(function () {
                showMessage(el("app-message"),
                    yeniMi ? "Hasar dosyası oluşturuldu." : "Hasar dosyası güncellendi.", true);
                hasarSihirbaziniKapat();
            }).catch(function (error) { handleError(el("app-message"), error); })
              .finally(function () { if (typeof acKilit === "function") { acKilit(); } });
        });
    }

    function degerRozeti(kaynak) {
        return "rozet rozet-kaynak-" + String(kaynak).toLowerCase();
    }

    function loadDeger() {
        var cards = el("deger-cards");
        var tbody = el("deger-rows");

        return api("/api/Vehicles/" + state.selectedVehicleId + "/deger").then(function (result) {
            var seri = (result && result.data) || {};
            var kayitlar = seri.kayitlar || [];

            clear(cards);
            cards.appendChild(card("Son değer", seri.sonDeger ? money(seri.sonDeger.deger) : "—", true));
            cards.appendChild(card("Kaynak", seri.sonDeger ? seri.sonDeger.kaynakAdi : "—"));
            cards.appendChild(card("Değer kaybı", seri.degerKaybi === null || seri.degerKaybi === undefined ? "—" : money(seri.degerKaybi)));

            clear(tbody);
            if (kayitlar.length === 0) {
                emptyRow(tbody, 4, "Bu araçta değer kaydı yok.");
            } else {
                kayitlar.forEach(function (kayit) {
                    var tr = document.createElement("tr");
                    tr.appendChild(make("td", formatDate(kayit.tarih)));
                    tr.appendChild(make("td", money(kayit.deger)));

                    var kaynak = document.createElement("td");
                    kaynak.appendChild(make("span", kayit.kaynakAdi, degerRozeti(kayit.kaynak)));
                    tr.appendChild(kaynak);

                    tr.appendChild(make("td", kayit.not || "-"));
                    tbody.appendChild(tr);
                });
            }

            grafikKitapligi().then(function () { drawDegerChart(kayitlar); }).catch(function () { });
        }).catch(function (error) {
            clear(cards);
            cards.appendChild(card("Değer", error && error.message ? error.message : "Alınamadı."));
        });
    }

    function drawDegerChart(kayitlar) {
        var canvas = el("deger-chart");
        var fallback = el("deger-fallback");

        if (state.degerChart) {
            state.degerChart.destroy();
            state.degerChart = null;
        }

        if (typeof Chart === "undefined" || kayitlar.length === 0) {
            canvas.classList.add("hidden");
            fallback.textContent = kayitlar.length === 0 ? "Grafik için en az bir değer kaydı gerekir." : "Grafik kitaplığı yüklenemedi.";
            return;
        }

        canvas.classList.remove("hidden");
        fallback.textContent = "";

        var sirali = kayitlar.slice().reverse();

        state.degerChart = new Chart(canvas.getContext("2d"), {
            type: "line",
            data: {
                labels: sirali.map(function (k) { return formatDate(k.tarih); }),
                datasets: [{
                    label: "Araç değeri (TL)",
                    data: sirali.map(function (k) { return Number(k.deger); }),
                    borderColor: "#ff7a1a",
                    backgroundColor: "rgba(255, 122, 26, 0.18)",
                    tension: 0.25,
                    fill: true
                }]
            },
            options: grafikSecenekleri(false)
        });
    }

    function degerTahminiIste() {
        clearMessages();
        el("deger-kasa-kutu").classList.add("hidden");

        return api("/api/Vehicles/" + state.selectedVehicleId + "/deger/tahmin", { method: "POST" })
            .then(function (result) {
                var uyari = el("deger-uyari");
                uyari.textContent = result.data.uyari + " Bugün kalan tahmin hakkı: " + result.data.kalanHak + ".";
                uyari.classList.remove("hidden");
                showMessage(el("app-message"), (result && result.message) || "Tahmin alındı.", true);
                loadDeger();
            })
            .catch(function (error) {
                if (error && error.durum === 422 && error.message && error.message.indexOf("kasa tipi") >= 0) {
                    el("deger-uyari").classList.add("hidden");
                    el("deger-kasa-kutu").classList.remove("hidden");
                    return;
                }

                var uyari = el("deger-uyari");
                uyari.textContent = error && error.message ? error.message : "Tahmin alınamadı.";
                uyari.classList.remove("hidden");
            });
    }

    function bindDeger() {
        el("deger-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();
            el("deger-uyari").classList.add("hidden");

            api("/api/Vehicles/" + state.selectedVehicleId + "/deger", {
                method: "POST",
                body: {
                    tarih: el("deger-tarih").value,
                    deger: sayiAlan("deger-tutar"),
                    kaynak: el("deger-kaynak").value,
                    not: el("deger-not").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Değer kaydedildi.", true);
                el("deger-form").reset();
                el("deger-tarih").value = todayInput();
                loadDeger();
            }).catch(function (error) { handleError(el("app-message"), error); });
        });

        el("deger-tahmin").addEventListener("click", degerTahminiIste);

        el("deger-kasa-kaydet").addEventListener("click", function () {
            clearMessages();

            api("/api/Vehicles/" + state.selectedVehicleId + "/kasa-tipi", {
                method: "PUT",
                body: el("deger-kasa").value
            }).then(function () {
                el("deger-kasa-kutu").classList.add("hidden");
                return loadVehicles().then(degerTahminiIste);
            }).catch(function (error) { handleError(el("app-message"), error); });
        });
    }

    var DEMO_EPOSTA = "demo@garajim.app";
    var DEMO_SIFRE = "Demo1234!";

    var TANITIM_KARTLARI = [
        ["Fişi fotoğrafla", "Servis fişini çek, tarih, tutar ve işlem türü otomatik çıkarılsın; sen yalnız onayla.", ""],
        ["Araç karnesi ve QR", "Bakım geçmişini tek bağlantıyla paylaş. Alıcı QR'ı okutur, kayıtları görür; neyi paylaştığına sen karar verirsin.", ""],
        ["Parça hafızası", "Hangi parça ne zaman, kaç kilometrede değişti? Sıradaki değişim yaklaşınca haber verir.", ""],
        ["Evrak takvimi", "Muayene, sigorta, kasko ve egzoz bitişleri takvimine düşer; süresi dolmadan e-posta gelir.", ""],
        ["AI Usta", "Aracının kendi kayıtlarını okuyup olasılık sıralar. Teşhis koymaz, ustaya gitmeden önce ne soracağını bilirsin.", "yakında"],
        ["Filo paketi", "Birden çok araç, sürücü zimmeti, ekip belgeleri ve km başına maliyet karşılaştırması.", ""]
    ];


    var REHBER_KARTLARI = [
        ["Motor arıza lambası yanıyor", "/rehber/belirti/sari-motor-ariza-lambasi-sabit-yaniyor-arac-normal-gidiyor.html"],
        ["P0420 arıza kodu", "/rehber/obd/p0420-ariza-kodu-nedir-anlami-nedenleri-aciliyet.html"],
        ["Fiat Egea 1.4 bakım aralıkları", "/rehber/bakim/"],
        ["Muayeneden kalma nedenleri", "/rehber/muayene/"],
        ["Kış lastiği zorunluluğu", "/rehber/turkiye/"],
        ["TÜVTÜRK kontrol listesi", "/rehber/muayene/"]
    ];

    function rehberKartlariniCiz() {
        var liste = el("tanitim-rehber-kartlar");

        if (!liste) {
            return;
        }

        clear(liste);

        REHBER_KARTLARI.forEach(function (kart) {
            var madde = document.createElement("li");
            var baglanti = document.createElement("a");

            baglanti.className = "tanitim-kart tanitim-rehber-kart";
            baglanti.href = kart[1];
            baglanti.appendChild(make("h3", kart[0]));

            madde.appendChild(baglanti);
            liste.appendChild(madde);
        });
    }

    function rehberBaglantisi(kap, metin, adres) {
        if (!kap || kap.querySelector(".rehber-link")) {
            return;
        }

        var baglanti = make("a", metin, "rehber-link");
        baglanti.href = adres;
        baglanti.target = "_blank";
        baglanti.rel = "noopener";

        kap.appendChild(baglanti);
    }

    function rehberBaglantilariniKur() {
        rehberBaglantisi(el("evrak-form"), "Rehber: muayene kuralları", "/rehber/muayene/");
        rehberBaglantisi(el("panel-lastik"), "Rehber: kış lastiği zorunluluğu", "/rehber/turkiye/");
        rehberBaglantisi(el("panel-parca"), "Rehber: bakım aralıkları", "/rehber/bakim/");
    }

    function tanitimKartlariniCiz() {

        var liste = el("tanitim-kartlar");
        if (!liste) {
            return;
        }

        clear(liste);

        TANITIM_KARTLARI.forEach(function (kart) {
            var li = document.createElement("li");
            li.className = "tanitim-kart";

            var baslik = make("h2", kart[0]);
            if (kart[2]) {
                baslik.appendChild(make("span", kart[2], "kart-etiket"));
            }

            li.appendChild(baslik);
            li.appendChild(make("p", kart[1]));
            liste.appendChild(li);
        });
    }

    function tanitimDesteginiYukle() {
        var baglanti = el("tanitim-destek");
        var plan = el("tanitim-plan-iletisim");

        if (!baglanti) {
            return;
        }

        fetch("/api/Yardim/sss", { headers: { "Accept": "application/json" } })
            .then(function (cevap) { return cevap.json(); })
            .then(function (sonuc) {
                var eposta = sonuc && sonuc.data ? sonuc.data.destekEposta : "";
                if (!eposta) {
                    return;
                }

                baglanti.href = "mailto:" + eposta;
                baglanti.textContent = eposta;
                plan.href = "mailto:" + eposta + "?subject=" + encodeURIComponent("Garajım filo paketi");
            })
            .catch(function () { });
    }

    function kayitSekmesineGec() {
        switchAuthTab(false);
        el("auth-screen").scrollTo({ top: document.querySelector(".auth-card").offsetTop, behavior: "smooth" });
        el("register-name").focus();
    }

    function demoIleGir() {
        showMessage(el("tanitim-mesaj"), "Demo hesabı açılıyor…", true);

        api("/api/Auth/login", {
            method: "POST",
            body: { email: DEMO_EPOSTA, password: DEMO_SIFRE }
        }).then(function (result) {
            saveSession(result.data.token, result.data);
            showMessage(el("tanitim-mesaj"), "");
            enterApp();
        }).catch(function () {
            showMessage(el("tanitim-mesaj"), "Demo hesabı bu sunucuda kapalı. Ücretsiz başla ile kendi hesabınızı açabilirsiniz.", false);
        });
    }

    var DOGRULA_GERI_SAYIM = 60;

    function kodKutulari() {
        return [1, 2, 3, 4, 5, 6].map(function (no) { return el("dogrula-" + no); });
    }

    function kodOku() {
        return kodKutulari().map(function (kutu) { return kutu.value.trim(); }).join("");
    }

    function kodTemizle() {
        kodKutulari().forEach(function (kutu) { kutu.value = ""; });
    }

    function kodYaz(metin) {
        var rakamlar = String(metin || "").replace(/\D/g, "").slice(0, 6);
        var kutular = kodKutulari();

        kutular.forEach(function (kutu, sira) {
            kutu.value = rakamlar[sira] || "";
        });

        var sonraki = Math.min(rakamlar.length, 5);
        kutular[sonraki].focus();
    }

    function dogrulamaEkraniniAc(eposta, mesaj) {
        state.dogrulanacakEposta = eposta;

        el("auth-screen").classList.remove("hidden");
        el("app-screen").classList.add("hidden");
        document.querySelector(".auth-card").classList.add("hidden");
        el("tanitim").classList.add("hidden");
        el("dogrula-kart").classList.remove("hidden");

        el("dogrula-aciklama").textContent = eposta + " adresine 6 haneli bir kod gönderdik.";
        showMessage(el("dogrula-mesaj"), mesaj || "", true);

        kodTemizle();
        kodKutulari()[0].focus();
        geriSayimBaslat();
    }

    function dogrulamaEkraniniKapat() {
        state.dogrulanacakEposta = null;
        geriSayimDurdur();
        el("dogrula-kart").classList.add("hidden");
        document.querySelector(".auth-card").classList.remove("hidden");
        el("tanitim").classList.remove("hidden");
    }

    function geriSayimDurdur() {
        if (state.dogrulaSayac) {
            clearInterval(state.dogrulaSayac);
            state.dogrulaSayac = null;
        }
    }

    function geriSayimBaslat() {
        geriSayimDurdur();

        var kalan = DOGRULA_GERI_SAYIM;
        var dugme = el("dogrula-yeniden");

        function yaz() {
            if (kalan <= 0) {
                dugme.disabled = false;
                dugme.textContent = "Kodu yeniden gönder";
                geriSayimDurdur();
                return;
            }

            dugme.disabled = true;
            dugme.textContent = "Kodu yeniden gönder (" + kalan + " sn)";
            kalan--;
        }

        yaz();
        state.dogrulaSayac = setInterval(yaz, 1000);
    }

    function kodDogrula() {
        var kod = kodOku();

        if (kod.length !== 6) {
            showMessage(el("dogrula-mesaj"), "6 haneyi de girin.", false);
            return;
        }

        api("/api/Auth/dogrula", {
            method: "POST",
            body: { email: state.dogrulanacakEposta, kod: kod }
        }).then(function (result) {
            geriSayimDurdur();
            el("dogrula-kart").classList.add("hidden");
            document.querySelector(".auth-card").classList.remove("hidden");
            el("tanitim").classList.remove("hidden");
            state.dogrulanacakEposta = null;
            saveSession(result.data.token, result.data);
            enterApp();
        }).catch(function (error) {
            kodTemizle();
            kodKutulari()[0].focus();
            handleError(el("dogrula-mesaj"), error);
        });
    }

    function bindDogrulama() {
        var kutular = kodKutulari();

        kutular.forEach(function (kutu, sira) {
            kutu.addEventListener("input", function () {
                kutu.value = kutu.value.replace(/\D/g, "").slice(0, 1);

                if (kutu.value && sira < 5) {
                    kutular[sira + 1].focus();
                }

                if (kodOku().length === 6) {
                    kodDogrula();
                }
            });

            kutu.addEventListener("keydown", function (event) {
                if (event.key === "Backspace" && !kutu.value && sira > 0) {
                    kutular[sira - 1].focus();
                }
            });

            kutu.addEventListener("paste", function (event) {
                event.preventDefault();
                kodYaz((event.clipboardData || window.clipboardData).getData("text"));

                if (kodOku().length === 6) {
                    kodDogrula();
                }
            });
        });

        el("dogrula-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            kodDogrula();
        });

        el("dogrula-yeniden").addEventListener("click", function () {
            api("/api/Auth/kod-gonder", {
                method: "POST",
                body: { email: state.dogrulanacakEposta }
            }).then(function (result) {
                showMessage(el("dogrula-mesaj"), (result && result.message) || "Kod gönderildi.", true);
                kodTemizle();
                kodKutulari()[0].focus();
                geriSayimBaslat();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("dogrula-mesaj"), error);
            });
        });

        el("dogrula-vazgec").addEventListener("click", function () {
            dogrulamaEkraniniKapat();
            switchAuthTab(true);
        });
    }

    function sifirlamaKutulari() {
        return [1, 2, 3, 4, 5, 6].map(function (no) { return el("sifirlama-" + no); });
    }

    function sifirlamaKoduOku() {
        return sifirlamaKutulari().map(function (kutu) { return kutu.value.trim(); }).join("");
    }

    function sifirlamaEkraniniAc() {
        el("auth-screen").classList.remove("hidden");
        el("app-screen").classList.add("hidden");
        document.querySelector(".auth-card").classList.add("hidden");
        el("tanitim").classList.add("hidden");
        el("dogrula-kart").classList.add("hidden");
        el("sifirlama-kart").classList.remove("hidden");

        el("sifirlama-eposta-form").classList.remove("hidden");
        el("sifirlama-kod-form").classList.add("hidden");
        el("sifirlama-aciklama").textContent = "Kayıtlı e-posta adresinizi girin.";
        showMessage(el("sifirlama-mesaj"), "");

        el("sifirlama-eposta").value = el("login-email").value.trim();
        el("sifirlama-eposta").focus();
    }

    function sifirlamaEkraniniKapat() {
        state.sifirlanacakEposta = null;
        el("sifirlama-kart").classList.add("hidden");
        document.querySelector(".auth-card").classList.remove("hidden");
        el("tanitim").classList.remove("hidden");
    }

    function sifirlamaKoduIste() {
        var eposta = el("sifirlama-eposta").value.trim();
        if (!eposta) {
            return;
        }

        gonderimKilitle("sifirlama-kod-gonder", true, "Gönderiliyor…");

        api("/api/Auth/sifre-sifirla-kod", {
            method: "POST",
            body: { email: eposta }
        }).then(function (result) {
            state.sifirlanacakEposta = eposta;
            el("sifirlama-eposta-form").classList.add("hidden");
            el("sifirlama-kod-form").classList.remove("hidden");
            el("sifirlama-aciklama").textContent = eposta + " adresine kod gönderildiyse birazdan ulaşır.";
            showMessage(el("sifirlama-mesaj"), (result && result.message) || "", true);
            sifirlamaKutulari().forEach(function (kutu) { kutu.value = ""; });
            el("sifirlama-yeni").value = "";
            sifirlamaKutulari()[0].focus();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("sifirlama-mesaj"), error);
        }).then(function () {
            gonderimKilitle("sifirlama-kod-gonder", false);
        });
    }

    function sifreyiSifirla() {
        var kod = sifirlamaKoduOku();
        var yeni = el("sifirlama-yeni").value;

        if (kod.length !== 6 || !yeni) {
            showMessage(el("sifirlama-mesaj"), "6 haneli kodu ve yeni şifrenizi girin.", false);
            return;
        }

        api("/api/Auth/sifre-sifirla", {
            method: "POST",
            body: { email: state.sifirlanacakEposta, kod: kod, yeniSifre: yeni }
        }).then(function (result) {
            sifirlamaEkraniniKapat();
            switchAuthTab(true);
            el("login-email").value = state.sifirlanacakEposta || "";
            el("login-password").value = "";
            el("login-password").focus();
            showMessage(el("auth-message"), (result && result.message) || "Şifreniz değiştirildi.", true);
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("sifirlama-mesaj"), error);
            sifirlamaKutulari().forEach(function (kutu) { kutu.value = ""; });
            sifirlamaKutulari()[0].focus();
        });
    }

    function bindSifirlama() {
        var kutular = sifirlamaKutulari();

        kutular.forEach(function (kutu, sira) {
            kutu.addEventListener("input", function () {
                kutu.value = kutu.value.replace(/\D/g, "").slice(0, 1);

                if (kutu.value && sira < 5) {
                    kutular[sira + 1].focus();
                }
            });

            kutu.addEventListener("keydown", function (event) {
                if (event.key === "Backspace" && !kutu.value && sira > 0) {
                    kutular[sira - 1].focus();
                }
            });

            kutu.addEventListener("paste", function (event) {
                event.preventDefault();

                var metin = String((event.clipboardData || window.clipboardData).getData("text") || "")
                    .replace(/\D/g, "").slice(0, 6);

                kutular.forEach(function (hedef, i) { hedef.value = metin.charAt(i) || ""; });

                var sonrakiBos = metin.length < 6 ? metin.length : 5;
                kutular[sonrakiBos].focus();
            });
        });

        el("sifre-unuttum").addEventListener("click", sifirlamaEkraniniAc);
        el("sifirlama-vazgec").addEventListener("click", sifirlamaEkraniniKapat);

        el("sifirlama-geri").addEventListener("click", function () {
            el("sifirlama-kod-form").classList.add("hidden");
            el("sifirlama-eposta-form").classList.remove("hidden");
            el("sifirlama-aciklama").textContent = "Kayıtlı e-posta adresinizi girin.";
            showMessage(el("sifirlama-mesaj"), "");
            el("sifirlama-eposta").focus();
        });

        el("sifirlama-eposta-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            sifirlamaKoduIste();
        });

        el("sifirlama-kod-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            sifreyiSifirla();
        });
    }

    function geciciSifreUyarisi(kullanici) {
        var uyari = el("gecici-sifre-uyari");

        if (!kullanici || !kullanici.geciciSifre) {
            uyari.classList.add("hidden");
            uyari.textContent = "";
            return;
        }

        uyari.textContent = "Hesabınız geçici bir şifreyle açıldı. Ayarlar bölümünden kendi şifrenizi belirlemeniz önerilir.";
        uyari.classList.remove("hidden");
    }

    function hesapDurumunuYukle() {
        return api("/api/Account/durum").then(function (result) {
            var veri = (result && result.data) || {};
            var serit = el("silme-serit");

            if (!veri.silmePlanlandi) {
                serit.classList.add("hidden");
                return;
            }

            el("silme-serit-metin").textContent =
                "Hesabınız " + veri.kalanGun + " gün sonra kalıcı olarak silinecek.";
            el("silme-iptal").classList.toggle("hidden", !isOwner());
            serit.classList.remove("hidden");
        }).catch(function () {
        });
    }

    function bindHesapSilme() {
        el("hesap-sil-kod").addEventListener("click", function () {
            api("/api/Account/sil-kod", { method: "POST" }).then(function (result) {
                el("hesap-sil-form").classList.remove("hidden");
                showMessage(el("hesap-sil-mesaj"), (result && result.message) || "", true);
            }).catch(function (error) { handleError(el("hesap-sil-mesaj"), error); });
        });

        el("hesap-sil-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);

            var sirket = (state.user && state.user.companyName) || "";
            var yazilan = el("hesap-sil-ad").value.trim();

            if (yazilan.toLocaleLowerCase("tr") !== sirket.trim().toLocaleLowerCase("tr")) {
                showMessage(el("hesap-sil-mesaj"), "Şirket adı eşleşmedi, hesap silinmedi.", false);
                if (typeof acKilit === "function") { acKilit(); }
                return;
            }

            api("/api/Account/sil", { method: "POST", body: { kod: el("hesap-sil-kodu").value.trim(), sirketAdi: yazilan } })
                .then(function (result) {
                    el("hesap-sil-form").classList.add("hidden");
                    el("hesap-sil-kodu").value = "";
                    el("hesap-sil-ad").value = "";
                    showMessage(el("hesap-sil-mesaj"), (result && result.message) || "", true);
                    hesapDurumunuYukle();
                })
                .catch(function (error) { handleError(el("hesap-sil-mesaj"), error); });
        });

        el("silme-iptal").addEventListener("click", function () {
            api("/api/Account/sil-iptal", { method: "POST" }).then(function () {
                hesapDurumunuYukle();
            }).catch(function (error) { handleError(el("app-message"), error); });
        });

        el("uye-hesap-sil").addEventListener("click", function () {
            if (!window.confirm("Hesabınız kapatılacak ve kişisel bilgileriniz kaldırılacak. Devam edilsin mi?")) {
                return;
            }

            api("/api/Account", { method: "DELETE" }).then(function () {
                clearSession();
                location.reload();
            }).catch(function (error) { handleError(el("hesap-sil-mesaj"), error); });
        });
    }

    function bindSifreDegistir() {
        el("sifre-degistir-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);

            var mevcut = el("sifre-mevcut").value;
            var yeni = el("sifre-yeni").value;

            api("/api/Auth/sifre-degistir", {
                method: "POST",
                body: { mevcut: mevcut, yeni: yeni }
            }).then(function (result) {
                el("sifre-mevcut").value = "";
                el("sifre-yeni").value = "";
                showMessage(el("sifre-degistir-mesaj"), (result && result.message) || "Şifreniz değiştirildi.", true);
                el("gecici-sifre-uyari").classList.add("hidden");

                setTimeout(function () {
                    clearSession();
                    location.reload();
                }, 1500);
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("sifre-degistir-mesaj"), error);
            });
        });
    }

    function bindTanitim() {
        tanitimKartlariniCiz();
        tanitimDesteginiYukle();

        el("tanitim-demo").addEventListener("click", demoIleGir);
        el("tanitim-basla").addEventListener("click", kayitSekmesineGec);
        el("tanitim-plan-basla").addEventListener("click", kayitSekmesineGec);

        el("tanitim-davet-btn").addEventListener("click", function () {
            var kod = el("tanitim-davet-kod").value.trim();
            if (!kod) {
                showMessage(el("tanitim-mesaj"), "Önce davet kodunu yazın.", false);
                return;
            }

            el("register-davet").value = kod;
            showMessage(el("tanitim-mesaj"), "");
            kayitSekmesineGec();
        });
    }

    function bindLastik() {
        el("lastik-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();

            if (!state.selectedVehicleId) {
                showMessage(el("app-message"), "Önce bir araç seçin.", false);
                return;
            }

            var dis = el("lastik-dis").value;

            api("/api/Lastik", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    ad: el("lastik-ad").value,
                    mevsim: el("lastik-mevsim").value,
                    marka: el("lastik-marka").value,
                    ebat: el("lastik-ebat").value,
                    disDerinligiMm: dis === "" ? null : Number(dis),
                    takilmaTarihi: el("lastik-tarih").value,
                    takilmaKm: Number(el("lastik-km").value)
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Set takıldı.", true);
                el("lastik-form").reset();
                el("lastik-tarih").value = todayInput();
                loadLastik();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });
    }

    function loadDavet() {
        return api("/api/Davet").then(function (result) {
            var durum = (result && result.data) || {};
            el("davet-kod").textContent = durum.paylasimBaglantisi
                ? mutlakAdres(durum.paylasimBaglantisi)
                : (durum.kod || "");
            el("davet-ozet").textContent = durum.davetSayisi + " davet · " + durum.kazanilanAracHakki + "/" + durum.ekAracUstSiniri
                + " kazanılan araç hakkı · toplam limit " + durum.aracLimiti + " araç"
                + (durum.davetEden ? " · sizi " + durum.davetEden + " davet etti" : "");

            var tbody = el("davet-rows");
            clear(tbody);
            var davetliler = durum.davetliler || [];
            if (davetliler.length === 0) {
                emptyRow(tbody, 2, "Henüz davet edilen şirket yok.");
                return;
            }
            davetliler.forEach(function (satir) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", satir.sirketAdi));
                tr.appendChild(make("td", formatDate(satir.katilmaTarihi)));
                tbody.appendChild(tr);
            });
        }).catch(function () {
            el("davet-ozet").textContent = "Davet bilgisi görüntülenemedi.";
        });
    }

    function bindDavet() {
        el("davet-kopyala").addEventListener("click", function () {
            var metin = el("davet-kod").textContent;
            if (metin && navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(metin).then(function () {
                    showMessage(el("app-message"), "Davet bağlantısı kopyalandı.", true);
                });
            }
        });
    }

    var KAYNAK_ANAHTARI = "garajim_kaynak";

    function kaynakOku() {
        try {
            var ham = window.sessionStorage.getItem(KAYNAK_ANAHTARI);
            return ham ? JSON.parse(ham) : null;
        } catch (hata) {
            return null;
        }
    }

    function kaynagiUrldenYakala() {
        var parametreler = new URLSearchParams(window.location.search);
        var kaynak = parametreler.get("utm_source");

        if (!kaynak) {
            return;
        }

        try {
            window.sessionStorage.setItem(KAYNAK_ANAHTARI, JSON.stringify({
                kaynak: kaynak.slice(0, 50),
                detay: (parametreler.get("utm_content") || "").slice(0, 100)
            }));
        } catch (hata) {
            return;
        }
    }

    function kaynagiAlanlaraYaz() {
        var saklanan = kaynakOku();

        if (!saklanan) {
            return;
        }

        el("register-kaynak").value = saklanan.kaynak || "";
        el("register-kaynak-detay").value = saklanan.detay || "";
    }

    function davetKodunuUrldenOku() {

        var eslesme = /[?&]davet=([A-Za-z0-9]{1,12})/.exec(window.location.search);
        if (eslesme) {
            el("register-davet").value = eslesme[1].toUpperCase();
            switchAuthTab(false);
        }
    }

    var PLAN_TURLERI = [
        ["Filo", "Filo"],
        ["Bireysel", "Bireysel"]
    ];

    function loadPanelUyarisi() {
        var kutu = el("panel-uyari");
        return api("/api/Reports/dashboard").then(function (result) {
            var panel = (result && result.data) || {};
            var plakalar = panel.kisLastigiUyariPlakalari || [];

            if (!panel.kisLastigiUyarisi || plakalar.length === 0) {
                kutu.textContent = "";
                kutu.classList.add("hidden");
                return;
            }

            kutu.textContent = panel.kisLastigiUyarisi + " Eksik araçlar: " + plakalar.join(", ");
            kutu.classList.remove("hidden");
        }).catch(function () {
            kutu.textContent = "";
            kutu.classList.add("hidden");
        });
    }

    function bindPlan() {
        el("plan-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();

            api("/api/Plan/yukseltme-talebi", {
                method: "POST",
                body: {
                    istenenPlan: el("plan-istenen").value,
                    mesaj: el("plan-mesaj").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Talebiniz iletildi.", true);
                el("plan-mesaj").value = "";
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });
    }

    var USTA_KADEME = {
        EnSik: "En sık",
        Sik: "Sık",
        Nadir: "Nadir"
    };

    var USTA_ACILIYET = {
        Bugun: "Bugün",
        BuHafta: "Bu hafta",
        Bakimda: "Bakımda"
    };

    var ustaDurum = { sohbetId: null, surum: null, tanima: null };

    function ustaMetin(etiket, deger) {
        var satir = document.createElement("p");
        satir.className = "hint";
        var baslik = document.createElement("strong");
        baslik.textContent = etiket + ": ";
        satir.appendChild(baslik);
        satir.appendChild(document.createTextNode(deger));
        return satir;
    }

    function ustaKademeKarti(kademe) {
        var kart = document.createElement("div");
        kart.className = "usta-kademe kademe-" + (kademe.kademe || "").toLowerCase();

        var baslik = document.createElement("div");
        baslik.className = "usta-kademe-baslik";

        var rozet = document.createElement("span");
        rozet.className = "usta-rozet";
        rozet.textContent = USTA_KADEME[kademe.kademe] || kademe.kademe;
        baslik.appendChild(rozet);

        var neden = document.createElement("span");
        neden.textContent = kademe.neden || "";
        baslik.appendChild(neden);

        kart.appendChild(baslik);
        kart.appendChild(ustaMetin("Belirti uyumu", kademe.belirtiUyumu || "-"));
        kart.appendChild(ustaMetin("Evde kontrol", kademe.evdeKontrol || "-"));

        var maliyet = kademe.maliyetTl || [];
        var aralik = maliyet.length === 2 ? money(maliyet[0]) + " – " + money(maliyet[1]) : "-";
        kart.appendChild(ustaMetin("Tahmini maliyet", aralik));
        kart.appendChild(ustaMetin("Aciliyet", USTA_ACILIYET[kademe.aciliyet] || kademe.aciliyet || "-"));

        return kart;
    }

    function ustaYanitKarti(mesaj) {
        var yanit = mesaj.yanit || {};
        var kart = document.createElement("div");
        kart.className = "usta-mesaj usta-yanit";

        if (yanit.kirmiziCizgi) {
            var bant = document.createElement("div");
            bant.className = "usta-kirmizi";
            bant.textContent = "Güvenlik uyarısı — " + (yanit.ozet || "");
            kart.appendChild(bant);
        } else {
            var ozet = document.createElement("p");
            ozet.className = "usta-ozet";
            ozet.textContent = yanit.ozet || mesaj.metin;
            kart.appendChild(ozet);
        }

        (yanit.kademeler || []).forEach(function (kademe) {
            kart.appendChild(ustaKademeKarti(kademe));
        });

        var notlar = yanit.aracVerisindenNotlar || [];
        if (notlar.length > 0) {
            var notBaslik = make("h4", "Aracının verisinden");
            kart.appendChild(notBaslik);
            var liste = document.createElement("ul");
            notlar.forEach(function (metin) {
                liste.appendChild(make("li", metin));
            });
            kart.appendChild(liste);
        }

        if (yanit.ustayaBoyleAnlat) {
            var anlatKutu = document.createElement("div");
            anlatKutu.className = "usta-anlat";
            anlatKutu.appendChild(make("strong", "Ustaya böyle anlat"));
            anlatKutu.appendChild(make("p", yanit.ustayaBoyleAnlat));

            var kopyala = make("button", "Kopyala", "link-btn");
            kopyala.type = "button";
            kopyala.addEventListener("click", function () {
                if (navigator.clipboard && navigator.clipboard.writeText) {
                    navigator.clipboard.writeText(yanit.ustayaBoyleAnlat).then(function () {
                        showMessage(el("app-message"), "Metin kopyalandı.", true);
                    });
                }
            });
            anlatKutu.appendChild(kopyala);
            kart.appendChild(anlatKutu);
        }

        var sorular = yanit.takipSorulari || [];
        if (sorular.length > 0) {
            var cipKutu = document.createElement("div");
            cipKutu.className = "usta-cipler";
            sorular.forEach(function (soru) {
                var cip = make("button", soru, "usta-cip");
                cip.type = "button";
                cip.addEventListener("click", function () {
                    el("usta-soru").value = soru;
                    ustaSor(soru);
                });
                cipKutu.appendChild(cip);
            });
            kart.appendChild(cipKutu);
        }

        if (yanit.uyari) {
            var uyari = make("p", yanit.uyari, "usta-uyari");
            kart.appendChild(uyari);
        }

        kart.appendChild(ustaGeriBildirimKutusu(mesaj));
        return kart;
    }

    function ustaGeriBildirimKutusu(mesaj) {
        var kutu = document.createElement("div");
        kutu.className = "usta-geri";

        var durum = make("span", mesaj.geriBildirim === "Olumlu" ? "👍 işaretlendi"
            : (mesaj.geriBildirim === "Olumsuz" ? "👎 işaretlendi" : ""), "hint");

        var olumlu = make("button", "👍", "link-btn");
        olumlu.type = "button";
        olumlu.addEventListener("click", function () { ustaGeriBildirim(mesaj.id, "Olumlu", null); });

        var olumsuz = make("button", "👎", "link-btn");
        olumsuz.type = "button";
        olumsuz.addEventListener("click", function () { ustaGeriBildirim(mesaj.id, "Olumsuz", null); });

        var cozum = make("button", "Bunu hangi bakım çözdü?", "link-btn");
        cozum.type = "button";
        cozum.addEventListener("click", function () { ustaCozumSec(mesaj.id, kutu); });

        kutu.appendChild(olumlu);
        kutu.appendChild(olumsuz);
        kutu.appendChild(cozum);
        kutu.appendChild(durum);
        return kutu;
    }

    function ustaCozumSec(mesajId, kutu) {
        if (!ustaDurum.sohbetId) {
            return;
        }

        api("/api/Usta/sohbet/" + ustaDurum.sohbetId + "/bakimlar").then(function (result) {
            var bakimlar = (result && result.data) || [];
            var mevcut = kutu.querySelector(".usta-cozum-liste");
            if (mevcut) {
                kutu.removeChild(mevcut);
            }

            var liste = document.createElement("div");
            liste.className = "usta-cozum-liste";

            if (bakimlar.length === 0) {
                liste.appendChild(make("span", "Son 90 günde bu araca ait bakım kaydı yok.", "hint"));
                kutu.appendChild(liste);
                return;
            }

            bakimlar.forEach(function (bakim) {
                var dugme = make("button",
                    formatDate(bakim.tarih) + " · " + labelOf(MAINTENANCE_TYPES, bakim.tur) + " · " + money(bakim.tutar),
                    "usta-cip");
                dugme.type = "button";
                dugme.addEventListener("click", function () {
                    ustaGeriBildirim(mesajId, "Olumlu", bakim.id);
                });
                liste.appendChild(dugme);
            });

            kutu.appendChild(liste);
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function ustaGeriBildirim(mesajId, deger, bakimId) {
        clearMessages();
        api("/api/Usta/mesaj/" + mesajId + "/geri-bildirim", {
            method: "POST",
            body: { geriBildirim: deger, cozumBakimId: bakimId }
        }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Geri bildirim alındı.", true);
            ustaSohbetYukle(ustaDurum.sohbetId);
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function ustaSoruKarti(mesaj) {
        var kart = document.createElement("div");
        kart.className = "usta-mesaj usta-soru";
        kart.appendChild(make("p", mesaj.metin));
        return kart;
    }

    function ustaAkisCiz(mesajlar) {
        var akis = el("usta-akis");
        clear(akis);

        if (mesajlar.length === 0) {
            akis.appendChild(make("p", "Sorunu yaz, usta aracının kayıtlarına bakarak cevaplasın.", "hint"));
            return;
        }

        mesajlar.forEach(function (mesaj) {
            akis.appendChild(mesaj.rol === "Kullanici" ? ustaSoruKarti(mesaj) : ustaYanitKarti(mesaj));
        });

        akis.scrollTop = akis.scrollHeight;
    }

    function ustaSohbetYukle(sohbetId) {
        if (!sohbetId) {
            ustaAkisCiz([]);
            return Promise.resolve();
        }

        return api("/api/Usta/sohbet/" + sohbetId).then(function (result) {
            var sohbet = (result && result.data) || {};
            ustaDurum.sohbetId = sohbet.id;
            ustaAkisCiz(sohbet.mesajlar || []);
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function ustaGecmisYukle() {
        if (!state.selectedVehicleId) {
            return Promise.resolve();
        }

        return api("/api/Usta/sohbet?aracId=" + state.selectedVehicleId).then(function (result) {
            var sohbetler = (result && result.data) || [];
            var select = el("usta-gecmis");
            clear(select);

            var bos = document.createElement("option");
            bos.value = "";
            bos.textContent = sohbetler.length === 0 ? "Geçmiş sohbet yok" : "Geçmiş sohbetler";
            select.appendChild(bos);

            sohbetler.forEach(function (sohbet) {
                var secenek = document.createElement("option");
                secenek.value = String(sohbet.id);
                secenek.textContent = sohbet.baslik;
                select.appendChild(secenek);
            });

            if (ustaDurum.sohbetId) {
                select.value = String(ustaDurum.sohbetId);
            }
        }).catch(function () {
            clear(el("usta-gecmis"));
        });
    }

    function ustaSohbetAc() {
        if (!state.selectedVehicleId) {
            showMessage(el("app-message"), "Önce bir araç seçin.", false);
            return Promise.reject(new Error("arac yok"));
        }

        return api("/api/Usta/sohbet", {
            method: "POST",
            body: { vehicleId: state.selectedVehicleId }
        }).then(function (result) {
            ustaDurum.sohbetId = result.data.id;
            ustaAkisCiz([]);
            return ustaGecmisYukle();
        });
    }

    function ustaSor(metin) {
        var soru = (metin || el("usta-soru").value || "").trim();
        if (soru.length === 0 || state.ustaGonderiyor) {
            return;
        }

        clearMessages();
        gonderimKilitle("usta-sor", true, "Usta düşünüyor…");
        state.ustaGonderiyor = true;

        var gonder = function () {
            return api("/api/Usta/sohbet/" + ustaDurum.sohbetId + "/mesaj", {
                method: "POST",
                body: { metin: soru }
            }).then(function (result) {
                el("usta-soru").value = "";
                el("usta-kalan").textContent = "Bugün kalan hak: " + result.data.kalanGunlukHak
                    + " · bu sohbette kalan: " + result.data.kalanSohbetMesaji;
                return ustaSohbetYukle(ustaDurum.sohbetId);
            });
        };

        var zincir = ustaDurum.sohbetId ? gonder() : ustaSohbetAc().then(gonder);

        zincir.catch(function (error) {
            if (error && error.kod === "ONAY_GEREKLI") {
                ustaOnayGoster(true);
                return;
            }
            handleError(el("app-message"), error);
        }).then(function () {
            state.ustaGonderiyor = false;
            gonderimKilitle("usta-sor", false);
        });
    }

    function ustaOnayGoster(gerekli) {
        el("usta-onay-kutusu").classList.toggle("hidden", !gerekli);
        el("usta-govde").classList.toggle("hidden", gerekli);
    }

    function ustaOnayDurumu() {
        return api("/api/Usta/onay").then(function (result) {
            var durum = (result && result.data) || {};
            ustaDurum.surum = durum.guncelSurum;
            el("usta-onay-metni").textContent = "AI Usta sorularınızı ve aracınızın bakım/yakıt/evrak özetini yanıt üretmek için "
                + "Google Gemini servisine gönderir. Sohbetleriniz 24 ay saklanır, dilediğinizde silebilirsiniz. "
                + "Verilen yanıt tahmindir, teşhis değildir; uygulanmasından doğan sonuçlardan Garajım sorumlu değildir. "
                + "Onay metni sürümü: " + (durum.guncelSurum || "-");
            ustaOnayGoster(durum.onayGerekli);
            return durum;
        });
    }

    function loadUsta() {
        ustaDurum.sohbetId = null;
        return ustaOnayDurumu().then(function (durum) {
            if (durum.onayGerekli) {
                return null;
            }
            ustaAkisCiz([]);
            el("usta-kalan").textContent = "";
            return ustaGecmisYukle();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function ustaSesliBaslat() {
        var Tanima = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (!Tanima) {
            return;
        }

        if (ustaDurum.tanima) {
            ustaDurum.tanima.stop();
            ustaDurum.tanima = null;
            return;
        }

        var tanima = new Tanima();
        tanima.lang = "tr-TR";
        tanima.interimResults = false;
        tanima.maxAlternatives = 1;

        tanima.onresult = function (olay) {
            el("usta-soru").value = olay.results[0][0].transcript;
        };
        tanima.onerror = function () {
            showMessage(el("app-message"), "Ses tanınamadı, yazarak deneyin.", false);
        };
        tanima.onend = function () {
            ustaDurum.tanima = null;
        };

        ustaDurum.tanima = tanima;
        tanima.start();
    }

    function bindUsta() {
        el("usta-onay-btn").addEventListener("click", function () {
            if (!el("usta-onay-kutu").checked) {
                showMessage(el("app-message"), "Devam etmek için kutuyu işaretleyin.", false);
                return;
            }

            clearMessages();
            api("/api/Usta/onay", { method: "POST", body: { metinSurumu: ustaDurum.surum } }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Onayınız kaydedildi.", true);
                loadUsta();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });

        el("usta-yeni-sohbet").addEventListener("click", function () {
            clearMessages();
            ustaSohbetAc().catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("usta-gecmis").addEventListener("change", function () {
            var deger = el("usta-gecmis").value;
            ustaDurum.sohbetId = deger ? Number(deger) : null;
            ustaSohbetYukle(ustaDurum.sohbetId);
        });

        el("usta-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            ustaSor(null);
        });

        var sesliDugme = el("usta-sesli");
        if (window.SpeechRecognition || window.webkitSpeechRecognition) {
            sesliDugme.classList.remove("hidden");
            sesliDugme.addEventListener("click", ustaSesliBaslat);
        }
    }

    function bindAuth() {
        el("tab-login").addEventListener("click", function () { switchAuthTab(true); });
        el("tab-register").addEventListener("click", function () { switchAuthTab(false); });

        el("login-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();
            api("/api/Auth/login", {
                method: "POST",
                body: {
                    email: el("login-email").value,
                    password: el("login-password").value
                }
            }).then(function (result) {
                saveSession(result.data.token, result.data);
                el("login-password").value = "";
                enterApp();
            }).catch(function (error) {
                if (error && error.kod === "EMAIL_DOGRULANMADI") {
                    el("login-password").value = "";
                    dogrulamaEkraniniAc(el("login-email").value.trim(), error.message);
                    return;
                }

                handleError(el("auth-message"), error);
            });
        });

        el("register-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();
            api("/api/Auth/register", {
                method: "POST",
                body: {
                    fullName: el("register-name").value,
                    email: el("register-email").value,
                    password: el("register-password").value,
                    companyName: el("register-company").value,
                    davetKodu: el("register-davet").value,
                    kaynak: el("register-kaynak").value,
                    kaynakDetay: el("register-kaynak-detay").value
                }
            }).then(function (result) {
                el("register-password").value = "";
                dogrulamaEkraniniAc(result.data.email, result.message);
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("auth-message"), error);
            });
        });

        el("logout-btn").addEventListener("click", function () {
            goToLogin("Çıkış yapıldı.");
        });
    }

    function bindTeam() {
        el("team-btn").addEventListener("click", function () {
            var box = el("team-box");
            var acilacak = box.classList.contains("hidden");
            box.classList.toggle("hidden", !acilacak);
            el("team-credential").classList.add("hidden");
            if (acilacak) {
                loadTeam();
            }
        });

        el("team-close").addEventListener("click", function () {
            el("team-box").classList.add("hidden");
        });

        el("team-sifre-kopyala").addEventListener("click", function () {
            baglantiKopyala(el("team-credential-password").textContent);
        });

        el("team-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();
            api("/api/Team", {
                method: "POST",
                body: {
                    fullName: el("team-name").value,
                    email: el("team-email").value,
                    role: el("team-role").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Üye eklendi.", true);
                el("team-form").reset();
                el("team-credential").classList.remove("hidden");
                el("team-credential-email").textContent = result.data.email;
                el("team-credential-password").textContent = result.data.temporaryPassword;
                el("team-credential").scrollIntoView({ block: "nearest" });
                loadTeam();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });
    }

    function bindAssignment() {
        el("assignment-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();
            var userId = Number(el("assignment-user").value);
            if (!userId) {
                showMessage(el("app-message"), "Önce ekibe bir kullanıcı ekleyin.");
                return;
            }
            var devir = el("assignment-submit").textContent === "Devret";
            api(devir ? "/api/Assignments/transfer" : "/api/Assignments", {
                method: devir ? "PUT" : "POST",
                body: { vehicleId: state.selectedVehicleId, userId: userId }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Zimmet güncellendi.", true);
                loadAssignments();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });

        el("assignment-end").addEventListener("click", function () {
            clearMessages();
            api("/api/Assignments/end", {
                method: "PUT",
                body: { vehicleId: state.selectedVehicleId }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Zimmet sonlandırıldı.", true);
                loadAssignments();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });
    }

    var RECEIPT_TYPES = [
        ["Yakit", "Yakıt"],
        ["Bakim", "Bakım"],
        ["Masraf", "Masraf"]
    ];

    function receiptTypeChanged() {
        var tur = el("receipt-type").value;
        el("receipt-liters-box").classList.toggle("hidden", tur !== "Yakit");
        el("receipt-maintenance-box").classList.toggle("hidden", tur !== "Bakim");
        el("receipt-category-box").classList.toggle("hidden", tur !== "Masraf");
    }

    function markNull(node, isNull) {
        node.classList.toggle("needs-value", isNull);
    }

    function fillReceiptVehicles(selectedId) {
        var select = el("receipt-vehicle");
        clear(select);
        state.vehicles.forEach(function (vehicle) {
            var option = document.createElement("option");
            option.value = String(vehicle.id);
            option.textContent = vehicle.plate + " - " + vehicle.brand + " " + vehicle.model;
            select.appendChild(option);
        });
        if (selectedId) {
            select.value = String(selectedId);
        } else if (state.selectedVehicleId) {
            select.value = String(state.selectedVehicleId);
        }
    }

    function showReceiptReview(draft) {
        state.receiptDraft = draft;
        el("receipt-review").classList.remove("hidden");

        var guven = Math.round((Number(draft.guvenSkoru) || 0) * 100);
        el("receipt-confidence").textContent = guven > 0
            ? "Okuma güveni %" + guven + ". Boş kalan alanları siz doldurun."
            : "Fiş okunamadı; alanları doldurup onaylayabilirsiniz.";

        fillReceiptVehicles(draft.vehicleId);

        el("receipt-type").value = draft.tahminiTur === "Bilinmiyor" ? "Masraf" : draft.tahminiTur;
        receiptTypeChanged();

        var tarih = el("receipt-date");
        tarih.value = draft.tarih ? String(draft.tarih).slice(0, 10) : "";
        markNull(tarih, !draft.tarih);

        var tutar = el("receipt-amount");
        tutar.value = draft.toplamTutar === null || draft.toplamTutar === undefined ? "" : draft.toplamTutar;
        markNull(tutar, draft.toplamTutar === null || draft.toplamTutar === undefined);

        var kilometre = el("receipt-km");
        kilometre.value = draft.km === null || draft.km === undefined ? "" : draft.km;
        markNull(kilometre, draft.km === null || draft.km === undefined);

        var litre = el("receipt-liters");
        litre.value = draft.litre === null || draft.litre === undefined ? "" : draft.litre;
        markNull(litre, draft.litre === null || draft.litre === undefined);

        el("receipt-note").value = "";
        renderReceiptParts(draft.parcalar);
    }

    function hideReceiptReview() {
        state.receiptDraft = null;
        el("receipt-review").classList.add("hidden");
    }

    function loadPendingReceipts() {
        return api("/api/Receipts?durum=Bekliyor").then(function (result) {
            var rows = (result && result.data) || [];
            var liste = el("receipt-pending");
            clear(liste);

            state.bekleyenFisSayisi = rows.length;
            fisDugmesiniTazele();

            var rozet = el("receipt-badge");
            rozet.textContent = rows.length ? String(rows.length) : "";
            rozet.classList.toggle("hidden", rows.length === 0);
            el("receipt-pending-title").classList.toggle("hidden", rows.length === 0);

            if (rows.length === 0) {
                var bos = document.createElement("li");
                bos.className = "bos-satir";
                bos.appendChild(bosDurumKutusu("fis"));
                liste.appendChild(bos);
            }

            rows.forEach(function (item) {
                var li = document.createElement("li");
                var ozet = item.orijinalAd;
                if (item.toplamTutar !== null && item.toplamTutar !== undefined) {
                    ozet += " · " + money(item.toplamTutar);
                }
                li.appendChild(make("span", ozet));

                var actions = make("span", "", "row-actions");
                var ac = make("button", "İncele", "link-btn");
                ac.type = "button";
                ac.addEventListener("click", function () {
                    el("receipt-box").classList.remove("hidden");
                    showReceiptReview(item);
                });
                actions.appendChild(ac);
                li.appendChild(actions);
                liste.appendChild(li);
            });
        }).catch(function () {
            el("receipt-badge").classList.add("hidden");
        });
    }

    var PART_TYPES = [
        ["MotorYagi", "Motor yağı"],
        ["YagFiltresi", "Yağ filtresi"],
        ["HavaFiltresi", "Hava filtresi"],
        ["PolenFiltresi", "Polen filtresi"],
        ["YakitFiltresi", "Yakıt filtresi"],
        ["FrenBalatasiOn", "Ön fren balatası"],
        ["FrenBalatasiArka", "Arka fren balatası"],
        ["FrenDiskiOn", "Ön fren diski"],
        ["FrenDiskiArka", "Arka fren diski"],
        ["Buji", "Buji"],
        ["TrigerSeti", "Triger seti"],
        ["VKayisi", "V kayışı"],
        ["Aku", "Akü"],
        ["Lastik", "Lastik"],
        ["Amortisor", "Amortisör"],
        ["Silecek", "Silecek"],
        ["Antifriz", "Antifriz"],
        ["FrenHidroligi", "Fren hidroliği"],
        ["SanzimanYagi", "Şanzıman yağı"],
        ["Devirdaim", "Devirdaim"],
        ["RotBasi", "Rot başı"],
        ["Salincak", "Salıncak"],
        ["Debriyaj", "Debriyaj"],
        ["Diger", "Diğer"]
    ];

    var PART_STATUS = {
        Iyi: "İyi",
        Yaklasiyor: "Yaklaşıyor",
        Gecti: "Geçti"
    };

    function addPartRow(kutu, deger) {
        var satir = make("div", "", "part-row");

        var tur = document.createElement("select");
        fillSelect(tur, PART_TYPES);
        tur.className = "part-type";
        tur.setAttribute("aria-label", "Parça türü");
        if (deger && deger.parcaTuru) {
            tur.value = deger.parcaTuru;
        }
        satir.appendChild(tur);

        var aciklama = document.createElement("input");
        aciklama.type = "text";
        aciklama.className = "part-desc";
        aciklama.placeholder = "Açıklama";
        aciklama.setAttribute("aria-label", "Parça açıklaması");
        aciklama.value = deger && deger.aciklama ? deger.aciklama : "";
        satir.appendChild(aciklama);

        var adet = document.createElement("input");
        adet.type = "number";
        adet.min = "1";
        adet.className = "part-qty";
        adet.placeholder = "Adet";
        adet.setAttribute("aria-label", "Parça adedi");
        adet.value = deger && deger.adet ? deger.adet : 1;
        satir.appendChild(adet);

        var tutar = document.createElement("input");
        tutar.type = "text";
        tutar.inputMode = "decimal";
        tutar.className = "part-cost";
        tutar.placeholder = "Tutar";
        tutar.setAttribute("aria-label", "Parça tutarı (TL)");
        tutar.value = deger && deger.tutar !== null && deger.tutar !== undefined ? deger.tutar : "";
        satir.appendChild(tutar);

        var sil = make("button", "Sil", "link-btn");
        sil.type = "button";
        sil.addEventListener("click", function () { kutu.removeChild(satir); });
        satir.appendChild(sil);

        kutu.appendChild(satir);
    }

    function readPartRows(kutu) {
        var parcalar = [];

        Array.prototype.forEach.call(kutu.querySelectorAll(".part-row"), function (satir) {
            var aciklama = satir.querySelector(".part-desc").value.trim();
            var hamTutar = satir.querySelector(".part-cost").value.trim();
            var sayi = hamTutar.length > 0 ? sayiOku(hamTutar) : NaN;
            var tutar = isNaN(sayi) ? null : sayi;
            var doluMu = aciklama.length > 0 || tutar !== null;

            if (!doluMu) {
                return;
            }

            parcalar.push({
                parcaTuru: satir.querySelector(".part-type").value,
                aciklama: aciklama,
                adet: Number(satir.querySelector(".part-qty").value) || 1,
                tutar: tutar,
                marka: null
            });
        });

        return parcalar;
    }

    function renderReceiptParts(parcalar) {
        var kutu = el("receipt-parts-box");
        var liste = el("receipt-parts");
        clear(liste);

        var varMi = parcalar && parcalar.length > 0;
        kutu.classList.toggle("hidden", !varMi);

        if (!varMi) {
            return;
        }

        parcalar.forEach(function (parca) {
            var li = document.createElement("li");
            var metin = labelOf(PART_TYPES, parca.parcaTuru) + " — " + (parca.aciklama || "");
            if (parca.tutar !== null && parca.tutar !== undefined) {
                metin += " · " + money(parca.tutar);
            }
            li.appendChild(make("span", metin));
            liste.appendChild(li);
        });
    }

    function loadPartMemory() {
        if (!state.selectedVehicleId) {
            return;
        }
        var tbody = el("parca-rows");
        api("/api/Vehicles/" + state.selectedVehicleId + "/parca-hafizasi").then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);

            if (rows.length === 0) {
                emptyRow(tbody, 7, "Bakım kayıtlarında parça yok.");
                return;
            }

            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", item.parcaAdi));
                tr.appendChild(make("td", formatDate(item.sonDegisimTarihi) + (item.sonDegisimKm ? " · " + km(item.sonDegisimKm) : "")));
                tr.appendChild(make("td", item.degisimSayisi));
                tr.appendChild(make("td", money(item.toplamTutar)));

                var sonraki = [];
                if (item.sonrakiTahminiKm) {
                    sonraki.push(km(item.sonrakiTahminiKm));
                }
                if (item.sonrakiTahminiTarih) {
                    sonraki.push(formatDate(item.sonrakiTahminiTarih));
                }
                tr.appendChild(make("td", sonraki.length ? sonraki.join(" / ") : "-"));

                tr.appendChild(make("td", PART_STATUS[item.durum] || item.durum, "durum-" + item.durum.toLowerCase()));

                var hucre = document.createElement("td");
                if (item.sonrakiTahminiKm || item.sonrakiTahminiTarih) {
                    var dugme = make("button", "Hatırlatma oluştur", "link-btn");
                    dugme.type = "button";
                    dugme.addEventListener("click", function () { createPartReminder(item.parcaTuru); });
                    hucre.appendChild(dugme);
                }
                tr.appendChild(hucre);

                tbody.appendChild(tr);
            });
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function createPartReminder(parcaTuru) {
        clearMessages();
        api("/api/Vehicles/" + state.selectedVehicleId + "/parca-hafizasi/" + parcaTuru + "/hatirlatma", { method: "POST" })
            .then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Hatırlatma eklendi.", true);
            })
            .catch(function (error) {
                handleError(el("app-message"), error);
            });
    }

    function bindParts() {
        el("maintenance-part-add").addEventListener("click", function () {
            addPartRow(el("maintenance-parts"), null);
        });
    }

    var EVRAK_TYPES = [
        ["Muayene", "Muayene"],
        ["TrafikSigortasi", "Trafik sigortası"],
        ["Kasko", "Kasko"],
        ["EgzozEmisyon", "Egzoz emisyon"],
        ["KisLastigi", "Kış lastiği"],
        ["Ehliyet", "Ehliyet"],
        ["SRC", "SRC belgesi"],
        ["Psikoteknik", "Psikoteknik"]
    ];

    var EVRAK_STATUS = {
        Iyi: "İyi",
        Yaklasiyor: "Yaklaşıyor",
        Gecti: "Geçti"
    };

    function evrakSatiri(item) {
        var tr = document.createElement("tr");

        if (!item.aktif) {
            tr.className = "evrak-pasif";
        }

        tr.appendChild(make("td", item.evrakAdi));
        tr.appendChild(make("td", item.plaka || item.kullaniciAdi || "-"));
        tr.appendChild(make("td", formatDate(item.bitisTarihi)));
        tr.appendChild(make("td", kalanGunMetni(item)));
        tr.appendChild(make("td", item.saglayici || "-"));

        tr.appendChild(item.aktif
            ? make("td", EVRAK_STATUS[item.durum] || item.durum, "durum-" + item.durum.toLowerCase())
            : make("td", "Geçersiz", "durum-pasif"));

        var hucre = make("td", "", "row-actions");

        if (item.aktif && canManage()) {
            var yenile = make("button", "Yenile", "link-btn");
            yenile.type = "button";
            yenile.addEventListener("click", function () { evrakYenile(item.id); });
            hucre.appendChild(yenile);

            var duzenle = make("button", "Düzenle", "link-btn");
            duzenle.type = "button";
            duzenle.addEventListener("click", function () { evrakiDuzenle(item); });
            hucre.appendChild(duzenle);
        }

        tr.appendChild(hucre);

        return tr;
    }

    function renderEvrakRows(rows) {
        var tbody = el("evrak-rows");
        clear(tbody);

        if (rows.length === 0) {
            bosSatir(tbody, 7, "evrak");
            return;
        }

        rows.slice().sort(function (a, b) {
            if (a.aktif !== b.aktif) {
                return a.aktif ? -1 : 1;
            }

            return String(a.bitisTarihi).localeCompare(String(b.bitisTarihi));
        }).forEach(function (item) {
            tbody.appendChild(evrakSatiri(item));
        });
    }

    var evrakDenetimi = listeDenetimi({
        anahtar: "evrak",
        cubukId: "evrak-liste-araclar",
        govdeId: "evrak-rows",
        bosAnahtar: "evrak",
        sutunSayisi: 7,
        varsayilanAlan: "durum",
        tarihSuzgeci: false,
        aramaEtiketi: "Evraklarda ara",
        aramaIpucu: "Sağlayıcı, poliçe ya da not",
        siralamalar: [
            { baslikId: "evrak-bas-tur", alan: "tur" },
            { baslikId: "evrak-bas-tarih", alan: "tarih" },
            { baslikId: "evrak-bas-saglayici", alan: "saglayici" },
            { baslikId: "evrak-bas-durum", alan: "durum" }
        ],
        uc: function () {
            return state.selectedVehicleId ? "/api/Evrak?vehicleId=" + state.selectedVehicleId : "/api/Evrak";
        },
        satir: evrakSatiri
    });

    function kalanGunMetni(item) {
        if (!item.aktif) {
            return "—";
        }

        if (item.kalanGun < 0) {
            return Math.abs(item.kalanGun) + " gün geçti";
        }

        return item.kalanGun + " gün";
    }

    function evrakiDuzenle(kayit) {
        state.duzenlenenEvrakId = kayit.id;

        el("evrak-tur").value = kayit.evrakTuru;
        el("evrak-baslangic").value = kayit.baslangicTarihi ? String(kayit.baslangicTarihi).slice(0, 10) : "";
        el("evrak-bitis").value = kayit.bitisTarihi ? String(kayit.bitisTarihi).slice(0, 10) : "";
        el("evrak-saglayici").value = kayit.saglayici || "";
        el("evrak-police").value = kayit.policeNo || "";
        el("evrak-not").value = kayit.not || "";

        evrakFormModu();
        tescilUyarisiniGuncelle();
    }

    function evrakFormModu() {
        var duzenleme = state.duzenlenenEvrakId !== null;

        el("evrak-submit").textContent = duzenleme ? "Evrakı güncelle" : "Evrak ekle";
        el("evrak-vazgec").classList.toggle("hidden", !duzenleme);
    }

    function evrakFormunuSifirla() {
        state.duzenlenenEvrakId = null;
        el("evrak-form").reset();
        evrakFormModu();
    }

    function loadEvrak() {
        return listeDenetimiKur(evrakDenetimi);
    }

    function loadEvrakAy() {
        var ay = el("evrak-ay").value;
        if (!ay) {
            showMessage(el("app-message"), "Önce bir ay seçin.");
            return;
        }
        clearMessages();
        api("/api/Evrak/takvim?ay=" + ay).then(function (result) {
            renderEvrakRows((result && result.data) || []);
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function evrakYenile(id) {
        clearMessages();
        api("/api/Evrak/" + id + "/yenile", { method: "POST" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Evrak yenilendi.", true);
            loadEvrak();
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function bindEvrak() {
        el("evrak-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();

            if (!state.selectedVehicleId) {
                showMessage(el("app-message"), "Önce bir araç seçin.");
                return;
            }

            var bitis = el("evrak-bitis").value;
            var baslangic = el("evrak-baslangic").value;

            var duzenleme = state.duzenlenenEvrakId !== null;

            api(duzenleme ? "/api/Evrak/" + state.duzenlenenEvrakId : "/api/Evrak", {
                method: duzenleme ? "PUT" : "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    evrakTuru: el("evrak-tur").value,
                    baslangicTarihi: baslangic ? baslangic : null,
                    bitisTarihi: bitis ? bitis : null,
                    saglayici: el("evrak-saglayici").value,
                    policeNo: el("evrak-police").value,
                    not: el("evrak-not").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Evrak eklendi.", true);
                evrakFormunuSifirla();
                loadEvrak();
                kurulumDurumunuYukle();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });

        el("evrak-vazgec").addEventListener("click", evrakFormunuSifirla);
        el("evrak-ay-btn").addEventListener("click", loadEvrakAy);
        el("evrak-hepsi-btn").addEventListener("click", function () {
            clearMessages();
            loadEvrak();
        });
    }

    var IMPORT_ALANLARI = [
        { key: "tarih", label: "Tarih" },
        { key: "km", label: "Kilometre" },
        { key: "litre", label: "Litre" },
        { key: "tutar", label: "Tutar" },
        { key: "birimfiyat", label: "Birim fiyat" },
        { key: "kategori", label: "Kategori" },
        { key: "aciklama", label: "Açıklama" },
        { key: "servis", label: "Servis" },
        { key: "tamdolum", label: "Tam dolum" }
    ];

    var importDurum = { onizleme: null, hatalar: [] };

    function importAlanlari(kayitTuru) {
        if (kayitTuru === "Yakit") {
            return ["tarih", "km", "litre", "tutar", "birimfiyat", "tamdolum"];
        }
        if (kayitTuru === "Bakim") {
            return ["tarih", "km", "tutar", "servis", "aciklama"];
        }
        return ["tarih", "tutar", "kategori", "aciklama"];
    }

    function importZorunlular(kayitTuru) {
        return kayitTuru === "Yakit" ? ["tarih", "tutar", "litre"] : ["tarih", "tutar"];
    }

    function importAlanAdi(key) {
        for (var i = 0; i < IMPORT_ALANLARI.length; i++) {
            if (IMPORT_ALANLARI[i].key === key) {
                return IMPORT_ALANLARI[i].label;
            }
        }
        return key;
    }

    function fillImportVehicles() {
        var select = el("import-arac");
        var onceki = select.value;
        clear(select);
        state.vehicles.forEach(function (vehicle) {
            var option = document.createElement("option");
            option.value = String(vehicle.id);
            option.textContent = vehicle.plate + " - " + vehicle.brand + " " + vehicle.model;
            select.appendChild(option);
        });
        if (onceki) {
            select.value = onceki;
        } else if (state.selectedVehicleId) {
            select.value = String(state.selectedVehicleId);
        }
    }

    function renderImportEslesme(onizleme) {
        var kap = el("import-eslesme");
        clear(kap);

        var zorunlular = importZorunlular(onizleme.kayitTuru);

        importAlanlari(onizleme.kayitTuru).forEach(function (alan) {
            var hucre = document.createElement("div");

            var etiket = document.createElement("label");
            etiket.setAttribute("for", "import-alan-" + alan);
            etiket.textContent = importAlanAdi(alan) + (zorunlular.indexOf(alan) >= 0 ? " *" : "");
            hucre.appendChild(etiket);

            var select = document.createElement("select");
            select.id = "import-alan-" + alan;
            select.setAttribute("data-alan", alan);

            var bos = document.createElement("option");
            bos.value = "";
            bos.textContent = "— eşlenmedi —";
            select.appendChild(bos);

            onizleme.basliklar.forEach(function (baslik, sira) {
                var option = document.createElement("option");
                option.value = String(sira);
                option.textContent = baslik || ("Sütun " + (sira + 1));
                select.appendChild(option);
            });

            var onerilen = onizleme.onerilenEslesme && onizleme.onerilenEslesme[alan];
            select.value = (onerilen === 0 || onerilen) ? String(onerilen) : "";

            hucre.appendChild(select);
            kap.appendChild(hucre);
        });
    }

    function renderImportOrnek(onizleme) {
        var baslikSatiri = el("import-ornek-baslik");
        clear(baslikSatiri);
        onizleme.basliklar.forEach(function (baslik, sira) {
            var th = document.createElement("th");
            th.textContent = baslik || ("Sütun " + (sira + 1));
            baslikSatiri.appendChild(th);
        });

        var govde = el("import-ornek-govde");
        clear(govde);
        (onizleme.ornekSatirlar || []).forEach(function (satir) {
            var tr = document.createElement("tr");
            onizleme.basliklar.forEach(function (baslik, sira) {
                var td = document.createElement("td");
                td.textContent = satir[sira] || "";
                tr.appendChild(td);
            });
            govde.appendChild(tr);
        });
    }

    function renderImportHatalar(hatalar) {
        importDurum.hatalar = hatalar || [];
        var govde = el("import-hata-govde");
        clear(govde);

        importDurum.hatalar.forEach(function (hata) {
            var tr = document.createElement("tr");

            var no = document.createElement("td");
            no.textContent = String(hata.satirNo);
            tr.appendChild(no);

            var sebep = document.createElement("td");
            sebep.textContent = hata.sebep;
            tr.appendChild(sebep);

            var icerik = document.createElement("td");
            icerik.textContent = hata.icerik || "";
            tr.appendChild(icerik);

            govde.appendChild(tr);
        });

        el("import-hata-indir").classList.toggle("hidden", importDurum.hatalar.length === 0);
    }

    function importEslesmeTopla() {
        var eslesme = {};
        var selectler = el("import-eslesme").querySelectorAll("select");
        for (var i = 0; i < selectler.length; i++) {
            var deger = selectler[i].value;
            if (deger !== "") {
                eslesme[selectler[i].getAttribute("data-alan")] = Number(deger);
            }
        }
        return eslesme;
    }

    function importSifirla() {
        importDurum.onizleme = null;
        importDurum.hatalar = [];
        el("import-dosya").value = "";
        el("import-onizleme").classList.add("hidden");
        el("import-sonuc").classList.add("hidden");
        clear(el("import-eslesme"));
        clear(el("import-ornek-baslik"));
        clear(el("import-ornek-govde"));
        clear(el("import-hata-govde"));
    }

    function importDosya() {
        var girdi = el("import-dosya");
        return girdi.files && girdi.files.length > 0 ? girdi.files[0] : null;
    }

    function importUygula(dryRun) {
        clearMessages();

        var dosya = importDosya();
        if (!dosya) {
            showMessage(el("app-message"), "Önce bir CSV dosyası seçin.", false);
            return;
        }

        var aracId = el("import-arac").value;
        if (!aracId) {
            showMessage(el("app-message"), "Önce bir araç seçin.", false);
            return;
        }

        var kayitTuru = el("import-tur").value;
        var eslesme = importEslesmeTopla();

        var eksik = importZorunlular(kayitTuru).filter(function (alan) {
            return !(alan in eslesme);
        });
        if (eksik.length > 0) {
            showMessage(el("app-message"), "Zorunlu sütunlar eşlenmeli: " + eksik.map(importAlanAdi).join(", "), false);
            return;
        }

        var form = new FormData();
        form.append("file", dosya);
        form.append("kayitTuru", kayitTuru);
        form.append("vehicleId", aracId);
        form.append("eslesme", JSON.stringify(eslesme));
        form.append("dryRun", dryRun ? "true" : "false");

        api("/api/Import/uygula", { method: "POST", body: form }).then(function (result) {
            var veri = result.data;
            el("import-sonuc").classList.remove("hidden");
            el("import-sonuc-ozet").textContent = (veri.dryRun ? "Deneme: " : "Sonuç: ")
                + veri.eklenen + (veri.dryRun ? " kayıt eklenecek, " : " kayıt eklendi, ")
                + veri.atlanan + " mükerrer atlandı, "
                + (veri.hatali || []).length + " satır hatalı.";
            renderImportHatalar(veri.hatali);

            showMessage(el("app-message"), (result && result.message) || "İşlem tamamlandı.", true);

            if (!veri.dryRun && veri.eklenen > 0) {
                loadVehicles();
                loadActiveTab();
            }
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function importHatalariIndir() {
        if (importDurum.hatalar.length === 0) {
            return;
        }

        var satirlar = ["satir;sebep;icerik"];
        importDurum.hatalar.forEach(function (hata) {
            satirlar.push(hata.satirNo + ";" + (hata.sebep || "").replace(/;/g, ",") + ";" + (hata.icerik || "").replace(/;/g, ","));
        });

        var blob = new Blob(["﻿" + satirlar.join("\r\n")], { type: "text/csv;charset=utf-8" });
        var url = URL.createObjectURL(blob);
        var link = document.createElement("a");
        link.href = url;
        link.download = "hatali-satirlar.csv";
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }

    function bindImport() {
        el("import-onizle").addEventListener("click", function () {
            clearMessages();

            var dosya = importDosya();
            if (!dosya) {
                showMessage(el("app-message"), "Önce bir CSV dosyası seçin.", false);
                return;
            }

            var form = new FormData();
            form.append("file", dosya);
            form.append("kayitTuru", el("import-tur").value);

            api("/api/Import/onizle", { method: "POST", body: form }).then(function (result) {
                var veri = result.data;
                importDurum.onizleme = veri;

                el("import-onizleme").classList.remove("hidden");
                el("import-sonuc").classList.add("hidden");
                el("import-ozet").textContent = veri.sablon + " biçimi sezildi. Ayraç: " + veri.ayrac
                    + ", " + veri.toplamSatir + " satır, "
                    + (veri.hataliSatirlar || []).length + " satır okunamadı.";

                renderImportEslesme(veri);
                renderImportOrnek(veri);
                renderImportHatalar(veri.hataliSatirlar);
                fillImportVehicles();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });

        el("import-tur").addEventListener("change", function () {
            if (importDurum.onizleme) {
                importDurum.onizleme.kayitTuru = el("import-tur").value;
                renderImportEslesme(importDurum.onizleme);
            }
        });

        el("import-deneme").addEventListener("click", function () {
            importUygula(true);
        });

        el("import-uygula").addEventListener("click", function () {
            importUygula(false);
        });

        el("import-sifirla").addEventListener("click", importSifirla);
        el("import-hata-indir").addEventListener("click", importHatalariIndir);
    }

    function bindAyarlar() {
        el("ayarlar-btn").addEventListener("click", function () {
            var kutu = el("ayarlar-box");
            kutu.classList.toggle("hidden", !kutu.classList.contains("hidden"));
            if (!kutu.classList.contains("hidden")) {
                fillImportVehicles();
                loadDavet();
            }
        });

        el("ayarlar-close").addEventListener("click", function () {
            el("ayarlar-box").classList.add("hidden");
        });

        el("takvim-olustur").addEventListener("click", function () {
            clearMessages();
            api("/api/Takvim/abonelik", { method: "POST" }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Abonelik oluşturuldu.", true);
                el("takvim-sonuc").classList.remove("hidden");
                el("takvim-url").textContent = mutlakAdres(result.data.url);
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });

        el("takvim-kapat").addEventListener("click", function () {
            clearMessages();
            api("/api/Takvim/abonelik", { method: "DELETE" }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Abonelik kapatıldı.", true);
                el("takvim-sonuc").classList.add("hidden");
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });

        el("takvim-kopyala").addEventListener("click", function () {
            var metin = el("takvim-url").textContent;
            if (metin && navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(metin).then(function () {
                    showMessage(el("app-message"), "Bağlantı kopyalandı.", true);
                });
            }
        });
    }

    function karneKapsamiOku() {
        return {
            bakimGecmisi: el("karne-bakim").checked,
            parcaHafizasi: el("karne-parca").checked,
            yakitOzeti: el("karne-yakit").checked,
            belgeler: el("karne-belge").checked,
            plakaGoster: el("karne-plaka").checked,
            tutarGoster: el("karne-tutar").checked,
            acilKart: el("karne-acil").checked,
            hasarGecmisi: el("karne-hasar").checked,
            beyanDegeri: el("karne-deger").checked
        };
    }

    function baglantiKopyala(metin) {
        if (!metin) {
            return;
        }

        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(metin).then(function () {
                showMessage(el("app-message"), "Bağlantı kopyalandı.", true);
            });
        }
    }

    function karneSonucGoster(veri) {
        clear(el("karne-bos"));

        el("karne-sonuc").classList.remove("hidden");
        el("karne-url").textContent = mutlakAdres(veri.url);
        el("karne-goruntulenme").textContent = "Görüntülenme: " + (veri.goruntulenmeSayisi || 0);

        var acilKutu = el("karne-acil-sonuc");
        acilKutu.classList.toggle("hidden", !veri.acilUrl);

        if (veri.acilUrl) {
            el("karne-acil-url").textContent = veri.acilUrl;

            try {
                window.GarajimQR.canvasaCiz(el("karne-acil-qr"), veri.acilUrl, 4, 2);
            } catch (hata) {
                el("karne-acil-qr").classList.add("hidden");
            }
        }

        try {
            window.GarajimQR.canvasaCiz(el("karne-qr"), mutlakAdres(veri.url), 4, 2);
        } catch (hata) {
            el("karne-qr").classList.add("hidden");
        }
    }

    function bindKarne() {
        el("karne-btn").addEventListener("click", function () {
            var kutu = el("karne-box");
            var acilacak = kutu.classList.contains("hidden");
            kutu.classList.toggle("hidden", !acilacak);
            if (acilacak) {
                el("karne-sonuc").classList.add("hidden");
                bosKutuyaCiz("karne-bos", "karne");
            }
        });

        el("karne-close").addEventListener("click", function () {
            el("karne-box").classList.add("hidden");
        });

        el("karne-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();

            if (!state.selectedVehicleId) {
                showMessage(el("app-message"), "Önce bir araç seçin.");
                return;
            }

            var sure = el("karne-sure").value;

            api("/api/Vehicles/" + state.selectedVehicleId + "/karne", {
                method: "POST",
                body: {
                    kapsam: karneKapsamiOku(),
                    sonKullanmaGun: sure ? Number(sure) : null
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Bağlantı oluşturuldu.", true);
                karneSonucGoster(result.data);
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });

        el("karne-kapat").addEventListener("click", function () {
            clearMessages();
            if (!state.selectedVehicleId) {
                return;
            }

            api("/api/Vehicles/" + state.selectedVehicleId + "/karne", { method: "DELETE" })
                .then(function (result) {
                    showMessage(el("app-message"), (result && result.message) || "Paylaşım kapatıldı.", true);
                    el("karne-sonuc").classList.add("hidden");
                })
                .catch(function (error) {
                    handleError(el("app-message"), error);
                });
        });

        el("karne-kopyala").addEventListener("click", function () {
            baglantiKopyala(el("karne-url").textContent);
        });

        el("karne-acil-kopyala").addEventListener("click", function () {
            baglantiKopyala(el("karne-acil-url").textContent);
        });
    }

    function bulkUploadOne(dosya, otoOnay) {
        var form = new FormData();
        form.append("file", dosya);
        return api("/api/Receipts?otoOnay=" + (otoOnay ? "true" : "false"), { method: "POST", body: form })
            .then(function (result) {
                return { ok: true, ad: dosya.name, veri: result.data };
            })
            .catch(function (error) {
                return { ok: false, ad: dosya.name, hata: error.message || "Yüklenemedi." };
            });
    }

    function taslagiInceleyeAc(taslakId) {
        clearMessages();
        api("/api/Receipts/" + taslakId).then(function (result) {
            el("receipt-box").classList.remove("hidden");
            showReceiptReview(result.data);
            el("receipt-review").scrollIntoView({ behavior: "smooth", block: "nearest" });
        }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function renderBulkSummary(sonuclar) {
        var onaylandi = sonuclar.filter(function (s) { return s.ok && s.veri.durum === "Onaylandi"; });
        var bekliyor = sonuclar.filter(function (s) { return s.ok && s.veri.durum === "Bekliyor"; });
        var hatali = sonuclar.filter(function (s) { return !s.ok; });

        el("bulk-summary").classList.remove("hidden");
        el("bulk-summary-line").textContent =
            "Onaylandı " + onaylandi.length + " · Bekliyor " + bekliyor.length + " · Hata " + hatali.length;

        var liste = el("bulk-summary-list");
        clear(liste);

        onaylandi.forEach(function (s) {
            var li = document.createElement("li");
            li.appendChild(make("span", s.ad + " — kaydedildi"));
            var rozet = make("span", "oto", "badge-oto");
            li.appendChild(rozet);
            liste.appendChild(li);
        });

        bekliyor.forEach(function (s) {
            var li = document.createElement("li");
            li.appendChild(make("span", s.ad + " — " + (s.veri.atlamaNedeni || "Kontrol bekliyor")));

            var actions = make("span", "", "row-actions");
            var incele = make("button", "İncele", "link-btn");
            incele.type = "button";
            incele.addEventListener("click", function () { taslagiInceleyeAc(s.veri.taslakId); });
            actions.appendChild(incele);
            li.appendChild(actions);

            liste.appendChild(li);
        });

        hatali.forEach(function (s) {
            var li = document.createElement("li");
            li.appendChild(make("span", s.ad + " — " + s.hata));
            liste.appendChild(li);
        });
    }

    function bindBulkUpload() {
        el("bulk-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();

            var input = el("bulk-files");
            if (!input.files || input.files.length === 0) {
                showMessage(el("app-message"), "Önce fiş dosyalarını seçin.");
                return;
            }

            var dosyalar = Array.prototype.slice.call(input.files);
            var otoOnay = el("bulk-auto").checked;
            var sonuclar = [];
            var ilerleme = el("bulk-progress");

            el("bulk-summary").classList.add("hidden");
            ilerleme.classList.remove("hidden");
            ilerleme.textContent = "0/" + dosyalar.length;

            var zincir = Promise.resolve();
            dosyalar.forEach(function (dosya, sira) {
                zincir = zincir.then(function () {
                    return bulkUploadOne(dosya, otoOnay).then(function (sonuc) {
                        sonuclar.push(sonuc);
                        ilerleme.textContent = (sira + 1) + "/" + dosyalar.length;
                    });
                });
            });

            zincir.then(function () {
                ilerleme.classList.add("hidden");
                el("bulk-form").reset();
                el("bulk-auto").checked = otoOnay;
                renderBulkSummary(sonuclar);
                loadPendingReceipts();
                loadVehicles();
            });
        });
    }

    function bindReceipts() {
        el("receipt-btn").addEventListener("click", function () {
            if ((state.vehicles || []).length === 0 && (state.bekleyenFisSayisi || 0) === 0) {
                showMessage(el("app-message"), "Fiş yükleyebilmek için önce size bir araç zimmetlenmeli.");
                return;
            }

            var box = el("receipt-box");
            var acilacak = box.classList.contains("hidden");
            box.classList.toggle("hidden", !acilacak);
            if (acilacak) {
                hideReceiptReview();
                el("receipt-form").reset();
                loadPendingReceipts();
            }
        });

        el("receipt-close").addEventListener("click", function () {
            el("receipt-box").classList.add("hidden");
            hideReceiptReview();
        });

        el("receipt-type").addEventListener("change", receiptTypeChanged);

        el("receipt-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();

            var input = el("receipt-file");
            if (!input.files || input.files.length === 0) {
                showMessage(el("app-message"), "Önce bir fiş fotoğrafı seçin.");
                return;
            }

            var form = new FormData();
            form.append("file", input.files[0]);

            el("receipt-progress").classList.remove("hidden");

            api("/api/Receipts?otoOnay=false", { method: "POST", body: form }).then(function (result) {
                el("receipt-progress").classList.add("hidden");
                showMessage(el("app-message"), (result && result.message) || "Fiş okundu.", true);
                el("receipt-form").reset();
                showReceiptReview(result.data.taslak);
                loadPendingReceipts();
            }).catch(function (error) {
                el("receipt-progress").classList.add("hidden");
                handleError(el("app-message"), error);
            });
        });

        el("receipt-confirm-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();

            if (!state.receiptDraft) {
                return;
            }

            var tur = el("receipt-type").value;
            var kilometre = el("receipt-km").value;
            var litre = el("receipt-liters").value;

            api("/api/Receipts/" + state.receiptDraft.id + "/confirm", {
                method: "POST",
                body: {
                    vehicleId: Number(el("receipt-vehicle").value),
                    tur: tur,
                    tarih: el("receipt-date").value,
                    tutar: sayiAlan("receipt-amount"),
                    km: kilometre ? Number(kilometre) : null,
                    litre: tur === "Yakit" && litre ? Number(litre) : null,
                    bakimTuru: tur === "Bakim" ? el("receipt-maintenance-type").value : null,
                    parcalar: tur === "Bakim" && state.receiptDraft.parcalar ? state.receiptDraft.parcalar : null,
                    masrafKategorisi: tur === "Masraf" ? el("receipt-category").value : null,
                    not: el("receipt-note").value
                }
            }).then(function (result) {
                hideReceiptReview();
                loadPendingReceipts();
                loadVehicles();
                selectTab(tur === "Yakit" ? "yakit" : tur === "Bakim" ? "bakim" : "masraf");
                showMessage(el("app-message"), (result && result.message) || "Kayıt oluşturuldu.", true);
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });

        el("receipt-reject").addEventListener("click", function () {
            if (!state.receiptDraft) {
                return;
            }
            clearMessages();
            api("/api/Receipts/" + state.receiptDraft.id + "/reject", { method: "POST" }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Fiş taslağı reddedildi.", true);
                hideReceiptReview();
                loadPendingReceipts();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });
    }

    function bindDocuments() {
        el("document-close").addEventListener("click", closeDocuments);

        el("document-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();
            if (!state.documentRecordId) {
                showMessage(el("app-message"), "Önce bir bakım kaydının Belgeler düğmesine basın.");
                return;
            }
            var input = el("document-file");
            if (!input.files || input.files.length === 0) {
                showMessage(el("app-message"), "Önce bir dosya seçin.");
                return;
            }
            var form = new FormData();
            form.append("file", input.files[0]);
            form.append("maintenanceRecordId", String(state.documentRecordId));

            api("/api/Documents", { method: "POST", body: form }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Belge yüklendi.", true);
                el("document-form").reset();
                loadDocuments();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });
    }

    function bindVehicle() {
        el("arsiv-btn").addEventListener("click", arsivPaneliniAc);
        el("km-hizli-kaydet").addEventListener("click", hizliKmKaydet);
        el("evrak-tur").addEventListener("change", tescilUyarisiniGuncelle);
        el("vehicle-arsivle").addEventListener("click", arsivSecenegiIleArsivle);
        el("arsiv-yenile").addEventListener("click", arsiviYukle);
        el("arsiv-kapat").addEventListener("click", function () { el("arsiv-box").classList.add("hidden"); });
        el("vehicle-km").addEventListener("input", kmDuzeltmeAlaniniGuncelle);

        el("vehicle-brand").addEventListener("change", function () {
            seriSecenekleriniDoldur(el("vehicle-brand"), el("vehicle-model"), "")
                .catch(function (error) { handleError(el("app-message"), error); });
        });

        el("vehicle-model-listede-yok").addEventListener("change", listedeYokDurumu);

        katalogSeciciKur(el("vehicle-brand"), function (secili, q, sayfa, ekle) {
            return markaSecenekleriniDoldur(el("vehicle-brand"), secili, q, sayfa, ekle);
        });

        katalogSeciciKur(el("vehicle-model"), function (secili, q, sayfa, ekle) {
            return seriSecenekleriniDoldur(el("vehicle-brand"), el("vehicle-model"), secili, q, sayfa, ekle);
        });

        katalogSeciciKur(el("price-marka"), function (secili, q, sayfa, ekle) {
            return markaSecenekleriniDoldur(el("price-marka"), secili, q, sayfa, ekle);
        });

        katalogSeciciKur(el("price-seri"), function (secili, q, sayfa, ekle) {
            return seriSecenekleriniDoldur(el("price-marka"), el("price-seri"), secili, q, sayfa, ekle);
        });

        el("katalog-duzenle").addEventListener("click", function () {
            var arac = seciliArac();

            if (arac) {
                aracFormunuAc(arac);
            }
        });

        el("vehicle-select").addEventListener("change", function (event) {
            state.selectedVehicleId = Number(event.target.value);
            kmRozetiniTazele();
            acilKartiSakla();
            clearMessages();
            closeDocuments();
            loadActiveTab();
        });

        el("add-vehicle-btn").addEventListener("click", function () {
            aracFormunuAc(null);
        });

        el("edit-vehicle-btn").addEventListener("click", function () {
            var arac = seciliArac();
            if (!arac) {
                showMessage(el("app-message"), "Önce bir araç seçin.", false);
                return;
            }
            aracFormunuAc(arac);
        });

        el("vehicle-cancel").addEventListener("click", function () {
            el("vehicle-form-box").classList.add("hidden");
            state.duzenlenenAracId = null;
        });

        el("vehicle-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();

            var govde = aracFormGovdesi();
            var duzenleme = state.duzenlenenAracId !== null;

            if (!duzenleme && (!govde.vites || !govde.kasaTipi)) {
                showMessage(el("app-message"), "Vites ve kasa tipi seçilmeli.", false);
                return;
            }

            var istek = duzenleme
                ? api("/api/Vehicles/" + state.duzenlenenAracId, { method: "PUT", body: govde })
                : api("/api/Vehicles", { method: "POST", body: govde });

            istek.then(function (result) {
                showMessage(el("app-message"), (result && result.message) || (duzenleme ? "Araç güncellendi." : "Araç eklendi."), true);
                el("vehicle-form").reset();
                el("vehicle-form-box").classList.add("hidden");

                if (!duzenleme && result && result.data) {
                    state.selectedVehicleId = result.data.id;
                }

                state.duzenlenenAracId = null;
                loadVehicles();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });
    }

    function seciliArac() {
        return state.vehicles.filter(function (v) { return v.id === state.selectedVehicleId; })[0] || null;
    }

    function aracFormGovdesi() {
        var listedeYok = el("vehicle-model-listede-yok").checked;

        var govde = {
            brand: el("vehicle-brand").value,
            model: listedeYok ? el("vehicle-model-serbest").value : el("vehicle-model").value,
            listedeYok: listedeYok,
            year: Number(el("vehicle-year").value),
            currentKm: Number(el("vehicle-km").value),
            kmDusurmeOnayi: el("vehicle-km-onay").checked,
            kmDuzeltmeNedeni: el("vehicle-km-neden").value,
            fuelType: el("vehicle-fuel").value,
            kullanimTuru: el("vehicle-kullanim").value,
            vites: el("vehicle-vites").value || null,
            kasaTipi: el("vehicle-kasa").value || null,
            motor: el("vehicle-motor").value || null,
            ilkTescilTarihi: el("vehicle-tescil").value || null,
            acilKisiAd: el("vehicle-acil-ad").value,
            acilKisiTelefon: el("vehicle-acil-tel").value,
            acilNot: el("vehicle-acil-not").value
        };

        if (state.duzenlenenAracId === null) {
            govde.plate = el("vehicle-plate").value;
            govde.yabanciPlaka = el("vehicle-yabanci-plaka").checked;
        }

        return govde;
    }

    function aracFormunuAc(arac) {
        state.duzenlenenAracId = arac ? arac.id : null;

        el("vehicle-form-box").classList.remove("hidden");
        el("vehicle-form-baslik").textContent = arac ? "Aracı düzenle" : "Yeni araç";

        var plakaKutusu = el("vehicle-plate").parentNode;
        plakaKutusu.classList.toggle("hidden", !!arac);
        el("vehicle-plate").required = !arac;

        el("vehicle-plate").value = arac ? arac.plate : "";
        el("vehicle-yabanci-plaka").checked = !!(arac && arac.yabanciPlaka);
        el("vehicle-yabanci-plaka").parentNode.classList.toggle("hidden", !!arac);
        el("vehicle-model-listede-yok").checked = !!(arac && arac.modelEslesmedi);
        el("vehicle-model-serbest").value = arac && arac.modelEslesmedi ? arac.model : "";
        listedeYokDurumu();

        markaSecenekleriniDoldur(el("vehicle-brand"), arac ? arac.brand : "").then(function () {
            return seriSecenekleriniDoldur(el("vehicle-brand"), el("vehicle-model"), arac ? arac.model : "");
        }).catch(function (error) { handleError(el("app-message"), error); });

        yillariDoldur(el("vehicle-year"), arac ? arac.year : null);
        el("vehicle-km").value = arac ? arac.currentKm : 0;
        el("vehicle-fuel").value = arac ? arac.fuelType : "Benzin";
        el("vehicle-vites").value = (arac && arac.vites) || "";
        el("vehicle-kasa").value = (arac && arac.kasaTipi) || "";
        el("vehicle-motor").value = (arac && arac.motor) || "";
        el("vehicle-kullanim").value = (arac && arac.kullanimTuru) || "Hususi";
        el("vehicle-tescil").value = arac && arac.ilkTescilTarihi ? String(arac.ilkTescilTarihi).slice(0, 10) : "";
        el("vehicle-acil-ad").value = (arac && arac.acilKisiAd) || "";
        el("vehicle-acil-tel").value = (arac && arac.acilKisiTelefon) || "";
        el("vehicle-acil-not").value = (arac && arac.acilNot) || "";

        el("vehicle-arsivle").classList.toggle("hidden", !arac || !canManage());

        el("vehicle-km-onay").checked = false;
        el("vehicle-km-neden").value = "";
        state.duzenlenenAracKm = arac ? arac.currentKm : null;
        kmDuzeltmeAlaniniGuncelle();
    }

    function kmDuzeltmeAlaniniGuncelle() {
        var kutu = el("vehicle-km-duzeltme");
        var mevcut = state.duzenlenenAracKm;
        var yeni = Number(el("vehicle-km").value);
        var dusuyor = mevcut !== null && mevcut !== undefined && isFinite(yeni) && yeni < mevcut;

        kutu.classList.toggle("hidden", !dusuyor);

        if (!dusuyor) {
            el("vehicle-km-onay").checked = false;
            el("vehicle-km-neden").value = "";
        }
    }

    function sekmeKlavye(olay) {
        if (olay.key !== "ArrowRight" && olay.key !== "ArrowLeft") {
            return;
        }

        var gorunur = Array.prototype.filter.call(
            document.querySelectorAll(".tab-btn"),
            function (d) { return !d.classList.contains("hidden"); });

        var simdiki = gorunur.indexOf(document.activeElement);

        if (simdiki < 0) {
            return;
        }

        olay.preventDefault();

        var sonraki = olay.key === "ArrowRight"
            ? (simdiki + 1) % gorunur.length
            : (simdiki - 1 + gorunur.length) % gorunur.length;

        gorunur[sonraki].focus();
        selectTab(gorunur[sonraki].getAttribute("data-tab"));
    }

    function selectTab(tab) {
        var buttons = document.querySelectorAll(".tab-btn");
        Array.prototype.forEach.call(buttons, function (button) {
            var secili = button.getAttribute("data-tab") === tab;
            button.classList.toggle("active", secili);
            button.setAttribute("aria-selected", secili ? "true" : "false");

            if (secili && typeof button.scrollIntoView === "function") {
                button.scrollIntoView({ block: "nearest", inline: "nearest" });
            }
        });
        var panels = document.querySelectorAll(".tab-panel");
        Array.prototype.forEach.call(panels, function (panel) {
            panel.classList.add("hidden");
        });
        el("panel-" + tab).classList.remove("hidden");
        clearMessages();
        loadActiveTab();
    }

    function bindTabs() {
        var serit = document.querySelector(".tabs");

        if (serit) {
            serit.addEventListener("keydown", sekmeKlavye);
        }

        var buttons = document.querySelectorAll(".tab-btn");
        Array.prototype.forEach.call(buttons, function (button) {
            button.addEventListener("click", function () {
                selectTab(button.getAttribute("data-tab"));
            });
        });
    }

    function bindRecordForms() {
        el("maintenance-vazgec").addEventListener("click", bakimFormunuSifirla);

        el("maintenance-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();
            var duzenleme = state.duzenlenenBakimId !== null;

            api(duzenleme ? "/api/Maintenance/" + state.duzenlenenBakimId : "/api/Maintenance", {
                method: duzenleme ? "PUT" : "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    parcalar: readPartRows(el("maintenance-parts")),
                    type: el("maintenance-type").value,
                    date: el("maintenance-date").value,
                    km: Number(el("maintenance-km").value),
                    cost: sayiAlan("maintenance-cost"),
                    serviceName: el("maintenance-service").value,
                    note: el("maintenance-note").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Kayıt eklendi.", true);
                bakimFormunuSifirla();
                loadMaintenance();
                loadVehicles();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });

        el("fuel-vazgec").addEventListener("click", yakitFormunuSifirla);
        el("expense-vazgec").addEventListener("click", masrafFormunuSifirla);

        el("fuel-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();
            var yakitDuzenleme = state.duzenlenenYakitId !== null;

            api(yakitDuzenleme ? "/api/Fuel/" + state.duzenlenenYakitId : "/api/Fuel", {
                method: yakitDuzenleme ? "PUT" : "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    date: el("fuel-date").value,
                    km: Number(el("fuel-km").value),
                    liters: el("fuel-liters").value === "" ? 0 : sayiAlan("fuel-liters"),
                    kwh: el("fuel-kwh").value === "" ? null : sayiAlan("fuel-kwh"),
                    sarjTuru: el("fuel-sarj").value === "" ? null : el("fuel-sarj").value,
                    totalCost: sayiAlan("fuel-cost"),
                    tamDolum: el("fuel-tam-dolum").checked
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Kayıt eklendi.", true);
                yakitFormunuSifirla();
                loadFuel();
                loadVehicles();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });

        el("expense-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();
            var masrafDuzenleme = state.duzenlenenMasrafId !== null;

            api(masrafDuzenleme ? "/api/Expenses/" + state.duzenlenenMasrafId : "/api/Expenses", {
                method: masrafDuzenleme ? "PUT" : "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    category: el("expense-category").value,
                    date: el("expense-date").value,
                    amount: sayiAlan("expense-amount"),
                    note: el("expense-note").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Kayıt eklendi.", true);
                masrafFormunuSifirla();
                loadExpenses();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });

        el("reminder-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();
            var dateValue = el("reminder-date").value;
            var kmValue = el("reminder-km").value;
            api("/api/Reminders", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    type: el("reminder-type").value,
                    dueDate: dateValue ? dateValue : null,
                    dueKm: kmValue ? Number(kmValue) : null,
                    note: el("reminder-note").value,
                    tekrarAy: el("reminder-tekrar-ay").value ? Number(el("reminder-tekrar-ay").value) : null,
                    tekrarKm: el("reminder-tekrar-km").value ? Number(el("reminder-tekrar-km").value) : null
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Hatırlatma eklendi.", true);
                el("reminder-form").reset();
                loadReminders();
            }).finally(function () { if (typeof acKilit === "function") { acKilit(); } }).catch(function (error) {
            handleError(el("app-message"), error);
            });
        });

        el("report-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();
            loadSummary().catch(function (error) {
                handleError(el("app-message"), error);
            });
            loadMonthly();
            loadFuelStats();
            loadMaliyet();
            loadFiloMaliyet();
        });

        el("price-marka").addEventListener("change", function () {
            seriSecenekleriniDoldur(el("price-marka"), el("price-seri"), "")
                .catch(function (error) { handleError(el("app-message"), error); });
        });

        el("price-form").addEventListener("submit", function (event) {
            event.preventDefault();
            var acKilit = formuKilitle(event.target);
            clearMessages();
            api("/api/price/estimate", {
                method: "POST",
                body: {
                    marka: el("price-marka").value,
                    seri: el("price-seri").value,
                    yil: Number(el("price-yil").value),
                    kilometre: Number(el("price-km").value),
                    yakitTipi: el("price-yakit").value,
                    vitesTipi: el("price-vites").value,
                    kasaTipi: el("price-kasa").value
                }
            }).then(function (result) {
                var data = result.data;
                el("price-result").classList.remove("hidden");
                el("price-value").textContent = wholeFormat.format(Number(data.tahminiFiyat)) + " " + data.paraBirimi;
                el("price-detail").textContent = data.yil + " " + data.marka + " " + data.seri + " - " + km(data.kilometre);
            }).catch(function (error) {
                el("price-result").classList.add("hidden");
                handleError(el("app-message"), error);
            });
        });
    }

    function initSelects() {
        fillSelect(el("team-role"), TEAM_ROLES);
        fillSelect(el("receipt-type"), RECEIPT_TYPES);
        fillSelect(el("receipt-maintenance-type"), MAINTENANCE_TYPES);
        fillSelect(el("receipt-category"), EXPENSE_CATEGORIES);
        fillSelect(el("evrak-tur"), EVRAK_TYPES);
        fillSelect(el("yolculuk-amac"), YOLCULUK_AMAC);
        fillSelect(el("lastik-mevsim"), LASTIK_MEVSIM);
        fillSelect(el("hasar-tur"), HASAR_TUR);
        fillSelect(el("hasar-tutanak"), HASAR_TUTANAK);
        fillSelect(el("hasar-durum"), HASAR_DURUM);
        fillSelect(el("hasar-foto-etiket"), HASAR_ETIKET);
        fillSelect(el("deger-kaynak"), DEGER_KAYNAK);
        fillSelect(el("fuel-sarj"), SARJ_TURU);
        fillSelect(el("plan-istenen"), PLAN_TURLERI);
        fillSelect(el("vehicle-fuel"), FUEL_TYPES);
        fillSelect(el("vehicle-kasa"), KASA_TIPLERI);
        fillSelect(el("vehicle-vites"), VITES_TIPLERI);
        fillSelect(el("vehicle-kullanim"), KULLANIM_TURLERI);
        fillSelect(el("deger-kasa"), KASA_TIPLERI.slice(1));
        fillSelect(el("maintenance-type"), MAINTENANCE_TYPES);
        fillSelect(el("expense-category"), EXPENSE_CATEGORIES);
        fillSelect(el("reminder-type"), REMINDER_TYPES);
        fillSimpleSelect(el("price-yakit"), PRICE_FUEL);
        fillSimpleSelect(el("price-vites"), PRICE_GEAR);
        fillSimpleSelect(el("price-kasa"), PRICE_BODY);
    }

    function initDates() {
        var today = todayInput();
        el("maintenance-date").value = today;
        el("fuel-date").value = today;
        el("expense-date").value = today;
        el("yolculuk-tarih").value = today;
        el("lastik-tarih").value = today;
        el("hasar-tarih").value = today;
        el("deger-tarih").value = today;
        el("report-end").value = today;
        var start = new Date();
        start.setMonth(start.getMonth() - 6);
        el("report-start").value = start.toISOString().slice(0, 10);
    }

    function init() {
        initSelects();
        initDates();
        bindAuth();
        bindVehicle();
        bindTabs();
        bindRecordForms();
        bindTeam();
        bindOturumModali();
        bindKmRozeti();
        bindOnizleme();
        bindAssignment();
        bindDocuments();
        bindReceipts();
        bindBulkUpload();
        bindParts();
        bindKarne();
        bindEvrak();
        bindAyarlar();
        bindImport();
        bindYolculuk();
        bindExport();
        bindLastik();
        bindKaza();
        bindTanitim();
        rehberKartlariniCiz();
        rehberBaglantilariniKur();
        turBagla();
        bindGeriBildirim();
        bindProfil();
        bindKurulumIpucu();
        ornekDugmeleriniBagla();
        el("kurulum-gizle").addEventListener("click", kurulumuGizle);
        bindDogrulama();
        bindSifirlama();
        bindSifreDegistir();
        bindHesapSilme();
        bindHasar();
        bindDeger();
        bindDavet();
        bindPlan();
        bindUsta();
        davetKodunuUrldenOku();
        kaynagiUrldenYakala();
        kaynagiAlanlaraYaz();
        kuyrukRozetiniTazele();

        if (readSession()) {
            enterApp();
        }
    }

    function yeniSurumSeridiniGoster() {
        var serit = el("surum-serit");
        if (!serit || !serit.classList.contains("hidden")) {
            return;
        }

        serit.classList.remove("hidden");
    }

    function surumDenetimi() {
        var acilis = document.documentElement.dataset.surum || null;

        return fetch("/index.html", { method: "HEAD", cache: "no-store" }).then(function (cevap) {
            var guncel = cevap.headers.get("X-App-Version");

            if (!guncel) {
                return;
            }

            if (!acilis) {
                document.documentElement.dataset.surum = guncel;
                return;
            }

            if (guncel !== acilis) {
                yeniSurumSeridiniGoster();
            }
        }).catch(function () {
        });
    }

    function registerServiceWorker() {
        var serit = el("surum-yenile");
        if (serit) {
            serit.addEventListener("click", function () { location.reload(); });
        }

        surumDenetimi();
        setInterval(surumDenetimi, 15 * 60 * 1000);

        if (!("serviceWorker" in navigator)) {
            return;
        }

        navigator.serviceWorker.addEventListener("controllerchange", yeniSurumSeridiniGoster);

        window.addEventListener("load", function () {
            navigator.serviceWorker.register("/sw.js").catch(function () {
            });
        });
    }

    registerServiceWorker();
    document.addEventListener("DOMContentLoaded", init);
})();
