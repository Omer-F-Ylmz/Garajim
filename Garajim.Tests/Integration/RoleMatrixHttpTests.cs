using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class RoleMatrixHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public RoleMatrixHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, string Eposta)> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var eposta = Eposta("owner");
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = eposta, fullName = "Filo Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, eposta);
        }

        private async Task<(HttpClient Client, int UserId, string Eposta)> EkipUyesiOlusturAsync(HttpClient sahip, string rol)
        {
            var eposta = Eposta(rol.ToLowerInvariant());
            var ekleCevap = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = $"{rol} Kullanıcı", role = rol });
            var govde = await ekleCevap.Content.ReadAsStringAsync();

            Assert.True(ekleCevap.IsSuccessStatusCode, $"Ekip üyesi eklenemedi: {(int)ekleCevap.StatusCode} {govde}");

            var veri = JsonDocument.Parse(govde).RootElement.GetProperty("data");
            var geciciSifre = veri.GetProperty("temporaryPassword").GetString();
            var userId = veri.GetProperty("userId").GetInt32();

            var client = _factory.CreateClient();
            var girisCevap = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = geciciSifre });
            var token = JsonDocument.Parse(await girisCevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, userId, eposta);
        }

        private static async Task<HttpResponseMessage> AracEkleAsync(HttpClient client, string plaka)
        {
            return await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2018,
                currentKm = 100000,
                fuelType = "Benzin"
            });
        }

        [Fact]
        public async Task Sahip_EkipUyesiEkleyebilirVeGeciciSifreBirKezDoner()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var eposta = Eposta("uye");

            var cevap = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Yeni Üye", role = "Driver" });
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            var veri = JsonDocument.Parse(govde).RootElement.GetProperty("data");
            Assert.False(string.IsNullOrWhiteSpace(veri.GetProperty("temporaryPassword").GetString()));

            var liste = await sahip.GetStringAsync("/api/Team");
            Assert.Contains(eposta, liste);
            Assert.DoesNotContain("temporaryPassword", liste);
        }

        [Fact]
        public async Task Manager_KullaniciYonetemez()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var (manager, _, _) = await EkipUyesiOlusturAsync(sahip, "Manager");

            var ekle = await manager.PostAsJsonAsync("/api/Team", new { email = Eposta("x"), fullName = "X", role = "Driver" });

            Assert.Equal(HttpStatusCode.Forbidden, ekle.StatusCode);
        }

        [Fact]
        public async Task Driver_KullaniciYonetemez()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var (driver, _, _) = await EkipUyesiOlusturAsync(sahip, "Driver");

            var ekle = await driver.PostAsJsonAsync("/api/Team", new { email = Eposta("x"), fullName = "X", role = "Driver" });
            var liste = await driver.GetAsync("/api/Team");

            Assert.Equal(HttpStatusCode.Forbidden, ekle.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, liste.StatusCode);
        }

        [Fact]
        public async Task Manager_AracEkleyebilir()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var (manager, _, _) = await EkipUyesiOlusturAsync(sahip, "Manager");

            var cevap = await AracEkleAsync(manager, "34MNG111");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        }

        [Fact]
        public async Task Driver_AracEkleyemez()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var (driver, _, _) = await EkipUyesiOlusturAsync(sahip, "Driver");

            var cevap = await AracEkleAsync(driver, "34DRV111");

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }

        [Fact]
        public async Task Driver_ZimmetsizAracaErisemez()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var aracCevap = await AracEkleAsync(sahip, "34OWN111");
            var aracId = JsonDocument.Parse(await aracCevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
            var (driver, _, _) = await EkipUyesiOlusturAsync(sahip, "Driver");

            var tekil = await driver.GetAsync($"/api/Vehicles/{aracId}");
            var liste = await driver.GetStringAsync("/api/Vehicles");

            Assert.Equal(HttpStatusCode.NotFound, tekil.StatusCode);
            Assert.DoesNotContain("34OWN111", liste);
        }

        [Fact]
        public async Task Sahip_RolDegistirebilir()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var (_, uyeId, _) = await EkipUyesiOlusturAsync(sahip, "Driver");

            var cevap = await sahip.PutAsJsonAsync($"/api/Team/{uyeId}/role", new { role = "Manager" });
            var liste = await sahip.GetStringAsync("/api/Team");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Contains("Manager", liste);
        }

        [Fact]
        public async Task PasiflestirilenKullaniciGirisYapamaz()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var eposta = Eposta("pasif");
            var ekleCevap = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Pasif Olacak", role = "Driver" });
            var veri = JsonDocument.Parse(await ekleCevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            var sifre = veri.GetProperty("temporaryPassword").GetString();
            var uyeId = veri.GetProperty("userId").GetInt32();

            var pasifCevap = await sahip.PutAsync($"/api/Team/{uyeId}/deactivate", null);

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = sifre });
            var girisGovde = await giris.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, pasifCevap.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, giris.StatusCode);
            Assert.Contains("pasif", girisGovde, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Sahip_BaskaSirketinKullanicisiniYonetemez()
        {
            var (sahipA, _) = await SahipOlusturAsync();
            var (sahipB, _) = await SahipOlusturAsync();
            var (_, bUyeId, _) = await EkipUyesiOlusturAsync(sahipB, "Driver");

            var rolCevap = await sahipA.PutAsJsonAsync($"/api/Team/{bUyeId}/role", new { role = "Manager" });
            var pasifCevap = await sahipA.PutAsync($"/api/Team/{bUyeId}/deactivate", null);

            Assert.Equal(HttpStatusCode.NotFound, rolCevap.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, pasifCevap.StatusCode);
        }

        [Fact]
        public async Task Sahip_KendiniPasiflestiremez()
        {
            var (sahip, _) = await SahipOlusturAsync();
            var liste = await sahip.GetStringAsync("/api/Team");
            var kendiId = JsonDocument.Parse(liste).RootElement.GetProperty("data")[0].GetProperty("id").GetInt32();

            var cevap = await sahip.PutAsync($"/api/Team/{kendiId}/deactivate", null);

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }
    }
}
