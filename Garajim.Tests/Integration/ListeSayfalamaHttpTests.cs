using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class ListeSayfalamaHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public ListeSayfalamaHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int AracId)> HazirlaAsync(string on, string plaka)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Liste", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Fiat",
                model = "Egea",
                year = 2019,
                currentKm = 40000,
                fuelType = "Dizel"
            });

            var govde = await arac.Content.ReadAsStringAsync();
            Assert.True(arac.IsSuccessStatusCode, govde);

            var id = JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();

            return (client, id);
        }

        private static async Task BakimEkleAsync(HttpClient client, int aracId, int gunOnce, int km, string servis, string not)
        {
            var cevap = await client.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                date = DateTime.UtcNow.Date.AddDays(-gunOnce),
                km,
                cost = 1000m + km,
                serviceName = servis,
                note = not
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        private static async Task<JsonElement> VeriAsync(HttpClient client, string yol)
        {
            var cevap = await client.GetAsync(yol);
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.True(cevap.IsSuccessStatusCode, govde);

            return JsonDocument.Parse(govde).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task ParametresizIstekEskiDuzBicimiKorur()
        {
            var (client, aracId) = await HazirlaAsync("listeduz", "34LS1001");
            await BakimEkleAsync(client, aracId, 3, 41000, "Oto Merkez", "yağ değişimi");

            var veri = await VeriAsync(client, "/api/Maintenance?vehicleId=" + aracId);

            Assert.Equal(JsonValueKind.Array, veri.ValueKind);
            Assert.Equal(1, veri.GetArrayLength());
        }

        [Fact]
        public async Task SayfaParametresiZarfDondurur()
        {
            var (client, aracId) = await HazirlaAsync("listezarf", "34LS1002");

            for (var i = 0; i < 7; i++)
            {
                await BakimEkleAsync(client, aracId, i + 1, 41000 + i * 100, "Servis " + i, "not " + i);
            }

            var veri = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&sayfa=2&boyut=3");

            Assert.Equal(JsonValueKind.Object, veri.ValueKind);
            Assert.Equal(7, veri.GetProperty("toplam").GetInt32());
            Assert.Equal(2, veri.GetProperty("sayfa").GetInt32());
            Assert.Equal(3, veri.GetProperty("boyut").GetInt32());
            Assert.Equal(3, veri.GetProperty("kayitlar").GetArrayLength());
        }

        [Fact]
        public async Task BoyutYuzUstuneCikmaz()
        {
            var (client, aracId) = await HazirlaAsync("listeboyut", "34LS1003");
            await BakimEkleAsync(client, aracId, 1, 41000, "Servis", "not");

            var veri = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&boyut=101");

            Assert.Equal(100, veri.GetProperty("boyut").GetInt32());
        }

        [Fact]
        public async Task SiralamaCalisir()
        {
            var (client, aracId) = await HazirlaAsync("listesirala", "34LS1004");
            await BakimEkleAsync(client, aracId, 10, 41000, "Eski", "a");
            await BakimEkleAsync(client, aracId, 1, 45000, "Yeni", "b");

            var artan = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&sirala=km:asc");
            var azalan = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&sirala=km:desc");

            Assert.Equal(41000, artan.GetProperty("kayitlar")[0].GetProperty("km").GetInt32());
            Assert.Equal(45000, azalan.GetProperty("kayitlar")[0].GetProperty("km").GetInt32());
        }

        [Fact]
        public async Task GecersizSiralamaAlaniDortYuzDoner()
        {
            var (client, aracId) = await HazirlaAsync("listekotu", "34LS1005");

            var cevap = await client.GetAsync($"/api/Maintenance?vehicleId={aracId}&sirala=plaka:desc");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task TurkceAramaBuyukKucukDuyarsizdir()
        {
            var (client, aracId) = await HazirlaAsync("listeara", "34LS1006");
            await BakimEkleAsync(client, aracId, 5, 41000, "Oto Merkez", "yağ filtresi değişti");
            await BakimEkleAsync(client, aracId, 4, 42000, "Lastikçi", "rot balans");

            var veri = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&q=YA%C4%9E");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
            Assert.Contains("yağ", veri.GetProperty("kayitlar")[0].GetProperty("note").GetString());
        }

        [Fact]
        public async Task AramaServisAdindaDaCalisir()
        {
            var (client, aracId) = await HazirlaAsync("listeservis", "34LS1007");
            await BakimEkleAsync(client, aracId, 5, 41000, "Şahin Oto", "bakım");
            await BakimEkleAsync(client, aracId, 4, 42000, "Lastikçi", "rot");

            var veri = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&q=sahin");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
        }

        [Fact]
        public async Task TarihAraligiSuzer()
        {
            var (client, aracId) = await HazirlaAsync("listetarih", "34LS1008");
            await BakimEkleAsync(client, aracId, 40, 41000, "Eski", "a");
            await BakimEkleAsync(client, aracId, 2, 42000, "Yeni", "b");

            var bas = DateTime.UtcNow.Date.AddDays(-10).ToString("yyyy-MM-dd");
            var veri = await VeriAsync(client, $"/api/Maintenance?vehicleId={aracId}&baslangic={bas}");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
            Assert.Equal("Yeni", veri.GetProperty("kayitlar")[0].GetProperty("serviceName").GetString());
        }

        [Fact]
        public async Task BaskaSirketinAracinaErisilemez()
        {
            var (_, yabanciAracId) = await HazirlaAsync("listeyabanci", "34LS1009");
            var (client, _) = await HazirlaAsync("listebenim", "34LS1010");

            var cevap = await client.GetAsync($"/api/Maintenance?vehicleId={yabanciAracId}&sayfa=1");

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }
    }
}
