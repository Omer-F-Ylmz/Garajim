using Garajim.API.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Garajim.Tests.Unit
{
    public class ProductionConfigurationGuardTests
    {
        private const string GecerliBaglanti = "Server=sunucu.mssql.somee.com,1433;Database=GarajimDb;User Id=garajim;Password=Gucluu1;TrustServerCertificate=True";
        private const string GecerliAnahtar = "uretim-icin-uretilmis-32-bayttan-uzun-imzalama-anahtari";

        private sealed class SahteOrtam : IHostEnvironment
        {
            public string EnvironmentName { get; set; } = Environments.Production;
            public string ApplicationName { get; set; } = "Garajim.API";
            public string ContentRootPath { get; set; } = ".";
            public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; }
        }

        private static IConfiguration Yapilandirma(string baglanti, string anahtar)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ConnectionStrings:Default"] = baglanti,
                    ["Jwt:Key"] = anahtar
                })
                .Build();
        }

        [Fact]
        public void GecerliUretimYapilandirmasiGecer()
        {
            var hata = Record.Exception(() =>
                ProductionConfigurationGuard.Validate(Yapilandirma(GecerliBaglanti, GecerliAnahtar), new SahteOrtam()));

            Assert.Null(hata);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("buraya-baglanti-cumlesi")]
        public void BosVeyaYerTutucuBaglantiReddedilir(string baglanti)
        {
            var hata = Assert.Throws<InvalidOperationException>(() =>
                ProductionConfigurationGuard.Validate(Yapilandirma(baglanti, GecerliAnahtar), new SahteOrtam()));

            Assert.Contains("ConnectionStrings:Default tanımlı değil", hata.Message);
            Assert.Contains("ConnectionStrings__Default", hata.Message);
        }

        [Theory]
        [InlineData("Server=(localdb)\\MSSQLLocalDB;Database=GarajimDb;Trusted_Connection=True")]
        [InlineData("Server=(LocalDB)\\v11.0;Database=GarajimDb")]
        [InlineData("Data Source=LOCALDB;Initial Catalog=GarajimDb")]
        public void LocalDbBaglantisiUretimdeReddedilir(string baglanti)
        {
            var hata = Assert.Throws<InvalidOperationException>(() =>
                ProductionConfigurationGuard.Validate(Yapilandirma(baglanti, GecerliAnahtar), new SahteOrtam()));

            Assert.Contains("LocalDB'ye işaret ediyor", hata.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("dev-ortami-varsayilan-jwt-anahtari-prodda-Jwt__Key-ortam-degiskeni-kullan")]
        public void BosVeyaYerTutucuAnahtarReddedilir(string anahtar)
        {
            var hata = Assert.Throws<InvalidOperationException>(() =>
                ProductionConfigurationGuard.Validate(Yapilandirma(GecerliBaglanti, anahtar), new SahteOrtam()));

            Assert.Contains("Jwt:Key tanımlı değil", hata.Message);
            Assert.Contains("Jwt__Key", hata.Message);
        }

        [Fact]
        public void KisaAnahtarReddedilir()
        {
            var hata = Assert.Throws<InvalidOperationException>(() =>
                ProductionConfigurationGuard.Validate(Yapilandirma(GecerliBaglanti, "kisa-anahtar"), new SahteOrtam()));

            Assert.Contains("en az 32 bayt", hata.Message);
        }

        [Fact]
        public void TumHatalarTekMesajdaToplanir()
        {
            var hata = Assert.Throws<InvalidOperationException>(() =>
                ProductionConfigurationGuard.Validate(Yapilandirma(null, null), new SahteOrtam()));

            Assert.Contains("ConnectionStrings:Default", hata.Message);
            Assert.Contains("Jwt:Key", hata.Message);
            Assert.Contains("uygulama başlatılmadı", hata.Message);
        }

        [Theory]
        [InlineData("Development")]
        [InlineData("Testing")]
        [InlineData("Staging")]
        public void UretimDisiOrtamlardaDenetimCalismaz(string ortam)
        {
            var hata = Record.Exception(() =>
                ProductionConfigurationGuard.Validate(Yapilandirma(null, null), new SahteOrtam { EnvironmentName = ortam }));

            Assert.Null(hata);
        }

        [Fact]
        public void UretimdekiAppsettingsVarsayilanlariReddedilir()
        {
            var yapilandirma = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(ApiKlasoru(), "appsettings.json"), optional: false)
                .Build();

            var hatalar = ProductionConfigurationGuard.Topla(yapilandirma);

            Assert.Equal(2, hatalar.Count);
        }

        private static string ApiKlasoru()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "Garajim.API", "appsettings.json");
                if (File.Exists(candidate))
                {
                    return Path.Combine(directory.FullName, "Garajim.API");
                }

                directory = directory.Parent;
            }

            return AppContext.BaseDirectory;
        }
    }
}
