namespace Garajim.Tests.Unit
{
    public class AracFormuTests
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
        public void VitesVeKasaEklemedeZorunlu()
        {
            var html = Oku("index.html");
            var app = Oku("app.js");

            Assert.Contains("<select id=\"vehicle-vites\" required></select>", html);
            Assert.Contains("<select id=\"vehicle-kasa\" required></select>", html);
            Assert.Contains("if (!duzenleme && (!govde.vites || !govde.kasaTipi))", app);
        }

        [Fact]
        public void VitesSecenekleriModelSozluguyleAyni()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("VITES_TIPLERI = [", StringComparison.Ordinal);
            var bitis = app.IndexOf("];", baslangic, StringComparison.Ordinal);

            Assert.True(baslangic > 0);

            var blok = app.Substring(baslangic, bitis - baslangic);

            foreach (var deger in new[] { "\"Otomatik\"", "\"Düz\"", "\"Yarı Otomatik\"" })
            {
                Assert.Contains(deger, blok);
            }
        }

        [Fact]
        public void DuzenleDugmesiYalnizYoneticiyeAcik()
        {
            var html = Oku("index.html");
            var app = Oku("app.js");

            Assert.Contains("id=\"edit-vehicle-btn\"", html);
            Assert.Contains("class=\"ghost hidden\"", html);
            Assert.Contains("el(\"edit-vehicle-btn\").classList.toggle(\"hidden\", !canManage());", app);
        }

        [Fact]
        public void DuzenlemeModuPlakayiGizlerVePutKullanir()
        {
            var app = Oku("app.js");

            Assert.Contains("plakaKutusu.classList.toggle(\"hidden\", !!arac);", app);
            Assert.Contains("api(\"/api/Vehicles/\" + state.duzenlenenAracId, { method: \"PUT\", body: govde })", app);
            Assert.Contains("if (state.duzenlenenAracId === null) {", app);
        }

        [Fact]
        public void YabanciPlakaSecenegiFormdaVar()
        {
            var html = Oku("index.html");
            var app = Oku("app.js");

            Assert.Contains("id=\"vehicle-yabanci-plaka\"", html);
            Assert.Contains("govde.yabanciPlaka = el(\"vehicle-yabanci-plaka\").checked;", app);
        }

        [Fact]
        public void ArsivleDugmesiDuzenlenenAracaBaglidir()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("function arsivSecenegiIleArsivle()", StringComparison.Ordinal);
            var bitis = app.IndexOf("function tescilUyarisiniGuncelle", baslangic, StringComparison.Ordinal);

            Assert.True(baslangic > 0);

            var govde = app.Substring(baslangic, bitis - baslangic);

            Assert.Contains("duzenlenenArac()", govde);
            Assert.DoesNotContain("seciliArac()", govde);
        }

        [Fact]
        public void YeniAracModundaArsivleDugmesiGizlenir()
        {
            var app = Oku("app.js");

            Assert.Contains("el(\"vehicle-arsivle\").classList.toggle(\"hidden\", !arac || !canManage());", app);
        }

        [Fact]
        public void FormTumAlanlariTasir()
        {
            var app = Oku("app.js");
            var baslangic = app.IndexOf("function aracFormGovdesi()", StringComparison.Ordinal);
            var bitis = app.IndexOf("function aracFormunuAc(", StringComparison.Ordinal);

            Assert.True(baslangic > 0 && bitis > baslangic);

            var govde = app.Substring(baslangic, bitis - baslangic);

            foreach (var alan in new[] { "brand", "model", "year", "currentKm", "fuelType", "kullanimTuru", "vites", "kasaTipi", "motor", "ilkTescilTarihi", "acilKisiAd", "acilKisiTelefon", "acilNot" })
            {
                Assert.Contains(alan + ":", govde);
            }
        }
    }
}
