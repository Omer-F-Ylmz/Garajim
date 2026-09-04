using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class MaliyetTutarlilikTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public MaliyetTutarlilikTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("maliyet"), fullName = "Maliyet Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        private static async Task<int> AracEkleAsync(HttpClient client)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Fiat",
                model = "Egea",
                year = 2020,
                currentKm = 10000,
                fuelType = "Benzin"
            });

            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static Task YakitEkleAsync(HttpClient client, int aracId, string tarih, int km, decimal litre, decimal tutar, bool tamDolum)
        {
            return client.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId,
                date = tarih,
                km,
                liters = litre,
                totalCost = tutar,
                tamDolum
            });
        }

        [Fact]
        public async Task FiloMaliyetiAracMaliyetiyleAyniMesafeyiKullanir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            await YakitEkleAsync(sahip, aracId, "2026-07-01", 10100, 40m, 2000m, true);
            await YakitEkleAsync(sahip, aracId, "2026-07-10", 10300, 20m, 1000m, false);
            await YakitEkleAsync(sahip, aracId, "2026-07-20", 10500, 30m, 1500m, true);
            await YakitEkleAsync(sahip, aracId, "2026-07-28", 10750, 25m, 1250m, false);

            var arac = await VeriAsync(await sahip.GetAsync(
                $"/api/Vehicles/{aracId}/maliyet?baslangic=2026-07-01&bitis=2026-07-31"));

            var filo = await VeriAsync(await sahip.GetAsync(
                "/api/Reports/filo-maliyet?baslangic=2026-07-01&bitis=2026-07-31"));

            var satir = filo.GetProperty("araclar").EnumerateArray()
                .Single(a => a.GetProperty("vehicleId").GetInt32() == aracId);

            Assert.Equal(arac.GetProperty("mesafeKm").GetInt32(), satir.GetProperty("mesafeKm").GetInt32());
            Assert.Equal(arac.GetProperty("maliyetKmBasi").GetDecimal(), satir.GetProperty("maliyetKmBasi").GetDecimal());
            Assert.Equal(arac.GetProperty("litre100Km").GetDecimal(), satir.GetProperty("litre100Km").GetDecimal());
        }

        [Fact]
        public async Task FiloMaliyetiKuyruktakiKismiDolumuOlcmez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            await YakitEkleAsync(sahip, aracId, "2026-07-01", 20100, 40m, 2000m, true);
            await YakitEkleAsync(sahip, aracId, "2026-07-15", 20500, 30m, 1500m, true);
            await YakitEkleAsync(sahip, aracId, "2026-07-25", 20900, 20m, 1000m, false);

            var filo = await VeriAsync(await sahip.GetAsync(
                "/api/Reports/filo-maliyet?baslangic=2026-07-01&bitis=2026-07-31"));

            var satir = filo.GetProperty("araclar").EnumerateArray()
                .Single(a => a.GetProperty("vehicleId").GetInt32() == aracId);

            Assert.Equal(400, satir.GetProperty("mesafeKm").GetInt32());
        }
    }
}
