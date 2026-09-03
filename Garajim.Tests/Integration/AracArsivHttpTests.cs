using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class AracArsivHttpTests : IDisposable
    {
        private sealed class DarPlanFactory : GarajimWebApplicationFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Plan:BireyselAracLimiti"] = "2",
                        ["Plan:DavetMaxEkArac"] = "0"
                    });
                });
            }
        }

        private readonly DarPlanFactory _factory = new DarPlanFactory();

        public void Dispose() => _factory.Dispose();

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Arşiv Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<int> AracEkleAsync(HttpClient client)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Fiat", model = "Egea", year = 2020, currentKm = 40000,
                fuelType = "Dizel", vites = "Manuel", kasaTipi = "Sedan"
            });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());

            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static Task<HttpResponseMessage> ArsivleAsync(HttpClient client, int aracId, string neden = "Satildi") =>
            client.PostAsJsonAsync($"/api/Vehicles/{aracId}/arsiv", new { neden });

        [Fact]
        public async Task ArsivlenenAracPlanLimitineSayilmaz()
        {
            var client = await SahipAsync("limit");
            var birinci = await AracEkleAsync(client);
            await AracEkleAsync(client);

            var dolu = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(), brand = "Fiat", model = "Egea", year = 2020,
                currentKm = 1000, fuelType = "Dizel", vites = "Manuel", kasaTipi = "Sedan"
            });
            Assert.Equal(HttpStatusCode.PaymentRequired, dolu.StatusCode);

            Assert.True((await ArsivleAsync(client, birinci)).IsSuccessStatusCode);

            var ucuncu = await AracEkleAsync(client);
            Assert.True(ucuncu > 0);
        }

        [Fact]
        public async Task ArsivlenenAraceKayitEklenemez()
        {
            var client = await SahipAsync("kayit");
            var aracId = await AracEkleAsync(client);
            await ArsivleAsync(client, aracId);

            var cevap = await client.PostAsJsonAsync("/api/Expenses", new
            {
                vehicleId = aracId, date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                category = "Otopark", amount = 100m, note = "Arşivli"
            });

            Assert.Equal(HttpStatusCode.Conflict, cevap.StatusCode);
        }

        [Fact]
        public async Task ArsivListeleriAyrilir()
        {
            var client = await SahipAsync("liste");
            var aracId = await AracEkleAsync(client);
            await AracEkleAsync(client);
            await ArsivleAsync(client, aracId);

            var aktif = await client.GetAsync("/api/Vehicles");
            var arsiv = await client.GetAsync("/api/Vehicles?arsiv=true");

            using var aktifBelge = JsonDocument.Parse(await aktif.Content.ReadAsStringAsync());
            using var arsivBelge = JsonDocument.Parse(await arsiv.Content.ReadAsStringAsync());

            Assert.Equal(1, aktifBelge.RootElement.GetProperty("data").GetArrayLength());
            Assert.Equal(1, arsivBelge.RootElement.GetProperty("data").GetArrayLength());
            Assert.True(arsivBelge.RootElement.GetProperty("data")[0].GetProperty("arsivli").GetBoolean());
        }

        [Fact]
        public async Task ArsivlenenAracinKarneBaglantisiCalismayaDevamEder()
        {
            var client = await SahipAsync("karne");
            var aracId = await AracEkleAsync(client);

            var karne = await client.PostAsJsonAsync($"/api/Vehicles/{aracId}/karne", new
            {
                kapsam = new
                {
                    bakimGecmisi = true, parcaHafizasi = false, yakitOzeti = false, belgeler = false,
                    plakaGoster = true, tutarGoster = false, acilKart = false, hasarGecmisi = false, beyanDegeri = false
                },
                sonKullanmaGun = 90
            });
            var url = JsonDocument.Parse(await karne.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("url").GetString();
            var token = url.Substring(url.IndexOf("t=", StringComparison.Ordinal) + 2);

            await ArsivleAsync(client, aracId);

            var anonim = _factory.CreateClient();
            var goruntu = await anonim.GetAsync($"/api/karne/{token}");

            Assert.Equal(HttpStatusCode.OK, goruntu.StatusCode);
        }

        [Fact]
        public async Task ArsivdenGeriAlmaLimitiDenetler()
        {
            var client = await SahipAsync("geri");
            var birinci = await AracEkleAsync(client);
            await ArsivleAsync(client, birinci);

            await AracEkleAsync(client);
            await AracEkleAsync(client);

            var geri = await client.PostAsync($"/api/Vehicles/{birinci}/arsivden-al", null);

            Assert.Equal(HttpStatusCode.PaymentRequired, geri.StatusCode);
        }

        [Fact]
        public async Task ArsivdenGeriAlmaYerVarkenCalisir()
        {
            var client = await SahipAsync("geriyer");
            var aracId = await AracEkleAsync(client);
            await ArsivleAsync(client, aracId);

            var geri = await client.PostAsync($"/api/Vehicles/{aracId}/arsivden-al", null);

            Assert.True(geri.IsSuccessStatusCode, await geri.Content.ReadAsStringAsync());

            var aktif = await client.GetAsync("/api/Vehicles");
            using var belge = JsonDocument.Parse(await aktif.Content.ReadAsStringAsync());

            Assert.Equal(1, belge.RootElement.GetProperty("data").GetArrayLength());
        }
    }
}
