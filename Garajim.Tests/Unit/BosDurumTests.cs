namespace Garajim.Tests.Unit
{
    public class BosDurumTests
    {
        private static readonly string[] Anahtarlar =
        {
            "arac", "yakit", "bakim", "masraf", "fis",
            "evrak", "lastik", "hasar", "yolculuk", "karne", "ekip"
        };

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

        private static List<string> Satirlar()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("BOS_DURUMLAR = {", StringComparison.Ordinal);
            var bitis = app.IndexOf("\n    };", baslangic, StringComparison.Ordinal);

            Assert.True(baslangic > 0, "BOS_DURUMLAR tanımlı olmalı.");

            return app.Substring(baslangic, bitis - baslangic)
                .Split('\n')
                .Select(s => s.Trim())
                .Where(s => s.Contains(": [", StringComparison.Ordinal))
                .ToList();
        }

        [Fact]
        public void OnBirSekmeninBosDurumuTanimli()
        {
            var satirlar = Satirlar();

            Assert.Equal(Anahtarlar.Length, satirlar.Count);

            foreach (var anahtar in Anahtarlar)
            {
                Assert.Contains(satirlar, s => s.StartsWith(anahtar + ": [", StringComparison.Ordinal));
            }
        }

        [Fact]
        public void HerBosDurumMetinDugmeVeHedefTasir()
        {
            foreach (var satir in Satirlar())
            {
                var parcalar = satir.Split("\", \"");

                Assert.True(parcalar.Length >= 3, "Eksik alan: " + satir);
                Assert.Contains("#", satir, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void HedeflerHtmldeGercektenVar()
        {
            var html = Oku("index.html");

            foreach (var satir in Satirlar())
            {
                var kirikBaslangic = satir.LastIndexOf("\"#", StringComparison.Ordinal);
                var kirikBitis = satir.IndexOf('"', kirikBaslangic + 1);
                var hedef = satir.Substring(kirikBaslangic + 2, kirikBitis - kirikBaslangic - 2);

                Assert.Contains("id=\"" + hedef + "\"", html);
            }
        }

        [Fact]
        public void TekOrtakBilesenKullanilir()
        {
            var app = Oku("app.js");

            Assert.Contains("function bosDurumKutusu(anahtar)", app);
            Assert.Contains("function bosSatir(tbody, sutun, anahtar)", app);

            var baslangic = app.IndexOf("function bosDurumKutusu(anahtar)", StringComparison.Ordinal);
            var bitis = app.IndexOf("function bosSatir(tbody, sutun, anahtar)", StringComparison.Ordinal);
            var govde = app.Substring(baslangic, bitis - baslangic);

            Assert.Contains("document.createElement", govde);
            Assert.DoesNotContain("innerHTML", govde);
        }

        [Fact]
        public void TablolarOrtakBilesenidenGecer()
        {
            var app = Oku("app.js");

            foreach (var anahtar in new[] { "bakim", "yakit", "masraf", "evrak", "lastik", "hasar", "yolculuk" })
            {
                Assert.Contains("bosSatir(tbody, ", app);
                Assert.Contains("\"" + anahtar + "\")", app);
            }
        }
    }
}
