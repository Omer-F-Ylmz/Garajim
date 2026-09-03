using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class PlanLimitHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public PlanLimitHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync(string davetKodu = null)
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("plan"), fullName = "Plan Sahip", password = "Test1234!", davetKodu });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static Task<HttpResponseMessage> AracEkleAsync(HttpClient client, string plaka)
        {
            return client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 100000,
                fuelType = "Benzin"
            });
        }

        [Fact]
        public async Task BireyselPlanUcuncuAractanSonra402Doner()
        {
            var sahip = await SahipOlusturAsync();

            for (var i = 1; i <= 3; i++)
            {
                var cevap = await AracEkleAsync(sahip, TestPlaka.Uret());
                Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            }

            var dorduncu = await AracEkleAsync(sahip, TestPlaka.Uret());

            Assert.Equal(HttpStatusCode.PaymentRequired, dorduncu.StatusCode);
            var govde = JsonDocument.Parse(await dorduncu.Content.ReadAsStringAsync()).RootElement;
            Assert.Contains("limit", govde.GetProperty("message").GetString());
        }

        [Fact]
        public async Task LimitSirketBazindaSayilir()
        {
            var birinci = await SahipOlusturAsync();
            for (var i = 1; i <= 3; i++)
            {
                await AracEkleAsync(birinci, TestPlaka.Uret());
            }

            var ikinci = await SahipOlusturAsync();
            var cevap = await AracEkleAsync(ikinci, TestPlaka.Uret());

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        }

        private static async Task<string> DavetKoduAsync(HttpClient client)
        {
            var cevap = await client.GetAsync("/api/Davet");
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("kod").GetString();
        }

        [Fact]
        public async Task HerDavetBireyselPlandaBirAracHakkiAcar()
        {
            var davetEden = await SahipOlusturAsync();

            for (var i = 1; i <= 3; i++)
            {
                await AracEkleAsync(davetEden, TestPlaka.Uret());
            }
            Assert.Equal(HttpStatusCode.PaymentRequired, (await AracEkleAsync(davetEden, TestPlaka.Uret())).StatusCode);

            var kod = await DavetKoduAsync(davetEden);
            await SahipOlusturAsync(kod);

            Assert.Equal(HttpStatusCode.OK, (await AracEkleAsync(davetEden, TestPlaka.Uret())).StatusCode);
            Assert.Equal(HttpStatusCode.PaymentRequired, (await AracEkleAsync(davetEden, TestPlaka.Uret())).StatusCode);
        }

        [Fact]
        public async Task DavetliKendiLimitiniArtirmaz()
        {
            var davetEden = await SahipOlusturAsync();
            var kod = await DavetKoduAsync(davetEden);

            var davetli = await SahipOlusturAsync(kod);

            for (var i = 1; i <= 3; i++)
            {
                Assert.Equal(HttpStatusCode.OK, (await AracEkleAsync(davetli, TestPlaka.Uret())).StatusCode);
            }

            Assert.Equal(HttpStatusCode.PaymentRequired, (await AracEkleAsync(davetli, TestPlaka.Uret())).StatusCode);
        }

        [Fact]
        public async Task PanelKazanilanAracHakkiniLimiteYansitir()
        {
            var davetEden = await SahipOlusturAsync();
            var kod = await DavetKoduAsync(davetEden);
            await SahipOlusturAsync(kod);

            var panel = JsonDocument.Parse(await (await davetEden.GetAsync("/api/Reports/dashboard")).Content.ReadAsStringAsync())
                .RootElement.GetProperty("data");

            Assert.Equal(4, panel.GetProperty("aracLimiti").GetInt32());
        }

        [Fact]
        public async Task SilinenAracLimitiSerbestBirakir()

        {
            var sahip = await SahipOlusturAsync();

            var ilkCevap = await AracEkleAsync(sahip, TestPlaka.Uret());
            var ilkId = JsonDocument.Parse(await ilkCevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
            await AracEkleAsync(sahip, TestPlaka.Uret());
            await AracEkleAsync(sahip, TestPlaka.Uret());

            Assert.Equal(HttpStatusCode.PaymentRequired, (await AracEkleAsync(sahip, TestPlaka.Uret())).StatusCode);

            await sahip.DeleteAsync($"/api/Vehicles/{ilkId}");

            Assert.Equal(HttpStatusCode.OK, (await AracEkleAsync(sahip, TestPlaka.Uret())).StatusCode);
        }
    }
}
