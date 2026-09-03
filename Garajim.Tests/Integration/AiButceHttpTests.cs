using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class AiButceHttpTests : IDisposable
    {
        private static readonly byte[] PngBaslik = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private sealed class ButceFactory : GarajimWebApplicationFactory
        {
            private readonly string _tavan;
            private readonly string _klasor;

            public ButceFactory(string tavan, string klasor)
            {
                _tavan = tavan;
                _klasor = klasor;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Ai:AylikTokenTavani"] = _tavan,
                        ["App:DestekEposta"] = "destek@garajim.local",
                        ["Documents:StoragePath"] = _klasor
                    });
                });
            }
        }

        private readonly string _klasor;
        private readonly ButceFactory _factory;

        public AiButceHttpTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-butce-" + Guid.NewGuid().ToString("N"));
            _factory = new ButceFactory("100", _klasor);
        }

        public void Dispose()
        {
            _factory.Dispose();

            if (Directory.Exists(_klasor))
            {
                Directory.Delete(_klasor, true);
            }
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Bütçe Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private void TavaniDoldur()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();
            var bugun = DateTime.UtcNow;

            context.AiTokenSayaclari.Add(new AiTokenSayaci
            {
                Yil = bugun.Year,
                Ay = bugun.Month,
                TokenGiris = 90,
                TokenCikis = 20
            });

            context.SaveChanges();
        }

        private static MultipartFormDataContent Fis()
        {
            var icerik = new byte[256];
            Array.Copy(PngBaslik, icerik, PngBaslik.Length);

            var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(icerik);
            dosya.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(dosya, "file", "fis.png");

            return form;
        }

        [Fact]
        public async Task TavanAsilincaFisYuklemeBesYuzUc()
        {
            var client = await SahipAsync("fis");
            TavaniDoldur();

            using var form = Fis();
            var cevap = await client.PostAsync("/api/Receipts", form);

            Assert.Equal(HttpStatusCode.ServiceUnavailable, cevap.StatusCode);
        }

        [Fact]
        public async Task TavanAsilmadanFisYuklemeCalisir()
        {
            var client = await SahipAsync("fisserbest");

            using var form = Fis();
            var cevap = await client.PostAsync("/api/Receipts", form);

            Assert.NotEqual(HttpStatusCode.ServiceUnavailable, cevap.StatusCode);
        }

        [Fact]
        public async Task TavanAsilincaDestekAdresineTekEpostaGider()
        {
            var client = await SahipAsync("bildirim");
            TavaniDoldur();

            var onceki = SahteEpostaGonderici.Ortak.SayiOf("destek@garajim.local");

            using (var form = Fis())
            {
                await client.PostAsync("/api/Receipts", form);
            }

            using (var form = Fis())
            {
                await client.PostAsync("/api/Receipts", form);
            }

            Assert.Equal(onceki + 1, SahteEpostaGonderici.Ortak.SayiOf("destek@garajim.local"));
        }

        [Fact]
        public async Task IstatistiklerAylikTavanVeKalaniGosterir()
        {
            var client = await SahipAsync("stats");
            TavaniDoldur();

            var cevap = await client.GetAsync("/api/Receipts/stats");
            using var belge = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            var veri = belge.RootElement.GetProperty("data");

            Assert.Equal(100, veri.GetProperty("aiTokenTavani").GetInt64());
            Assert.Equal(110, veri.GetProperty("aiTokenKullanilan").GetInt64());
            Assert.Equal(0, veri.GetProperty("aiTokenKalan").GetInt64());
            Assert.True(veri.GetProperty("aiButcesiAsildi").GetBoolean());
        }
    }
}
