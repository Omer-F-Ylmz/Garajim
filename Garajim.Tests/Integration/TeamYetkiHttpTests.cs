using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class TeamYetkiHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public TeamYetkiHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("ekip"), fullName = "Ekip Sahip", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> UyeOlusturAsync(HttpClient sahip, string rol)
        {
            var eposta = Eposta("ekip" + rol.ToLowerInvariant());
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Ekip " + rol, role = rol });
            Assert.Equal(HttpStatusCode.OK, ekle.StatusCode);
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
        }

        [Fact]
        public async Task ManagerEkipListesiniOkuyabilir()
        {
            var sahip = await SahipOlusturAsync();
            var (yonetici, _) = await UyeOlusturAsync(sahip, "Manager");

            var cevap = await yonetici.GetAsync("/api/Team");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            var veri = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
            Assert.Equal(2, veri.GetArrayLength());
        }

        [Fact]
        public async Task ManagerEkipBelgeleriniOkuyabilir()
        {
            var sahip = await SahipOlusturAsync();
            var (yonetici, _) = await UyeOlusturAsync(sahip, "Manager");

            Assert.Equal(HttpStatusCode.OK, (await yonetici.GetAsync("/api/Team/belgeler")).StatusCode);
        }

        [Fact]
        public async Task ManagerYeniUyeDavetEdemez()
        {
            var sahip = await SahipOlusturAsync();
            var (yonetici, _) = await UyeOlusturAsync(sahip, "Manager");

            var cevap = await yonetici.PostAsJsonAsync("/api/Team", new { email = Eposta("yeni"), fullName = "Yeni Üye", role = "Driver" });

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }

        [Fact]
        public async Task ManagerRolDegistiremez()
        {
            var sahip = await SahipOlusturAsync();
            var (yonetici, _) = await UyeOlusturAsync(sahip, "Manager");
            var (_, surucuId) = await UyeOlusturAsync(sahip, "Driver");

            var cevap = await yonetici.PutAsJsonAsync($"/api/Team/{surucuId}/role", new { role = "Manager" });

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }

        [Fact]
        public async Task ManagerUyeyiPasiflestiremez()
        {
            var sahip = await SahipOlusturAsync();
            var (yonetici, _) = await UyeOlusturAsync(sahip, "Manager");
            var (_, surucuId) = await UyeOlusturAsync(sahip, "Driver");

            var cevap = await yonetici.PutAsJsonAsync($"/api/Team/{surucuId}/deactivate", new { });

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }

        [Fact]
        public async Task DriverEkipUclarinaErisemez()
        {
            var sahip = await SahipOlusturAsync();
            var (surucu, _) = await UyeOlusturAsync(sahip, "Driver");

            Assert.Equal(HttpStatusCode.Forbidden, (await surucu.GetAsync("/api/Team")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await surucu.GetAsync("/api/Team/belgeler")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await surucu.PostAsJsonAsync("/api/Team", new { email = Eposta("x"), fullName = "X", role = "Driver" })).StatusCode);
        }

        [Fact]
        public async Task SahipYazmaUclarinaErisebilir()
        {
            var sahip = await SahipOlusturAsync();
            var (_, surucuId) = await UyeOlusturAsync(sahip, "Driver");

            Assert.Equal(HttpStatusCode.OK, (await sahip.GetAsync("/api/Team")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await sahip.PutAsJsonAsync($"/api/Team/{surucuId}/role", new { role = "Manager" })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await sahip.PutAsJsonAsync($"/api/Team/{surucuId}/deactivate", new { })).StatusCode);
        }

        [Fact]
        public async Task ManagerBaskaSirketinEkibiniGormez()
        {
            var birinci = await SahipOlusturAsync();
            await UyeOlusturAsync(birinci, "Driver");

            var ikinci = await SahipOlusturAsync();
            var (yonetici, _) = await UyeOlusturAsync(ikinci, "Manager");

            var veri = JsonDocument.Parse(await (await yonetici.GetAsync("/api/Team")).Content.ReadAsStringAsync())
                .RootElement.GetProperty("data");

            Assert.Equal(2, veri.GetArrayLength());
        }
        [Fact]
        public async Task PasiflestirilenUyeninZimmetiKapanir()
        {
            var sahip = await SahipOlusturAsync();
            var arac = await sahip.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(), brand = "Fiat", model = "Egea",
                year = 2020, currentKm = 10000, fuelType = "Benzin"
            });
            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var uye = await sahip.PostAsJsonAsync("/api/Team", new
            {
                email = $"zimmet-{Guid.NewGuid():N}@garajim.local",
                fullName = "Zimmetli Sürücü",
                role = "Driver"
            });
            var userId = JsonDocument.Parse(await uye.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("userId").GetInt32();

            var zimmet = await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId });
            Assert.True(zimmet.IsSuccessStatusCode, await zimmet.Content.ReadAsStringAsync());

            var pasif = await sahip.PutAsync($"/api/Team/{userId}/deactivate", null);
            Assert.True(pasif.IsSuccessStatusCode, await pasif.Content.ReadAsStringAsync());

            var zimmetler = JsonDocument.Parse(
                await (await sahip.GetAsync($"/api/Assignments?vehicleId={aracId}")).Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").EnumerateArray().ToList();

            Assert.All(zimmetler, z => Assert.False(z.GetProperty("isActive").GetBoolean()));
        }

    }
}
