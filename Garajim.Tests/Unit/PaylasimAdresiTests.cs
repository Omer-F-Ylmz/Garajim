using Garajim.API.Startup;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Unit
{
    public class PaylasimAdresiTests
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
        public void SpaGoreliAdresiMutlakaCevirir()
        {
            var app = Oku("app.js");
            var bas = app.IndexOf("function mutlakAdres(", StringComparison.Ordinal);

            Assert.True(bas > 0, "mutlakAdres yardımcısı yok");

            var govde = app.Substring(bas, app.IndexOf("\n    }", bas, StringComparison.Ordinal) - bas);

            Assert.Contains("location.origin", govde);
        }

        [Fact]
        public void KarneDavetTakvimAdresleriMutlaklastirilir()
        {
            var app = Oku("app.js");

            Assert.Contains("mutlakAdres(veri.url)", app);
            Assert.Contains("mutlakAdres(durum.paylasimBaglantisi)", app);
            Assert.Contains("mutlakAdres(result.data.url)", app);
        }

        [Fact]
        public void UretimdeTabanAdresiZorunlu()
        {
            var yapilandirma = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:Default"] = "Server=uzak;Database=g;User Id=u;Password=p;",
                ["Jwt:Key"] = new string('k', 40),
                ["App:BaseUrl"] = ""
            }).Build();

            Assert.Contains(ProductionConfigurationGuard.Topla(yapilandirma),
                h => h.Contains("App:BaseUrl", StringComparison.Ordinal));
        }

        [Fact]
        public void TabanAdresiDoluysaSikayetYok()
        {
            var yapilandirma = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:Default"] = "Server=uzak;Database=g;User Id=u;Password=p;",
                ["Jwt:Key"] = new string('k', 40),
                ["App:BaseUrl"] = "https://garajim.runasp.net"
            }).Build();

            Assert.DoesNotContain(ProductionConfigurationGuard.Topla(yapilandirma),
                h => h.Contains("App:BaseUrl", StringComparison.Ordinal));
        }
    }
}
