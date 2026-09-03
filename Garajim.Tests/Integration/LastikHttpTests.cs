using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class LastikHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public LastikHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static string Plaka() => TestPlaka.Uret();

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("lastik"), fullName = "Lastik Sahip", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("lastikdriver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Lastik Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
        }

        private static async Task<int> AracEkleAsync(HttpClient client)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = Plaka(),
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 100000,
                fuelType = "Benzin"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static Task<HttpResponseMessage> TakAsync(HttpClient client, int aracId, string ad, string mevsim, int km, string tarih, decimal? dis = null)
        {
            return client.PostAsJsonAsync("/api/Lastik", new
            {
                vehicleId = aracId,
                ad,
                mevsim,
                marka = "Michelin",
                ebat = "205/55 R16",
                disDerinligiMm = dis,
                takilmaTarihi = tarih,
                takilmaKm = km
            });
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task SetTakilirVeDurumdaGorunur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            await TakAsync(sahip, aracId, "Yaz 2026", "Yaz", 100000, "2026-04-01", 7.5m);

            var durum = await VeriAsync(await sahip.GetAsync($"/api/Lastik?vehicleId={aracId}"));

            Assert.Equal("Yaz 2026", durum.GetProperty("takiliSet").GetProperty("ad").GetString());
            Assert.Equal("Yaz", durum.GetProperty("takiliSet").GetProperty("mevsim").GetString());
            Assert.True(durum.GetProperty("takiliSet").GetProperty("takili").GetBoolean());
            Assert.Equal(1, durum.GetProperty("setler").GetArrayLength());
        }

        [Fact]
        public async Task SokulenSetToplamKmyiDegismezOlarakTasir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var id = (await VeriAsync(await TakAsync(sahip, aracId, "Kış 2026", "Kis", 100000, "2026-01-05"))).GetProperty("id").GetInt32();

            await sahip.PutAsJsonAsync($"/api/Lastik/{id}/sok", new { sokulmeTarihi = "2026-04-01", sokulmeKm = 108400, disDerinligiMm = 5.2m });

            var durum = await VeriAsync(await sahip.GetAsync($"/api/Lastik?vehicleId={aracId}"));
            var set = durum.GetProperty("setler")[0];

            Assert.False(set.GetProperty("takili").GetBoolean());
            Assert.Equal(8400, set.GetProperty("toplamKm").GetInt32());
            Assert.Equal(set.GetProperty("sokulmeKm").GetInt32() - set.GetProperty("takilmaKm").GetInt32(), set.GetProperty("toplamKm").GetInt32());
            Assert.Equal(5.2m, set.GetProperty("disDerinligiMm").GetDecimal());
            Assert.Equal(JsonValueKind.Null, durum.GetProperty("takiliSet").ValueKind);
        }

        [Fact]
        public async Task YeniSetTakilincaEskiSetOtomatikSokulur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            await TakAsync(sahip, aracId, "Kış 2026", "Kis", 100000, "2026-01-05");
            await TakAsync(sahip, aracId, "Yaz 2026", "Yaz", 108400, "2026-04-01");

            var durum = await VeriAsync(await sahip.GetAsync($"/api/Lastik?vehicleId={aracId}"));

            Assert.Equal("Yaz 2026", durum.GetProperty("takiliSet").GetProperty("ad").GetString());

            var eski = durum.GetProperty("setler").EnumerateArray().Single(s => s.GetProperty("ad").GetString() == "Kış 2026");
            Assert.False(eski.GetProperty("takili").GetBoolean());
            Assert.Equal(8400, eski.GetProperty("toplamKm").GetInt32());
            Assert.Equal(108400, eski.GetProperty("sokulmeKm").GetInt32());
        }

        [Fact]
        public async Task GeriyeDonukKilometreyleTakmaReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            await TakAsync(sahip, aracId, "Kış 2026", "Kis", 100000, "2026-01-05");

            var cevap = await TakAsync(sahip, aracId, "Yaz 2026", "Yaz", 99000, "2026-04-01");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task GecersizSokulmeReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var id = (await VeriAsync(await TakAsync(sahip, aracId, "Kış 2026", "Kis", 100000, "2026-01-05"))).GetProperty("id").GetInt32();

            var kmHatasi = await sahip.PutAsJsonAsync($"/api/Lastik/{id}/sok", new { sokulmeTarihi = "2026-04-01", sokulmeKm = 99000 });
            var tarihHatasi = await sahip.PutAsJsonAsync($"/api/Lastik/{id}/sok", new { sokulmeTarihi = "2025-12-01", sokulmeKm = 108000 });

            Assert.Equal(HttpStatusCode.BadRequest, kmHatasi.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, tarihHatasi.StatusCode);
        }

        [Fact]
        public async Task SokulmusSetTekrarSokulemez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var id = (await VeriAsync(await TakAsync(sahip, aracId, "Kış 2026", "Kis", 100000, "2026-01-05"))).GetProperty("id").GetInt32();
            await sahip.PutAsJsonAsync($"/api/Lastik/{id}/sok", new { sokulmeTarihi = "2026-04-01", sokulmeKm = 108400 });

            var cevap = await sahip.PutAsJsonAsync($"/api/Lastik/{id}/sok", new { sokulmeTarihi = "2026-05-01", sokulmeKm = 109000 });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Theory]
        [InlineData("Bahar")]
        [InlineData("9")]
        public async Task TanimsizMevsimReddedilir(string mevsim)
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var cevap = await TakAsync(sahip, aracId, "Test", mevsim, 100000, "2026-04-01");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task SetYokkenUyariVerilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var durum = await VeriAsync(await sahip.GetAsync($"/api/Lastik?vehicleId={aracId}"));

            Assert.Equal(JsonValueKind.Null, durum.GetProperty("takiliSet").ValueKind);
            Assert.Contains("takılı", durum.GetProperty("uyari").GetString());
        }

        [Fact]
        public async Task YasalSinirdakiDisDerinligiUyariVerir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            await TakAsync(sahip, aracId, "Yaz 2026", "DortMevsim", 100000, "2026-04-01", 1.5m);

            var durum = await VeriAsync(await sahip.GetAsync($"/api/Lastik?vehicleId={aracId}"));

            Assert.Contains("Diş derinliği", durum.GetProperty("uyari").GetString());
        }

        [Fact]
        public async Task DriverLastikYazamaz()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = surucuId });

            var yazma = await TakAsync(surucu, aracId, "Yaz", "Yaz", 100000, "2026-04-01");
            var okuma = await surucu.GetAsync($"/api/Lastik?vehicleId={aracId}");

            Assert.Equal(HttpStatusCode.Forbidden, yazma.StatusCode);
            Assert.Equal(HttpStatusCode.OK, okuma.StatusCode);
        }

        [Fact]
        public async Task YabanciSirketinAraci404Doner()
        {
            var birinci = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci);
            await TakAsync(birinci, aracId, "Yaz", "Yaz", 100000, "2026-04-01");

            var ikinci = await SahipOlusturAsync();
            var cevap = await ikinci.GetAsync($"/api/Lastik?vehicleId={aracId}");

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task SetSilinebilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var id = (await VeriAsync(await TakAsync(sahip, aracId, "Yaz", "Yaz", 100000, "2026-04-01"))).GetProperty("id").GetInt32();

            Assert.Equal(HttpStatusCode.OK, (await sahip.DeleteAsync($"/api/Lastik/{id}")).StatusCode);

            var durum = await VeriAsync(await sahip.GetAsync($"/api/Lastik?vehicleId={aracId}"));
            Assert.Equal(0, durum.GetProperty("setler").GetArrayLength());
        }
    }
}
