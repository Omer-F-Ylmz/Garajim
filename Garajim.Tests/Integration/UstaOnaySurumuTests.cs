using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class UstaYeniSurumFactory : UstaWebApplicationFactory
    {
        public const string YeniSurum = "2027-01-v2";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Usta:OnaySurumu"] = YeniSurum
                });
            });
        }
    }

    public class UstaOnaySurumuTests : IClassFixture<UstaYeniSurumFactory>
    {
        private readonly UstaYeniSurumFactory _factory;

        public UstaOnaySurumuTests(UstaYeniSurumFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("surum"), fullName = "Sürüm Sahip", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task GuncelSurumYapilandirmadanOkunur()
        {
            var sahip = await SahipOlusturAsync();

            var durum = await VeriAsync(await sahip.GetAsync("/api/Usta/onay"));

            Assert.Equal(UstaYeniSurumFactory.YeniSurum, durum.GetProperty("guncelSurum").GetString());
            Assert.True(durum.GetProperty("onayGerekli").GetBoolean());
            Assert.Equal("/sartlar.html", durum.GetProperty("metinBagi").GetString());
        }

        [Fact]
        public async Task EskiSurumOnayiYeniSurumdeGecmez()
        {
            var sahip = await SahipOlusturAsync();

            var eski = await sahip.PostAsJsonAsync("/api/Usta/onay", new { metinSurumu = "2026-09-v1" });

            Assert.Equal(HttpStatusCode.BadRequest, eski.StatusCode);
            Assert.True((await VeriAsync(await sahip.GetAsync("/api/Usta/onay"))).GetProperty("onayGerekli").GetBoolean());
        }

        [Fact]
        public async Task YeniSurumOnaylanincaKapiAcilir()
        {
            var sahip = await SahipOlusturAsync();
            var arac = await sahip.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = "34SR" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant(),
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 100000,
                fuelType = "Benzin"
            });
            var aracId = (await VeriAsync(arac)).GetProperty("id").GetInt32();

            Assert.Equal(HttpStatusCode.Forbidden, (await sahip.PostAsJsonAsync("/api/Usta/sohbet", new { vehicleId = aracId })).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await sahip.PostAsJsonAsync("/api/Usta/onay", new { metinSurumu = UstaYeniSurumFactory.YeniSurum })).StatusCode);

            Assert.Equal(HttpStatusCode.OK, (await sahip.PostAsJsonAsync("/api/Usta/sohbet", new { vehicleId = aracId })).StatusCode);
            Assert.False((await VeriAsync(await sahip.GetAsync("/api/Usta/onay"))).GetProperty("onayGerekli").GetBoolean());
        }
    }

    public class SartlarSayfasiTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public SartlarSayfasiTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task SartlarSayfasiGirissizAcilirVeZorunluMaddeleriIcerir()
        {
            var cevap = await _factory.CreateClient().GetAsync("/sartlar.html");
            var metin = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Contains("2026-09-v1", metin);
            Assert.Contains("AI Usta", metin);
            Assert.Contains("Gemini", metin);
            Assert.Contains("24 ay", metin);
            Assert.Contains("silinmesini isteme", metin);
            Assert.Contains("kendiniz de silebilirsiniz", metin);
            Assert.Contains("sorumlu değildir", metin);
            Assert.Contains("6698", metin);
        }
    }
}
