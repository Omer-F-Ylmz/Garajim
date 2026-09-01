using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class DashboardHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public DashboardHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("dash"), fullName = "Panel Sahip", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("dashdriver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Panel Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 100000,
                fuelType = "Benzin"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task<JsonElement> PaneAsync(HttpClient client)
        {
            var cevap = await client.GetAsync("/api/Reports/dashboard");
            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task BosSirketteSayilarSifirdirLimitDoner()
        {
            var sahip = await SahipOlusturAsync();

            var veri = await PaneAsync(sahip);

            Assert.Equal(0, veri.GetProperty("aracSayisi").GetInt32());
            Assert.Equal(3, veri.GetProperty("aracLimiti").GetInt32());
            Assert.Equal("Bireysel", veri.GetProperty("plan").GetString());
            Assert.Equal(0, veri.GetProperty("evrakGecti").GetInt32());
            Assert.Equal(0, veri.GetProperty("evrakYaklasiyor").GetInt32());
            Assert.Equal(0, veri.GetProperty("hatirlatmaYaklasiyor").GetInt32());
            Assert.Equal(0m, veri.GetProperty("buAyMaliyet").GetDecimal());
        }

        [Fact]
        public async Task AracVeZimmetSayilariGorunur()
        {
            var sahip = await SahipOlusturAsync();
            var ek = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();
            var aracId = await AracEkleAsync(sahip, $"34PN{ek}");
            var (_, surucuId) = await SurucuOlusturAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = surucuId });

            var veri = await PaneAsync(sahip);

            Assert.Equal(1, veri.GetProperty("aracSayisi").GetInt32());
            Assert.Equal(1, veri.GetProperty("aktifZimmet").GetInt32());
        }

        [Fact]
        public async Task EvrakDurumlariSayilir()
        {
            var sahip = await SahipOlusturAsync();
            var ek = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();
            var aracId = await AracEkleAsync(sahip, $"06PN{ek}");

            var bugun = DateTime.UtcNow.Date;
            await sahip.PostAsJsonAsync("/api/Evrak", new { vehicleId = aracId, evrakTuru = "Muayene", bitisTarihi = bugun.AddDays(-5).ToString("yyyy-MM-dd") });
            await sahip.PostAsJsonAsync("/api/Evrak", new { vehicleId = aracId, evrakTuru = "Kasko", bitisTarihi = bugun.AddDays(10).ToString("yyyy-MM-dd") });
            await sahip.PostAsJsonAsync("/api/Evrak", new { vehicleId = aracId, evrakTuru = "TrafikSigortasi", bitisTarihi = bugun.AddDays(200).ToString("yyyy-MM-dd") });

            var veri = await PaneAsync(sahip);

            Assert.Equal(1, veri.GetProperty("evrakGecti").GetInt32());
            Assert.Equal(1, veri.GetProperty("evrakYaklasiyor").GetInt32());
        }

        [Fact]
        public async Task BuAyMaliyetiVeGecenAyKarsilastirmasiHesaplanir()
        {
            var sahip = await SahipOlusturAsync();
            var ek = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();
            var aracId = await AracEkleAsync(sahip, $"35PN{ek}");

            var buAy = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var gecenAy = buAy.AddMonths(-1);

            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = buAy.ToString("yyyy-MM-dd"), liters = 40m, totalCost = 2000m, km = 100100 });
            await sahip.PostAsJsonAsync("/api/Expenses", new { vehicleId = aracId, category = "Otopark", date = buAy.ToString("yyyy-MM-dd"), amount = 500m, note = "otopark" });
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = gecenAy.ToString("yyyy-MM-dd"), liters = 30m, totalCost = 1500m, km = 100000 });

            var veri = await PaneAsync(sahip);

            Assert.Equal(2500m, veri.GetProperty("buAyMaliyet").GetDecimal());
            Assert.Equal(1500m, veri.GetProperty("gecenAyMaliyet").GetDecimal());
        }

        [Fact]
        public async Task HatirlatmaYaklasanlarSayilir()
        {
            var sahip = await SahipOlusturAsync();
            var ek = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();
            var aracId = await AracEkleAsync(sahip, $"41PN{ek}");

            var bugun = DateTime.UtcNow.Date;
            await sahip.PostAsJsonAsync("/api/Reminders", new { vehicleId = aracId, type = "Muayene", dueDate = bugun.AddDays(10).ToString("yyyy-MM-dd"), note = "yakin" });
            await sahip.PostAsJsonAsync("/api/Reminders", new { vehicleId = aracId, type = "TrafikSigortasi", dueDate = bugun.AddDays(200).ToString("yyyy-MM-dd"), note = "uzak" });

            var veri = await PaneAsync(sahip);

            Assert.Equal(1, veri.GetProperty("hatirlatmaYaklasiyor").GetInt32());
        }

        [Fact]
        public async Task SirketlerBirbirininVerisiniGormez()
        {
            var birinci = await SahipOlusturAsync();
            var ek = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();
            await AracEkleAsync(birinci, $"07PN{ek}");

            var ikinci = await SahipOlusturAsync();
            var veri = await PaneAsync(ikinci);

            Assert.Equal(0, veri.GetProperty("aracSayisi").GetInt32());
        }

        [Fact]
        public async Task DriverYalnizZimmetliAracinipanelindeGorur()
        {
            var sahip = await SahipOlusturAsync();
            var ek = Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();
            var zimmetli = await AracEkleAsync(sahip, $"16PN{ek}");
            await AracEkleAsync(sahip, $"17PN{ek}");

            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = zimmetli, userId = surucuId });

            var veri = await PaneAsync(surucu);

            Assert.Equal(1, veri.GetProperty("aracSayisi").GetInt32());
        }
    }
}
