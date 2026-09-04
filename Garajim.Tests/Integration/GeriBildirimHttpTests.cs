using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class GeriBildirimHttpTests : IDisposable
    {
        private class DestekliFactory : GarajimWebApplicationFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["App:DestekEposta"] = "destek@garajim.local"
                    });
                });
            }
        }

        private readonly DestekliFactory _factory = new DestekliFactory();

        public void Dispose()
        {
            _factory.Dispose();
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Geri", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static Task<HttpResponseMessage> GonderAsync(HttpClient client, string mesaj, string tur = "Oneri")
        {
            return client.PostAsJsonAsync("/api/GeriBildirim", new
            {
                tur,
                mesaj,
                sayfa = "bakim",
                surum = "1.0.0+test"
            });
        }

        [Fact]
        public async Task GeriBildirimKaydedilirVeDestegeEpostaGider()
        {
            var client = await SahipAsync("geriekle");
            var imza = "Fiş okuma çok işime yaradı " + Guid.NewGuid().ToString("N");

            var cevap = await GonderAsync(client, imza);

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Contains(SahteEpostaGonderici.Ortak.Gonderilenler, e => e.Govde.Contains(imza));
        }

        [Fact]
        public async Task GirissizReddedilir()
        {
            var cevap = await GonderAsync(_factory.CreateClient(), "Anonim mesaj");

            Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
        }

        [Fact]
        public async Task BosMesajReddedilir()
        {
            var client = await SahipAsync("geribos");

            var cevap = await GonderAsync(client, "   ");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task UygunsuzIfadeReddedilir()
        {
            var client = await SahipAsync("geriuygunsuz");

            var cevap = await GonderAsync(client, "Bu uygulama tam bir dallama işi.");
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
            Assert.Contains("uygun", govde, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task GunlukBesSiniriVar()
        {
            var client = await SahipAsync("gerilimit");

            for (var i = 1; i <= 5; i++)
            {
                var ok = await GonderAsync(client, "Geri bildirim " + i);
                Assert.True(ok.IsSuccessStatusCode, await ok.Content.ReadAsStringAsync());
            }

            var altinci = await GonderAsync(client, "Geri bildirim 6");

            Assert.Equal(HttpStatusCode.TooManyRequests, altinci.StatusCode);
        }

        [Fact]
        public async Task BilinmeyenTurReddedilir()
        {
            var client = await SahipAsync("geritur");

            var cevap = await GonderAsync(client, "Deneme", "Sacma");

            Assert.False(cevap.IsSuccessStatusCode);
        }

        [Fact]
        public async Task SirketIzolasyonuKorunur()
        {
            var birinci = await SahipAsync("geriizolasyon1");
            var ikinci = await SahipAsync("geriizolasyon2");

            await GonderAsync(birinci, "Birinci şirketin geri bildirimi");
            await GonderAsync(ikinci, "İkinci şirketin geri bildirimi");

            var liste = await ikinci.GetAsync("/api/GeriBildirim");
            var veri = JsonDocument.Parse(await liste.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.Equal(1, veri.GetArrayLength());
            Assert.Contains("İkinci", veri[0].GetProperty("mesaj").GetString());
        }
    }
}
