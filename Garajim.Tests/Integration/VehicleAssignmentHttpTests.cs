using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class VehicleAssignmentHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public VehicleAssignmentHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "Filo Sahibi", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> UyeOlusturAsync(HttpClient sahip, string rol)
        {
            var eposta = Eposta(rol.ToLowerInvariant());
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = $"{rol} Kişi", role = rol });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            var sifre = veri.GetProperty("temporaryPassword").GetString();
            var userId = veri.GetProperty("userId").GetInt32();

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = sifre });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, userId);
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2018,
                currentKm = 100000,
                fuelType = "Benzin"
            });

            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        [Fact]
        public async Task ZimmetVerilenSurucuAraciGorur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34ZIM111");
            var (surucu, surucuId) = await UyeOlusturAsync(sahip, "Driver");

            var oncesi = await surucu.GetAsync($"/api/Vehicles/{aracId}");
            var zimmet = await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = surucuId });
            var sonrasi = await surucu.GetAsync($"/api/Vehicles/{aracId}");

            Assert.Equal(HttpStatusCode.NotFound, oncesi.StatusCode);
            Assert.Equal(HttpStatusCode.OK, zimmet.StatusCode);
            Assert.Equal(HttpStatusCode.OK, sonrasi.StatusCode);
        }

        [Fact]
        public async Task AyniAracaIkinciAktifZimmetReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34ZIM222");
            var (_, birinciId) = await UyeOlusturAsync(sahip, "Driver");
            var (_, ikinciId) = await UyeOlusturAsync(sahip, "Driver");

            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = birinciId });
            var ikinci = await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = ikinciId });
            var govde = await ikinci.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.BadRequest, ikinci.StatusCode);
            Assert.Contains("zimmet", govde, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DevirdeEskiSurucununErisimiDuserYeninkiAcilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34ZIM333");
            var (eski, eskiId) = await UyeOlusturAsync(sahip, "Driver");
            var (yeni, yeniId) = await UyeOlusturAsync(sahip, "Driver");

            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = eskiId });
            var devir = await sahip.PutAsJsonAsync("/api/Assignments/transfer", new { vehicleId = aracId, userId = yeniId });

            var eskiErisim = await eski.GetAsync($"/api/Vehicles/{aracId}");
            var yeniErisim = await yeni.GetAsync($"/api/Vehicles/{aracId}");

            Assert.Equal(HttpStatusCode.OK, devir.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, eskiErisim.StatusCode);
            Assert.Equal(HttpStatusCode.OK, yeniErisim.StatusCode);
        }

        [Fact]
        public async Task DevirGecmisteIkiKayitBirakirEskisiKapali()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34ZIM444");
            var (_, eskiId) = await UyeOlusturAsync(sahip, "Driver");
            var (_, yeniId) = await UyeOlusturAsync(sahip, "Driver");

            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = eskiId });
            await sahip.PutAsJsonAsync("/api/Assignments/transfer", new { vehicleId = aracId, userId = yeniId });

            var gecmis = await sahip.GetStringAsync($"/api/Assignments?vehicleId={aracId}");
            var kayitlar = JsonDocument.Parse(gecmis).RootElement.GetProperty("data");

            Assert.Equal(2, kayitlar.GetArrayLength());
            var acik = kayitlar.EnumerateArray().Count(k => k.GetProperty("endDate").ValueKind == JsonValueKind.Null);
            Assert.Equal(1, acik);
        }

        [Fact]
        public async Task ZimmetSonlandirilincaSurucuErisimiDuser()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34ZIM555");
            var (surucu, surucuId) = await UyeOlusturAsync(sahip, "Driver");

            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = surucuId });
            var bitir = await sahip.PutAsJsonAsync("/api/Assignments/end", new { vehicleId = aracId });
            var sonrasi = await surucu.GetAsync($"/api/Vehicles/{aracId}");

            Assert.Equal(HttpStatusCode.OK, bitir.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, sonrasi.StatusCode);
        }

        [Fact]
        public async Task SonlandirmaSonrasiYeniZimmetVerilebilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34ZIM666");
            var (_, birinciId) = await UyeOlusturAsync(sahip, "Driver");
            var (ikinci, ikinciId) = await UyeOlusturAsync(sahip, "Driver");

            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = birinciId });
            await sahip.PutAsJsonAsync("/api/Assignments/end", new { vehicleId = aracId });
            var yeniZimmet = await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = ikinciId });

            Assert.Equal(HttpStatusCode.OK, yeniZimmet.StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await ikinci.GetAsync($"/api/Vehicles/{aracId}")).StatusCode);
        }

        [Fact]
        public async Task ManagerZimmetVerebilirDriverVeremez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34ZIM777");
            var (manager, _) = await UyeOlusturAsync(sahip, "Manager");
            var (driver, driverId) = await UyeOlusturAsync(sahip, "Driver");

            var managerZimmet = await manager.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = driverId });
            var driverZimmet = await driver.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = driverId });

            Assert.Equal(HttpStatusCode.OK, managerZimmet.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, driverZimmet.StatusCode);
        }

        [Fact]
        public async Task BaskaSirketinAracinaZimmetVerilemez()
        {
            var sahipA = await SahipOlusturAsync();
            var sahipB = await SahipOlusturAsync();
            var bAraci = await AracEkleAsync(sahipB, "06ZIM888");
            var (_, aSurucuId) = await UyeOlusturAsync(sahipA, "Driver");

            var cevap = await sahipA.PostAsJsonAsync("/api/Assignments", new { vehicleId = bAraci, userId = aSurucuId });

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task BaskaSirketinKullanicisinaZimmetVerilemez()
        {
            var sahipA = await SahipOlusturAsync();
            var sahipB = await SahipOlusturAsync();
            var aAraci = await AracEkleAsync(sahipA, "34ZIM999");
            var (_, bSurucuId) = await UyeOlusturAsync(sahipB, "Driver");

            var cevap = await sahipA.PostAsJsonAsync("/api/Assignments", new { vehicleId = aAraci, userId = bSurucuId });

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task ZimmetliSurucuAracaKayitGirebilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34ZIM100");
            var (surucu, surucuId) = await UyeOlusturAsync(sahip, "Driver");
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = surucuId });

            var cevap = await surucu.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId,
                date = "2026-03-01",
                km = 101000,
                liters = 40,
                totalCost = 1800
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        }
    }
}
