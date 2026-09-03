using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Dal.Concrete.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class KmDuzeltmeHttpTests : IDisposable
    {
        private readonly GarajimWebApplicationFactory _factory = new GarajimWebApplicationFactory();

        public void Dispose() => _factory.Dispose();

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int AracId)> AracliSahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Km Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Fiat", model = "Egea", year = 2020, currentKm = 100000,
                fuelType = "Dizel", vites = "Manuel", kasaTipi = "Sedan"
            });
            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            return (client, aracId);
        }

        private static object Guncelle(int km, bool? onay = null, string neden = null) => new
        {
            brand = "Fiat", model = "Egea", year = 2020, currentKm = km, fuelType = "Dizel",
            kmDusurmeOnayi = onay, kmDuzeltmeNedeni = neden
        };

        private int LogSayisi(int aracId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();
            return context.KmDuzeltmeLoglari.IgnoreQueryFilters().Count(l => l.VehicleId == aracId);
        }

        [Fact]
        public async Task OnaysizKmDusurmeReddedilir()
        {
            var (client, aracId) = await AracliSahipAsync("onaysiz");

            var cevap = await client.PutAsJsonAsync($"/api/Vehicles/{aracId}", Guncelle(90000));

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
            Assert.Equal(0, LogSayisi(aracId));
        }

        [Fact]
        public async Task NedensizOnayReddedilir()
        {
            var (client, aracId) = await AracliSahipAsync("nedensiz");

            var cevap = await client.PutAsJsonAsync($"/api/Vehicles/{aracId}", Guncelle(90000, onay: true, neden: "  "));

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task OnayliDusurmeKaydedilirVeLoglanir()
        {
            var (client, aracId) = await AracliSahipAsync("onayli");

            var cevap = await client.PutAsJsonAsync($"/api/Vehicles/{aracId}",
                Guncelle(90000, onay: true, neden: "Gösterge paneli değişti"));

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());

            var arac = await client.GetAsync($"/api/Vehicles/{aracId}");
            using var belge = JsonDocument.Parse(await arac.Content.ReadAsStringAsync());

            Assert.Equal(90000, belge.RootElement.GetProperty("data").GetProperty("currentKm").GetInt32());
            Assert.Equal(1, LogSayisi(aracId));
        }

        [Fact]
        public async Task KmYukseltmeOnaySizCalisir()
        {
            var (client, aracId) = await AracliSahipAsync("yukselt");

            var cevap = await client.PutAsJsonAsync($"/api/Vehicles/{aracId}", Guncelle(120000));

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
            Assert.Equal(0, LogSayisi(aracId));
        }

        [Fact]
        public async Task DuzeltmeKayitlarinKilometresineDokunmaz()
        {
            var (client, aracId) = await AracliSahipAsync("kayit");

            await client.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId, date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                km = 100500, liters = 40m, totalCost = 2000m
            });

            await client.PutAsJsonAsync($"/api/Vehicles/{aracId}",
                Guncelle(90000, onay: true, neden: "Sayaç sıfırlandı"));

            var yakit = await client.GetAsync($"/api/Fuel?vehicleId={aracId}");
            using var belge = JsonDocument.Parse(await yakit.Content.ReadAsStringAsync());

            Assert.Equal(100500, belge.RootElement.GetProperty("data")[0].GetProperty("km").GetInt32());
        }

        [Fact]
        public async Task KarneKmSatiriDuzeltmeyiIsaretler()
        {
            var (client, aracId) = await AracliSahipAsync("karne");

            await client.PutAsJsonAsync($"/api/Vehicles/{aracId}",
                Guncelle(90000, onay: true, neden: "Gösterge değişti"));

            var karne = await client.PostAsJsonAsync($"/api/Vehicles/{aracId}/karne", new
            {
                kapsam = new
                {
                    bakimGecmisi = true, parcaHafizasi = false, yakitOzeti = false, belgeler = false,
                    plakaGoster = true, tutarGoster = false, acilKart = false, hasarGecmisi = false, beyanDegeri = false
                },
                sonKullanmaGun = 30
            });
            var url = JsonDocument.Parse(await karne.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("url").GetString();
            var token = url.Substring(url.IndexOf("t=", StringComparison.Ordinal) + 2);

            var anonim = _factory.CreateClient();
            var goruntu = await anonim.GetAsync($"/api/karne/{token}");
            using var belge = JsonDocument.Parse(await goruntu.Content.ReadAsStringAsync());

            Assert.True(belge.RootElement.GetProperty("data").GetProperty("kmDuzeltildi").GetBoolean());
        }
    }
}
