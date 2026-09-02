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

        private static IConfiguration Yapilandirma(string baglanti, string anahtar, string sahteYanit = null, bool smtp = true)
        {
            var degerler = new Dictionary<string, string>
            {
                ["ConnectionStrings:Default"] = baglanti,
                ["Jwt:Key"] = anahtar
            };

            if (smtp)
            {
                degerler["Smtp:Host"] = "smtp-relay.brevo.com";
                degerler["Smtp:User"] = "garajim";
                degerler["Smtp:Pass"] = "brevo-anahtari";
                degerler["Smtp:From"] = "bilgi.app";
            }

            if (sahteYanit != null)
            {
                degerler["Usta:SahteYanit"] = sahteYanit;
            }

            return new ConfigurationBuilder().AddInMemoryCollection(degerler).Build();
        }

        [Theory]
        [InlineData("true")]
        [InlineData("True")]
        [InlineData("TRUE")]
        public void UretimdeSahteUstaYanitiReddedilir(string deger)
        {
            var hata = Assert.Throws<InvalidOperationException>(() =>
                ProductionConfigurationGuard.Validate(Yapilandirma(GecerliBaglanti, GecerliAnahtar, deger), new SahteOrtam()));

            Assert.Contains("Usta:SahteYanit", hata.Message);
            Assert.Contains("sahte", hata.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("false")]
        [InlineData("False")]
        [InlineData("")]
        public void SahteUstaYanitiKapaliysaUretimGecer(string deger)
        {
            var hata = Record.Exception(() =>
                ProductionConfigurationGuard.Validate(Yapilandirma(GecerliBaglanti, GecerliAnahtar, deger), new SahteOrtam()));

            Assert.Null(hata);
        }

        [Fact]
        public void GelistirmedeSahteUstaYanitiSerbesttir()
        {
            var ortam = new SahteOrtam { EnvironmentName = Environments.Development };

            var hata = Record.Exception(() =>
                ProductionConfigurationGuard.Validate(Yapilandirma(null, null, "true"), ortam));

            Assert.Null(hata);
        }

        [Fact]
        public void SahteYanitHatasiDigerHatalarlaBirlikteBildirilir()
        {
            var hatalar = ProductionConfigurationGuard.Topla(Yapilandirma(null, null, "true"));

            Assert.Equal(3, hatalar.Count);
            Assert.Contains(hatalar, h => h.Contains("Usta:SahteYanit"));
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

        [Theory]
        [InlineData("Smtp:Host")]
        [InlineData("Smtp:User")]
        [InlineData("Smtp:From")]
        public void UretimdeEksikSmtpAyariReddedilir(string eksikAnahtar)
        {
            var yapilandirma = Yapilandirma(GecerliBaglanti, GecerliAnahtar);
            yapilandirma[eksikAnahtar] = null;

            var hatalar = ProductionConfigurationGuard.Topla(yapilandirma);

            Assert.Contains(hatalar, h => h.Contains(eksikAnahtar, StringComparison.Ordinal));
        }

        [Fact]
        public void UretimdeSmtpParolasiYoksaReddedilir()
        {
            var yapilandirma = Yapilandirma(GecerliBaglanti, GecerliAnahtar);
            yapilandirma["Smtp:Pass"] = null;
            yapilandirma["Smtp:Password"] = null;

            var hatalar = ProductionConfigurationGuard.Topla(yapilandirma);

            Assert.Contains(hatalar, h => h.Contains("Smtp:Pass", StringComparison.Ordinal));
        }

        [Fact]
        public void SmtpTamamenEksikseUretimBaslamaz()
        {
            var ortam = new SahteOrtam();
            var yapilandirma = Yapilandirma(GecerliBaglanti, GecerliAnahtar, smtp: false);

            var hata = Assert.Throws<InvalidOperationException>(
                () => ProductionConfigurationGuard.Validate(yapilandirma, ortam));

            Assert.Contains("Smtp__Host", hata.Message);
        }

        [Fact]
        public void SmtpEksikligiGelistirmeOrtaminiDurdurmaz()
        {
            var ortam = new SahteOrtam { EnvironmentName = Environments.Development };
            var yapilandirma = Yapilandirma(null, null, smtp: false);

            ProductionConfigurationGuard.Validate(yapilandirma, ortam);
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

            Assert.Equal(6, hatalar.Count);
            Assert.Contains(hatalar, h => h.Contains("ConnectionStrings:Default", StringComparison.Ordinal));
            Assert.Contains(hatalar, h => h.Contains("Jwt:Key", StringComparison.Ordinal));
            foreach (var anahtar in new[] { "Smtp:Host", "Smtp:User", "Smtp:From", "Smtp:Pass" })
            {
                Assert.Contains(hatalar, h => h.Contains(anahtar, StringComparison.Ordinal));
            }
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
