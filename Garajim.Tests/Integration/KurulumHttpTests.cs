using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class KurulumHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public KurulumHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Kurulum", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<JsonElement> DurumAsync(HttpClient client)
        {
            var cevap = await client.GetAsync("/api/Kurulum");
            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Fiat",
                model = "Egea",
                year = 2019,
                currentKm = 50000,
                fuelType = "Dizel"
            });
            var govde = await cevap.Content.ReadAsStringAsync();
            Assert.True(cevap.IsSuccessStatusCode, govde);
            return JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        [Fact]
        public async Task YeniHesaptaButunAdimlarBos()
        {
            var client = await SahipAsync("kurulumbos");

            var veri = await DurumAsync(client);

            Assert.False(veri.GetProperty("aracVar").GetBoolean());
            Assert.False(veri.GetProperty("ilkKayitVar").GetBoolean());
            Assert.False(veri.GetProperty("evrakVar").GetBoolean());
            Assert.False(veri.GetProperty("gizlendi").GetBoolean());
            Assert.Equal(0, veri.GetProperty("yuzde").GetInt32());
        }

        [Fact]
        public async Task AracEklenincePayArtar()
        {
            var client = await SahipAsync("kurulumarac");
            await AracEkleAsync(client, "34KR1001");

            var veri = await DurumAsync(client);

            Assert.True(veri.GetProperty("aracVar").GetBoolean());
            Assert.Equal(33, veri.GetProperty("yuzde").GetInt32());
        }

        [Fact]
        public async Task YakitKaydiIlkKaydiSayar()
        {
            var client = await SahipAsync("kurulumyakit");
            var aracId = await AracEkleAsync(client, "34KR1002");

            await client.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId,
                date = DateTime.UtcNow.Date.AddDays(-1),
                km = 50100,
                liters = 40.5,
                totalCost = 1800m,
                tamDolum = true
            });

            var veri = await DurumAsync(client);

            Assert.True(veri.GetProperty("ilkKayitVar").GetBoolean());
            Assert.Equal(66, veri.GetProperty("yuzde").GetInt32());
        }

        [Fact]
        public async Task BakimKaydiDaIlkKaydiSayar()
        {
            var client = await SahipAsync("kurulumbakim");
            var aracId = await AracEkleAsync(client, "34KR1003");

            var cevap = await client.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                date = DateTime.UtcNow.Date.AddDays(-2),
                km = 50200,
                cost = 3200m
            });
            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());

            var veri = await DurumAsync(client);

            Assert.True(veri.GetProperty("ilkKayitVar").GetBoolean());
        }

        [Fact]
        public async Task EvrakUcuncuAdimiTamamlar()
        {
            var client = await SahipAsync("kurulumevrak");
            var aracId = await AracEkleAsync(client, "34KR1004");

            await client.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId,
                date = DateTime.UtcNow.Date.AddDays(-1),
                km = 50100,
                liters = 40.5,
                totalCost = 1800m,
                tamDolum = true
            });

            var evrak = await client.PostAsJsonAsync("/api/Evrak", new
            {
                vehicleId = aracId,
                evrakTuru = "Muayene",
                bitisTarihi = DateTime.UtcNow.Date.AddMonths(6)
            });
            Assert.True(evrak.IsSuccessStatusCode, await evrak.Content.ReadAsStringAsync());

            var veri = await DurumAsync(client);

            Assert.True(veri.GetProperty("evrakVar").GetBoolean());
            Assert.Equal(100, veri.GetProperty("yuzde").GetInt32());
        }

        [Fact]
        public async Task GizlemeKalicidir()
        {
            var client = await SahipAsync("kurulumgizle");

            var gizle = await client.PostAsync("/api/Kurulum/gizle", null);
            Assert.Equal(HttpStatusCode.OK, gizle.StatusCode);

            var veri = await DurumAsync(client);

            Assert.True(veri.GetProperty("gizlendi").GetBoolean());
        }

        [Fact]
        public async Task SurucuyeKapali()
        {
            var sahip = await SahipAsync("kurulumowner");
            var eposta = Eposta("kurulumdriver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var surucu = _factory.CreateClient();
            var giris = await surucu.PostAsJsonAsync("/api/Auth/login",
                new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("token").GetString();
            surucu.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var cevap = await surucu.GetAsync("/api/Kurulum");

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }
    }
}
