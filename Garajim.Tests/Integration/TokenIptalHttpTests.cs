using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Garajim.Dal.Concrete.Context;

namespace Garajim.Tests.Integration
{
    public class TokenIptalHttpTests : IDisposable
    {
        private readonly GarajimWebApplicationFactory _factory = new GarajimWebApplicationFactory();

        public void Dispose()
        {
            _factory.Dispose();
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("iptal"), fullName = "İptal Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId, string Eposta)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("iptalsurucu");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "İptal Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login",
                new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return (client, veri.GetProperty("userId").GetInt32(), eposta);
        }

        private async Task VeritabaniAsync(Func<GarajimDbContext, Task> is_)
        {
            using var kapsam = _factory.Services.CreateScope();
            await is_(kapsam.ServiceProvider.GetRequiredService<GarajimDbContext>());
        }

        [Fact]
        public async Task KapatilanKullanicininTokeniAninaGecersizOlur()
        {
            var sahip = await SahipOlusturAsync();
            var surucu = await SurucuOlusturAsync(sahip);

            Assert.Equal(HttpStatusCode.OK, (await surucu.Client.GetAsync("/api/Vehicles")).StatusCode);

            var kapat = await sahip.PutAsJsonAsync($"/api/Team/{surucu.UserId}/deactivate", new { });
            Assert.Equal(HttpStatusCode.OK, kapat.StatusCode);

            var sonra = await surucu.Client.GetAsync("/api/Vehicles");

            Assert.Equal(HttpStatusCode.Unauthorized, sonra.StatusCode);
        }

        [Fact]
        public async Task RolDusurulunceEskiTokenYoneticiIslemiYapamaz()
        {
            var sahip = await SahipOlusturAsync();

            var eposta = Eposta("iptalmanager");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Yönetici", role = "Manager" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            var yoneticiId = veri.GetProperty("userId").GetInt32();

            var yonetici = _factory.CreateClient();
            var giris = await yonetici.PostAsJsonAsync("/api/Auth/login",
                new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("token").GetString();
            yonetici.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var oncePlaka = TestPlaka.Uret();
            Assert.Equal(HttpStatusCode.OK, (await yonetici.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = oncePlaka, brand = "Fiat", model = "Egea", year = 2020,
                currentKm = 10000, fuelType = "Dizel", vites = "Düz", kasaTipi = "Sedan"
            })).StatusCode);

            var dusur = await sahip.PutAsJsonAsync($"/api/Team/{yoneticiId}/role", new { role = "Driver" });
            Assert.Equal(HttpStatusCode.OK, dusur.StatusCode);

            var sonraPlaka = TestPlaka.Uret();
            var sonra = await yonetici.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = sonraPlaka, brand = "Fiat", model = "Egea", year = 2020,
                currentKm = 10000, fuelType = "Dizel", vites = "Düz", kasaTipi = "Sedan"
            });

            Assert.True(sonra.StatusCode == HttpStatusCode.Unauthorized || sonra.StatusCode == HttpStatusCode.Forbidden,
                "Rolü düşürülen kullanıcı eski token'la araç ekleyebildi: " + sonra.StatusCode);
        }

        [Fact]
        public async Task DogrulamaGeriAlinirsaTokenGecersizOlur()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("iptaldogrulama");
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "Doğrulama", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/Vehicles")).StatusCode);

            await VeritabaniAsync(async db =>
            {
                var kullanici = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Email == eposta);
                kullanici.EmailDogrulandi = false;
                await db.SaveChangesAsync();
            });

            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/Vehicles")).StatusCode);
        }

        [Fact]
        public async Task AktifKullaniciEtkilenmez()
        {
            var sahip = await SahipOlusturAsync();

            for (var i = 0; i < 3; i++)
            {
                Assert.Equal(HttpStatusCode.OK, (await sahip.GetAsync("/api/Vehicles")).StatusCode);
            }
        }
    }
}
