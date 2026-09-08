namespace Garajim.Tests.Unit
{
    public class KucukArayuzBulgulariTests
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
        public void CevrimdisiKuyrukSeridiBosalincaTemizlenir()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("addEventListener(\"online\"", StringComparison.Ordinal);

            Assert.True(bas > 0);
            Assert.Contains("kuyrukRozetiniGuncelle", app.Substring(bas, 200));
        }

        [Fact]
        public void SifirlamaKodKutulariYapistirmaKabulEder()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function bindSifirlama(", StringComparison.Ordinal);
            var son = app.IndexOf("function geciciSifreUyarisi(", bas, StringComparison.Ordinal);

            Assert.True(bas > 0 && son > bas);
            Assert.Contains("\"paste\"", app.Substring(bas, son - bas));
        }

        [Fact]
        public void ArsivlemeVeSokumPromptKullanmaz()
        {
            var app = Oku("app.js");

            Assert.DoesNotContain("window.prompt", app);
        }

        [Fact]
        public void PwaIpucuIpadUserAgentiniDeYakalar()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function iosMu(", StringComparison.Ordinal);
            var govde = app.Substring(bas, 400);

            Assert.Contains("maxTouchPoints", govde);
        }

        [Fact]
        public void PwaMetniCihazBagimsiz()
        {
            var app = Oku("app.js");

            Assert.DoesNotContain("telefonuna kurabilirsin", app);
        }

        [Fact]
        public void SifreDegistirFormundaKuralIpucuVar()
        {
            var html = Oku("index.html");
            var bas = html.IndexOf("id=\"sifre-yeni\"", StringComparison.Ordinal);

            Assert.Contains("En az 8 karakter", html.Substring(bas, 300));
        }

        [Fact]
        public void KayitSifresiIstemcideDeDenetlenir()
        {
            var html = Oku("index.html");
            var bas = html.IndexOf("id=\"register-password\"", StringComparison.Ordinal);

            Assert.Contains("minlength=\"8\"", html.Substring(bas - 60, 160));
        }

        [Fact]
        public void MesajlarBirSureSonraSilinir()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function showMessage(", StringComparison.Ordinal);
            var son = app.IndexOf("function clearMessages(", bas, StringComparison.Ordinal);

            Assert.Contains("setTimeout", app.Substring(bas, son - bas));
        }

        [Fact]
        public void BelgeBasligiIcKimlikGostermez()
        {
            var app = Oku("app.js");

            Assert.DoesNotContain("\"Bakım kaydı #\"", app);
            Assert.DoesNotContain("Bakım kaydı #", app);
        }

        [Fact]
        public void FisReddiSilmeDemez()
        {
            var app = Oku("app.js");

            Assert.DoesNotContain("Taslak silindi.", app);
        }

        [Fact]
        public void TopluOkumaOtoOnayVarsayilanKapali()
        {
            var html = Oku("index.html");
            var bas = html.IndexOf("id=\"bulk-auto\"", StringComparison.Ordinal);
            var etiket = html.Substring(bas, 90);

            Assert.DoesNotContain("checked", etiket);
        }

        [Fact]
        public void SupheliRozetiGorunurAciklamaTasir()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("şüpheli", StringComparison.Ordinal);

            Assert.True(bas > 0);
            Assert.Contains("aria-label", app.Substring(bas - 400, 800));
        }
    }
}
