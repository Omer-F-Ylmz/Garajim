using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class DavetHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public DavetHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync(string sirket = null, string davetKodu = null)
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new
            {
                email = Eposta("davet"),
                fullName = "Davet Sahip",
                companyName = sirket,
                password = "Test1234!",
                davetKodu
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("davetdriver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Davet Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
        }

        private static async Task<JsonElement> DurumAsync(HttpClient client)
        {
            var cevap = await client.GetAsync("/api/Davet");
            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task DavetKoduIlkIstektePUretilirVeSabitKalir()
        {
            var sahip = await SahipOlusturAsync();

            var birinci = await DurumAsync(sahip);
            var ikinci = await DurumAsync(sahip);

            var kod = birinci.GetProperty("kod").GetString();
            Assert.Equal(8, kod.Length);
            Assert.Equal(kod, ikinci.GetProperty("kod").GetString());
            Assert.Contains(kod, birinci.GetProperty("paylasimBaglantisi").GetString());
            Assert.Equal(0, birinci.GetProperty("davetSayisi").GetInt32());
            Assert.Equal(0, birinci.GetProperty("kazanilanAracHakki").GetInt32());
            Assert.Equal(3, birinci.GetProperty("ekAracUstSiniri").GetInt32());
            Assert.Equal(3, birinci.GetProperty("aracLimiti").GetInt32());
        }

        [Fact]
        public async Task DavetEdenAracHakkiKazanirDavetliKazanmaz()
        {
            var davetEden = await SahipOlusturAsync("Davet Eden A.Ş.");
            var kod = (await DurumAsync(davetEden)).GetProperty("kod").GetString();

            var davetli = await SahipOlusturAsync("Davetli Ltd.", kod);

            var edenDurum = await DurumAsync(davetEden);
            Assert.Equal(1, edenDurum.GetProperty("davetSayisi").GetInt32());
            Assert.Equal(1, edenDurum.GetProperty("kazanilanAracHakki").GetInt32());
            Assert.Equal(4, edenDurum.GetProperty("aracLimiti").GetInt32());
            Assert.Equal("Davetli Ltd.", edenDurum.GetProperty("davetliler")[0].GetProperty("sirketAdi").GetString());

            var davetliDurum = await DurumAsync(davetli);
            Assert.Equal(0, davetliDurum.GetProperty("kazanilanAracHakki").GetInt32());
            Assert.Equal(3, davetliDurum.GetProperty("aracLimiti").GetInt32());
            Assert.Equal("Davet Eden A.Ş.", davetliDurum.GetProperty("davetEden").GetString());
        }

        [Fact]
        public async Task IkiDavetIkiAracHakkiVerir()
        {
            var davetEden = await SahipOlusturAsync();
            var kod = (await DurumAsync(davetEden)).GetProperty("kod").GetString();

            await SahipOlusturAsync("Birinci", kod);
            await SahipOlusturAsync("İkinci", kod);

            var durum = await DurumAsync(davetEden);

            Assert.Equal(2, durum.GetProperty("davetSayisi").GetInt32());
            Assert.Equal(2, durum.GetProperty("kazanilanAracHakki").GetInt32());
            Assert.Equal(5, durum.GetProperty("aracLimiti").GetInt32());
        }

        [Fact]
        public async Task KodKucukHarfVeBosluklaDaCalisir()
        {
            var davetEden = await SahipOlusturAsync();
            var kod = (await DurumAsync(davetEden)).GetProperty("kod").GetString();

            await SahipOlusturAsync("Karisik", " " + kod.ToLowerInvariant() + " ");

            Assert.Equal(1, (await DurumAsync(davetEden)).GetProperty("davetSayisi").GetInt32());
        }

        [Fact]
        public async Task GecersizKodKayitReddeder()
        {
            var client = _factory.CreateClient();

            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new
            {
                email = Eposta("gecersiz"),
                fullName = "Geçersiz Davet",
                password = "Test1234!",
                davetKodu = "ZZZZZZZZ"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task KodsuzKayitEskisiGibiCalisir()
        {
            var sahip = await SahipOlusturAsync();
            var durum = await DurumAsync(sahip);

            Assert.Equal(JsonValueKind.Null, durum.GetProperty("davetEden").ValueKind);
            Assert.Equal(0, durum.GetProperty("kazanilanAracHakki").GetInt32());
        }

        [Fact]
        public async Task DriverDavetPaneliniGoremez()
        {
            var sahip = await SahipOlusturAsync();
            var (surucu, _) = await SurucuOlusturAsync(sahip);

            var cevap = await surucu.GetAsync("/api/Davet");

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }

        [Fact]
        public async Task SirketlerBirbirininDavetlisiniGormez()
        {
            var birinci = await SahipOlusturAsync();
            var kod = (await DurumAsync(birinci)).GetProperty("kod").GetString();
            await SahipOlusturAsync("Birincinin Daveti", kod);

            var ikinci = await SahipOlusturAsync();
            var durum = await DurumAsync(ikinci);

            Assert.Equal(0, durum.GetProperty("davetSayisi").GetInt32());
            Assert.Equal(0, durum.GetProperty("davetliler").GetArrayLength());
        }
    }
}
