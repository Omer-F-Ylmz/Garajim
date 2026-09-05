namespace Garajim.Tests.Unit
{
    public class HasarSihirbaziTests
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

        private static string Govde(string baslangic, string bitis)
        {
            var app = Oku("app.js");
            var bas = app.IndexOf(baslangic, StringComparison.Ordinal);
            var son = app.IndexOf(bitis, bas, StringComparison.Ordinal);

            Assert.True(bas > 0 && son > bas, baslangic + " bulunamadı");
            return app.Substring(bas, son - bas);
        }

        [Fact]
        public void SihirbazYeniKaydiIsaretler()
        {
            var govde = Govde("function hasarSihirbaziniAc(", "function hasarSihirbaziniKapat(");

            Assert.Contains("state.hasarYeniKayit", govde);
        }

        [Fact]
        public void VazgecYeniAcilanKaydiSiler()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function hasarSihirbaziniIptalEt(", StringComparison.Ordinal);
            var son = app.IndexOf("function hasarGovdesi(", bas, StringComparison.Ordinal);

            Assert.True(bas > 0 && son > bas, "iptal fonksiyonu yok");

            var govde = app.Substring(bas, son - bas);

            Assert.Contains("state.hasarYeniKayit", govde);
            Assert.Contains("method: \"DELETE\"", govde);
            Assert.Contains("/api/Hasar/", govde);
        }

        [Fact]
        public void VazgecDugmesiIptalIslevineBaglanir()
        {
            var app = Oku("app.js");

            Assert.Contains("el(\"hasar-vazgec\").addEventListener(\"click\", hasarSihirbaziniIptalEt);", app);
            Assert.DoesNotContain("el(\"hasar-vazgec\").addEventListener(\"click\", hasarSihirbaziniKapat);", app);
        }

        [Fact]
        public void KapanistaListeTazelenir()
        {
            var govde = Govde("function hasarSihirbaziniKapat(", "function hasarGovdesi(");

            Assert.Contains("loadHasar()", govde);
        }

        [Fact]
        public void YeniKayittaOlusturuldiMesajiVerilir()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("el(\"hasar-form\").addEventListener(\"submit\"", StringComparison.Ordinal);
            var govde = app.Substring(bas, 700);

            Assert.Contains("Hasar dosyası oluşturuldu.", govde);
        }
    }
}
