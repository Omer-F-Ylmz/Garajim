namespace Garajim.Tests.Unit
{
    public class HizliKmArayuzTests
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
        public void KmRozetiUstCubuktaVar()
        {
            var html = Oku("index.html");
            var bas = html.IndexOf("class=\"topbar-right\"", StringComparison.Ordinal);
            var son = html.IndexOf("</header>", bas, StringComparison.Ordinal);

            Assert.True(bas > 0 && son > bas);
            Assert.Contains("id=\"km-rozet\"", html.Substring(bas, son - bas));
        }

        [Fact]
        public void KmModaliSayfadaVar()
        {
            var html = Oku("index.html");

            Assert.Contains("id=\"km-modal\"", html);
            Assert.Contains("id=\"km-modal-deger\"", html);
            Assert.Contains("role=\"dialog\"", html);
        }

        [Fact]
        public void RozetKmVeTazelikGosterir()
        {
            var govde = Bolum(Oku("app.js"), "function kmRozetiniTazele(", "function kmModaliAc(");

            Assert.Contains("km(", govde);
            Assert.Contains("kmTazelikMetni", govde);
        }

        [Fact]
        public void TazelikMetniGunSayarVeBayatiIsaretler()
        {
            var govde = Bolum(Oku("app.js"), "function kmTazelikMetni(", "function kmRozetiniTazele(");

            Assert.Contains("bugün", govde);
            Assert.Contains("gün önce", govde);
        }

        [Fact]
        public void KmModaliMevcuttanKucukDegeriEngeller()
        {
            var govde = Bolum(Oku("app.js"), "function kmModaliKaydet(", "function bindKmRozeti(");

            Assert.Contains("currentKm", govde);
            Assert.Contains("azaltılamaz", govde);
        }

        [Fact]
        public void KmModaliMevcutUcuKullanir()
        {
            var govde = Bolum(Oku("app.js"), "function kmModaliKaydet(", "function bindKmRozeti(");

            Assert.Contains("/km", govde);
            Assert.Contains("\"PUT\"", govde);
        }

        [Fact]
        public void SurucuRozetiDuzenleyemez()
        {
            var govde = Bolum(Oku("app.js"), "function kmRozetiniTazele(", "function kmModaliAc(");

            Assert.Contains("canManage()", govde);
        }

        [Fact]
        public void KmRozetiInnerHtmlKullanmaz()
        {
            var govde = Bolum(Oku("app.js"), "function kmTazelikMetni(", "function bindKmRozeti(");

            Assert.DoesNotContain("innerHTML", govde);
            Assert.Contains("textContent", govde);
        }
    }
}
