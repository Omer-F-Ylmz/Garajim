namespace Garajim.Tests.Unit
{
    public class ServiceWorkerKurallariTests
    {
        private static string Oku(string dosya)
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);
            return File.ReadAllText(Path.Combine(kok.FullName, "Garajim.API", "wwwroot", dosya));
        }

        [Fact]
        public void KazaRehberiOnbellegeAlinir()
        {
            var sw = Oku("sw.js");

            Assert.Contains("REHBER_YOLU = \"/api/Hasar/rehber\"", sw);
            Assert.Contains("caches.match(REHBER_YOLU)", sw);

            var rehberDali = sw.IndexOf("url.pathname === REHBER_YOLU", StringComparison.Ordinal);
            var apiDali = sw.IndexOf("url.pathname.indexOf(\"/api/\") === 0", StringComparison.Ordinal);

            Assert.True(rehberDali > 0);
            Assert.True(apiDali > rehberDali, "Rehber dalı genel /api/ dalından önce gelmeli, yoksa hiç çalışmaz.");
        }

        [Fact]
        public void KarneVeAcilHalaOnbellegeGirmez()
        {
            var sw = Oku("sw.js");

            Assert.Contains("url.pathname.indexOf(\"/karne\") === 0 || url.pathname.indexOf(\"/acil\") === 0", sw);

            var bypassDali = sw.IndexOf("url.pathname.indexOf(\"/karne\") === 0", StringComparison.Ordinal);
            var genelOnbellek = sw.LastIndexOf("caches.open(KABUK_SURUMU).then(function (cache) { cache.put(istek, kopya); });", StringComparison.Ordinal);

            Assert.True(bypassDali > 0);
            Assert.True(genelOnbellek > bypassDali, "Karne/acil dalı genel önbellek dalından önce dönmeli.");

            foreach (var yasak in new[] { "\"/karne.html\"", "\"/acil.html\"", "\"/karne.js\"", "\"/acil.js\"" })
            {
                Assert.DoesNotContain(yasak, sw);
            }
        }

        [Fact]
        public void KabukListesiSadeceUygulamaKabugunuTutar()
        {
            var sw = Oku("sw.js");
            var baslangic = sw.IndexOf("KABUK_DOSYALARI", StringComparison.Ordinal);
            var bitis = sw.IndexOf("];", baslangic, StringComparison.Ordinal);
            var liste = sw.Substring(baslangic, bitis - baslangic);

            foreach (var beklenen in new[] { "\"/\"", "\"/index.html\"", "\"/styles.css\"", "\"/app.js\"", "\"/manifest.json\"" })
            {
                Assert.Contains(beklenen, liste);
            }

            Assert.DoesNotContain("karne", liste);
            Assert.DoesNotContain("acil", liste);
            Assert.DoesNotContain("/api/", liste);
        }

        [Fact]
        public void OnbellekAdiSurumdenUretilir()
        {
            var sw = Oku("sw.js");

            Assert.Contains("garajim-kabuk-", sw);
            Assert.Contains("__SURUM__", sw);
            Assert.DoesNotContain("garajim-kabuk-v", sw);
            Assert.Contains("caches.delete(ad)", sw);
        }

        [Fact]
        public void KuyrukIcinSyncDinleyicisiVar()
        {
            var sw = Oku("sw.js");

            Assert.Contains("addEventListener(\"sync\"", sw);
            Assert.Contains("garajim-hasar-kuyruk", sw);
            Assert.Contains("hasar-kuyrugu-bosalt", sw);
        }

        [Fact]
        public void UygulamaSyncYoksaCevrimiciOlayinaDusuyor()
        {
            var app = Oku("app.js");

            Assert.Contains("\"SyncManager\" in window", app);
            Assert.Contains("window.addEventListener(\"online\"", app);
            Assert.Contains("KAZA_KUYRUK_ANAHTARI", app);
            Assert.Contains("kuyrugoBosalt", app);
        }

        [Fact]
        public void AcilKartYalnizYerelDepodanCizilir()
        {
            var app = Oku("app.js");

            Assert.Contains("ACIL_KART_ANAHTARI", app);
            Assert.Contains("function acilKartiCiz()", app);
            Assert.DoesNotContain("/acil.html", app);
        }
        [Fact]
        public void KuyrukBildirimiVeRozetiVar()
        {
            var app = Oku("app.js");
            var html = Oku("index.html");

            Assert.Contains("Kaydınız kuyruğa alındı, bağlantı gelince gönderilecek.", app);
            Assert.Contains("el(\"kaza-modal\").classList.add(\"hidden\");", app);
            Assert.Contains("id=\"kuyruk-serit\"", html);
            Assert.Contains("function kuyrukRozetiniGuncelle()", app);
        }

        [Fact]
        public void KazaAniMobildeYapiskan()
        {
            var css = Oku("styles.css");
            var mobilBlok = css.Split("@media").FirstOrDefault(b => b.Contains("max-width: 767px") && b.Contains(".kaza-ani"));

            Assert.NotNull(mobilBlok);
            Assert.Contains("position: sticky", mobilBlok);
        }

        [Fact]
        public void KarneHataSayfasindaYazdirGizlenir()
        {
            var js = Oku("karne.js");

            Assert.Contains("el(\"yazdir\").classList.add(\"hidden\")", js);
        }

        [Fact]
        public void MobilUstCubukIkiSatiriGecmez()
        {
            var css = Oku("styles.css");

            Assert.Contains(".topbar", css);
            Assert.Contains("--ustcubuk-mobil", css);
        }
        [Fact]
        public void SurumBilgisiKisaShaVeAyracTasir()
        {
            Assert.Equal("1.0.0+e578dd8", Garajim.API.Startup.SurumBilgisi.Sadelestir("1.0.0+e578dd827d8c4de3cc7ec1cda106efafa02525f5"));
            Assert.Equal("1.0.0", Garajim.API.Startup.SurumBilgisi.Sadelestir("1.0.0"));
        }



    }
}
