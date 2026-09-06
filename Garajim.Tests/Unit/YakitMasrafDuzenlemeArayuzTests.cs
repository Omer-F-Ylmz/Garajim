namespace Garajim.Tests.Unit
{
    public class YakitMasrafDuzenlemeArayuzTests
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

        private static string Bolum(string metin, string bas, string son)
        {
            var i = metin.IndexOf(bas, StringComparison.Ordinal);
            Assert.True(i > 0, bas + " bulunamadı");

            var j = metin.IndexOf(son, i, StringComparison.Ordinal);
            Assert.True(j > i, son + " bulunamadı");

            return metin.Substring(i, j - i);
        }

        [Fact]
        public void YakitFormuDuzenlemedePutKullanir()
        {
            var govde = Bolum(Oku("app.js"), "var yakitDuzenleme = state.duzenlenenYakitId", "loadVehicles();");

            Assert.Contains("\"PUT\"", govde);
            Assert.Contains("/api/Fuel/", govde);
        }

        [Fact]
        public void MasrafFormuDuzenlemedePutKullanir()
        {
            var govde = Bolum(Oku("app.js"), "var masrafDuzenleme = state.duzenlenenMasrafId", "loadExpenses();");

            Assert.Contains("\"PUT\"", govde);
            Assert.Contains("/api/Expenses/", govde);
        }

        [Fact]
        public void VazgecDugmeleriFormuSifirlar()
        {
            var app = Oku("app.js");

            Assert.Contains("el(\"fuel-vazgec\").addEventListener(\"click\", yakitFormunuSifirla);", app);
            Assert.Contains("el(\"expense-vazgec\").addEventListener(\"click\", masrafFormunuSifirla);", app);
        }

        [Fact]
        public void DuzenlemeKipiGonderDugmesininMetniniDegistirir()
        {
            var app = Oku("app.js");

            Assert.Contains("Yakıtı güncelle", app);
            Assert.Contains("Masrafı güncelle", app);
        }

        [Fact]
        public void VazgecDugmeleriSayfadaVar()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"fuel-vazgec\"", html);
            Assert.Contains("id=\"expense-vazgec\"", html);
            Assert.Contains("id=\"fuel-submit\"", html);
            Assert.Contains("id=\"expense-submit\"", html);
        }
    }
}
