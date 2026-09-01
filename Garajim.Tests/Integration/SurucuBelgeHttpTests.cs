using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class SurucuBelgeHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public SurucuBelgeHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("belge"), fullName = "Belge Sahip", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> UyeOlusturAsync(HttpClient sahip, string rol)
        {
            var eposta = Eposta("belge" + rol.ToLowerInvariant());
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Belge " + rol, role = rol });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
        }

        private static async Task<JsonElement> BelgelerAsync(HttpClient client)
        {
            var cevap = await client.GetAsync("/api/Team/belgeler");
            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task BelgesizEkipUyesiIyiDurumdaListelenir()
        {
            var sahip = await SahipOlusturAsync();
            await UyeOlusturAsync(sahip, "Driver");

            var veri = await BelgelerAsync(sahip);

            Assert.Equal(2, veri.GetArrayLength());
            var surucu = veri.EnumerateArray().Single(u => u.GetProperty("rol").GetString() == "Driver");
            Assert.Equal(0, surucu.GetProperty("belgeler").GetArrayLength());
            Assert.Equal("Iyi", surucu.GetProperty("enKotuDurum").GetString());
        }

        [Fact]
        public async Task SurucuninEhliyetiVeDurumuDoner()
        {
            var sahip = await SahipOlusturAsync();
            var (_, surucuId) = await UyeOlusturAsync(sahip, "Driver");

            var bugun = DateTime.UtcNow.Date;
            await sahip.PostAsJsonAsync("/api/Evrak", new { userId = surucuId, evrakTuru = "Ehliyet", bitisTarihi = bugun.AddDays(400).ToString("yyyy-MM-dd") });
            await sahip.PostAsJsonAsync("/api/Evrak", new { userId = surucuId, evrakTuru = "SRC", bitisTarihi = bugun.AddDays(10).ToString("yyyy-MM-dd") });

            var veri = await BelgelerAsync(sahip);
            var surucu = veri.EnumerateArray().Single(u => u.GetProperty("userId").GetInt32() == surucuId);

            Assert.Equal(2, surucu.GetProperty("belgeler").GetArrayLength());
            Assert.Equal("Yaklasiyor", surucu.GetProperty("enKotuDurum").GetString());

            var src = surucu.GetProperty("belgeler").EnumerateArray().Single(b => b.GetProperty("evrakTuru").GetString() == "SRC");
            Assert.Equal("Yaklasiyor", src.GetProperty("durum").GetString());
            Assert.Equal(10, src.GetProperty("kalanGun").GetInt32());
        }

        [Fact]
        public async Task SuresiGecenBelgeEnKotuDurumuBelirler()
        {
            var sahip = await SahipOlusturAsync();
            var (_, surucuId) = await UyeOlusturAsync(sahip, "Driver");

            var bugun = DateTime.UtcNow.Date;
            await sahip.PostAsJsonAsync("/api/Evrak", new { userId = surucuId, evrakTuru = "Ehliyet", bitisTarihi = bugun.AddDays(400).ToString("yyyy-MM-dd") });
            await sahip.PostAsJsonAsync("/api/Evrak", new { userId = surucuId, evrakTuru = "Psikoteknik", bitisTarihi = bugun.AddDays(-3).ToString("yyyy-MM-dd") });

            var veri = await BelgelerAsync(sahip);
            var surucu = veri.EnumerateArray().Single(u => u.GetProperty("userId").GetInt32() == surucuId);

            Assert.Equal("Gecti", surucu.GetProperty("enKotuDurum").GetString());
        }

        [Fact]
        public async Task ListeEnKotuDurumdakiUyeyiUsteAlir()
        {
            var sahip = await SahipOlusturAsync();
            var (_, iyi) = await UyeOlusturAsync(sahip, "Driver");
            var (_, gecen) = await UyeOlusturAsync(sahip, "Manager");

            var bugun = DateTime.UtcNow.Date;
            await sahip.PostAsJsonAsync("/api/Evrak", new { userId = iyi, evrakTuru = "Ehliyet", bitisTarihi = bugun.AddDays(500).ToString("yyyy-MM-dd") });
            await sahip.PostAsJsonAsync("/api/Evrak", new { userId = gecen, evrakTuru = "Ehliyet", bitisTarihi = bugun.AddDays(-1).ToString("yyyy-MM-dd") });

            var veri = await BelgelerAsync(sahip);

            Assert.Equal(gecen, veri[0].GetProperty("userId").GetInt32());
        }

        [Fact]
        public async Task ManagerListeyiGorurDriverGoremez()
        {
            var sahip = await SahipOlusturAsync();
            var (yonetici, _) = await UyeOlusturAsync(sahip, "Manager");
            var (surucu, _) = await UyeOlusturAsync(sahip, "Driver");

            Assert.Equal(HttpStatusCode.OK, (await yonetici.GetAsync("/api/Team/belgeler")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await surucu.GetAsync("/api/Team/belgeler")).StatusCode);
        }

        [Fact]
        public async Task BaskaSirketinUyeleriListelenmez()
        {
            var birinci = await SahipOlusturAsync();
            await UyeOlusturAsync(birinci, "Driver");

            var ikinci = await SahipOlusturAsync();
            var veri = await BelgelerAsync(ikinci);

            Assert.Equal(1, veri.GetArrayLength());
        }

        [Fact]
        public async Task PasifUyeListelenmez()
        {
            var sahip = await SahipOlusturAsync();
            var (_, surucuId) = await UyeOlusturAsync(sahip, "Driver");

            await sahip.PutAsJsonAsync($"/api/Team/{surucuId}/deactivate", new { });

            var veri = await BelgelerAsync(sahip);

            Assert.DoesNotContain(veri.EnumerateArray(), u => u.GetProperty("userId").GetInt32() == surucuId);
        }

        [Fact]
        public async Task SurucuKendiBelgeleriniGorur()
        {
            var sahip = await SahipOlusturAsync();
            var (surucu, surucuId) = await UyeOlusturAsync(sahip, "Driver");
            await sahip.PostAsJsonAsync("/api/Evrak", new { userId = surucuId, evrakTuru = "Ehliyet", bitisTarihi = DateTime.UtcNow.Date.AddDays(100).ToString("yyyy-MM-dd") });

            var cevap = await surucu.GetAsync("/api/Evrak");
            var veri = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Contains(veri.EnumerateArray(), e => e.GetProperty("evrakTuru").GetString() == "Ehliyet");
        }
    }
}
