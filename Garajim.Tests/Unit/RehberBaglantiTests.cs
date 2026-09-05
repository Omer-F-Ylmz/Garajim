namespace Garajim.Tests.Unit
{
    public class RehberBaglantiTests
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

        private static string RehberYolu(string gorel)
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            return Path.Combine(kok.FullName, "Garajim.API", "wwwroot", "rehber", gorel);
        }

        [Fact]
        public void TanitimdaAltiRehberKartiVar()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("REHBER_KARTLARI = [", StringComparison.Ordinal);
            var bitis = app.IndexOf("];", baslangic, StringComparison.Ordinal);

            Assert.True(baslangic > 0);

            var blok = app.Substring(baslangic, bitis - baslangic);
            var satirlar = blok.Split('\n').Where(s => s.TrimStart().StartsWith("[\"", StringComparison.Ordinal)).ToList();

            Assert.Equal(6, satirlar.Count);
            Assert.Contains("tanitim-rehber-kartlar", app);
            Assert.Contains("id=\"tanitim-rehber\"", Oku("index.html"));
        }

        [Fact]
        public void TanitimRehberKartlariMevcutSayfalaraGider()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("REHBER_KARTLARI = [", StringComparison.Ordinal);
            var blok = app.Substring(baslangic, app.IndexOf("];", baslangic, StringComparison.Ordinal) - baslangic);

            foreach (System.Text.RegularExpressions.Match eslesme in
                     System.Text.RegularExpressions.Regex.Matches(blok, "\"/rehber/([^\"]*)\""))
            {
                var hedef = eslesme.Groups[1].Value;

                if (hedef.EndsWith("/", StringComparison.Ordinal) || hedef.Length == 0)
                {
                    hedef += "index.html";
                }

                Assert.True(File.Exists(RehberYolu(hedef)), "kırık bağlantı: /rehber/" + eslesme.Groups[1].Value);
            }
        }

        [Fact]
        public void UygulamaIcindeRehberBaglantilariYeniSekmedeAcilir()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("function rehberBaglantisi(", StringComparison.Ordinal);
            var bitis = app.IndexOf("function tanitimKartlariniCiz(", StringComparison.Ordinal);

            Assert.True(baslangic > 0 && bitis > baslangic);

            var govde = app.Substring(baslangic, bitis - baslangic);

            Assert.Contains("target = \"_blank\"", govde);
            Assert.Contains("rel = \"noopener\"", govde);
            Assert.DoesNotContain("innerHTML", govde);
            Assert.Contains("evrak-form", govde);
            Assert.Contains("panel-lastik", govde);
            Assert.Contains("panel-parca", govde);
        }

        [Fact]
        public void UstMenuVeYardimRehbereBaglanir()
        {
            Assert.Contains("href=\"/rehber/\"", Oku("index.html"));
            Assert.Contains("href=\"/rehber/\"", Oku("yardim.html"));
        }
    }
}
