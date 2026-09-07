namespace Garajim.Tests.Unit
{
    public class UstSeritEtiketiTests
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

        private static string Govde()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function ustSeritEtiketi(", StringComparison.Ordinal);

            Assert.True(bas > 0, "ustSeritEtiketi bulunamadı");

            var son = app.IndexOf("\n    function ", bas + 10, StringComparison.Ordinal);

            Assert.True(son > bas);
            return app.Substring(bas, son - bas);
        }

        [Fact]
        public void EtiketAyniAdiIkiKezYazmaz()
        {
            var govde = Govde();

            Assert.Contains("toLocaleLowerCase(\"tr\")", govde);
            Assert.Contains("return ad;", govde);
        }

        [Fact]
        public void EtiketBoslariKirpar()
        {
            var govde = Govde();

            Assert.Contains(".trim()", govde);
        }

        [Fact]
        public void EtiketTekYerdeKurulur()
        {
            var app = Oku("app.js");

            Assert.Equal(3, app.Split("ustSeritEtiketi(").Length - 1);
            Assert.DoesNotContain("label = label ? label + \" · \" + user.companyName", app);
            Assert.DoesNotContain("etiket = etiket ? etiket + \" · \" + user.companyName", app);
        }

        [Fact]
        public void EtiketTextContentKullanir()
        {
            var app = Oku("app.js");

            Assert.Contains("el(\"user-label\").textContent = ustSeritEtiketi(", app);
        }
    }
}
