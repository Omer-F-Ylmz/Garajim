using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class EvrakHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public EvrakHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "Evrak Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("driver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Evrak Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka, string kullanim = "Hususi")
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 120000,
                fuelType = "Benzin",
                kullanimTuru = kullanim,
                ilkTescilTarihi = "2020-03-10"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        private static Task<HttpResponseMessage> EvrakEkleAsync(HttpClient client, object govde)
        {
            return client.PostAsJsonAsync("/api/Evrak", govde);
        }

        [Fact]
        public async Task AracEvrakiEklenirVeDurumHesaplanir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34EVR001");

            var cevap = await EvrakEkleAsync(sahip, new
            {
                vehicleId = aracId,
                evrakTuru = "TrafikSigortasi",
                bitisTarihi = DateTime.UtcNow.Date.AddDays(10).ToString("yyyy-MM-dd"),
                saglayici = "Örnek Sigorta",
                policeNo = "P-123"
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var veri = await VeriAsync(cevap);
            Assert.Equal("Yaklasiyor", veri.GetProperty("durum").GetString());
            Assert.True(veri.GetProperty("aktif").GetBoolean());
            Assert.Equal("Örnek Sigorta", veri.GetProperty("saglayici").GetString());
        }

        [Fact]
        public async Task BitisBosBirakilirsaKuraldanOnerilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34EVR002", "Ticari");

            var veri = await VeriAsync(await EvrakEkleAsync(sahip, new
            {
                vehicleId = aracId,
                evrakTuru = "Muayene",
                baslangicTarihi = "2026-05-20"
            }));

            Assert.Equal(new DateTime(2027, 5, 20), veri.GetProperty("bitisTarihi").GetDateTime());
        }

        [Fact]
        public async Task AracVeKullaniciBirlikteVerilirseReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34EVR003");
            var (_, surucuId) = await SurucuOlusturAsync(sahip);

            var ikisi = await EvrakEkleAsync(sahip, new
            {
                vehicleId = aracId,
                userId = surucuId,
                evrakTuru = "Kasko",
                bitisTarihi = "2027-01-01"
            });

            var hicbiri = await EvrakEkleAsync(sahip, new
            {
                evrakTuru = "Kasko",
                bitisTarihi = "2027-01-01"
            });

            Assert.Equal(HttpStatusCode.BadRequest, ikisi.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, hicbiri.StatusCode);
        }

        [Fact]
        public async Task GecersizEvrakTuruReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34EVR004");

            var cevap = await sahip.PostAsJsonAsync("/api/Evrak", new
            {
                vehicleId = aracId,
                evrakTuru = 999,
                bitisTarihi = "2027-01-01"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task YabanciSirketinEvraki404Doner()
        {
            var birinci = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci, "34EVR005");
            var evrakId = (await VeriAsync(await EvrakEkleAsync(birinci, new
            {
                vehicleId = aracId,
                evrakTuru = "Kasko",
                bitisTarihi = "2027-01-01"
            }))).GetProperty("id").GetInt32();

            var ikinci = await SahipOlusturAsync();

            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.GetAsync($"/api/Evrak/{evrakId}")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await ikinci.DeleteAsync($"/api/Evrak/{evrakId}")).StatusCode);
        }

        [Fact]
        public async Task DriverZimmetliAracEvrakiniOkurAmaYazamaz()
        {
            var sahip = await SahipOlusturAsync();
            var zimmetli = await AracEkleAsync(sahip, "34EVR006");
            var zimmetsiz = await AracEkleAsync(sahip, "34EVR007");
            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = zimmetli, userId = surucuId });

            await EvrakEkleAsync(sahip, new { vehicleId = zimmetli, evrakTuru = "Muayene", bitisTarihi = "2027-06-01" });

            Assert.Equal(HttpStatusCode.OK, (await surucu.GetAsync($"/api/Vehicles/{zimmetli}/evrak")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await surucu.GetAsync($"/api/Vehicles/{zimmetsiz}/evrak")).StatusCode);

            var yazma = await EvrakEkleAsync(surucu, new { vehicleId = zimmetli, evrakTuru = "Kasko", bitisTarihi = "2027-06-01" });
            Assert.Equal(HttpStatusCode.Forbidden, yazma.StatusCode);
        }

        [Fact]
        public async Task SurucuKendiEvrakiniOkurBaskasininkiniGormez()
        {
            var sahip = await SahipOlusturAsync();
            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);
            var (digerSurucu, digerId) = await SurucuOlusturAsync(sahip);

            await EvrakEkleAsync(sahip, new { userId = surucuId, evrakTuru = "Ehliyet", bitisTarihi = "2029-01-01" });
            await EvrakEkleAsync(sahip, new { userId = digerId, evrakTuru = "SRC", bitisTarihi = "2029-01-01" });

            var kendi = await VeriAsync(await surucu.GetAsync("/api/Evrak"));
            var turler = kendi.EnumerateArray().Select(e => e.GetProperty("evrakTuru").GetString()).ToList();

            Assert.Contains("Ehliyet", turler);
            Assert.DoesNotContain("SRC", turler);
        }

        [Fact]
        public async Task TakvimAyaGoreFiltreler()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34EVR008");

            await EvrakEkleAsync(sahip, new { vehicleId = aracId, evrakTuru = "Muayene", bitisTarihi = "2027-03-15" });
            await EvrakEkleAsync(sahip, new { vehicleId = aracId, evrakTuru = "Kasko", bitisTarihi = "2027-04-20" });

            var mart = await VeriAsync(await sahip.GetAsync("/api/Evrak/takvim?ay=2027-03"));
            var tek = Assert.Single(mart.EnumerateArray());

            Assert.Equal("Muayene", tek.GetProperty("evrakTuru").GetString());
        }

        [Fact]
        public async Task GecersizAyBicimiReddedilir()
        {
            var sahip = await SahipOlusturAsync();

            Assert.Equal(HttpStatusCode.BadRequest, (await sahip.GetAsync("/api/Evrak/takvim?ay=2027-13")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await sahip.GetAsync("/api/Evrak/takvim?ay=saçma")).StatusCode);
        }

        [Fact]
        public async Task YenilemeEskisiniPasifleyipZinciriKorur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34EVR009");

            var eskiId = (await VeriAsync(await EvrakEkleAsync(sahip, new
            {
                vehicleId = aracId,
                evrakTuru = "TrafikSigortasi",
                bitisTarihi = "2027-05-20"
            }))).GetProperty("id").GetInt32();

            var yeni = await VeriAsync(await sahip.PostAsync($"/api/Evrak/{eskiId}/yenile", null));

            Assert.NotEqual(eskiId, yeni.GetProperty("id").GetInt32());
            Assert.Equal(new DateTime(2028, 5, 20), yeni.GetProperty("bitisTarihi").GetDateTime());
            Assert.True(yeni.GetProperty("aktif").GetBoolean());

            var eski = await VeriAsync(await sahip.GetAsync($"/api/Evrak/{eskiId}"));
            Assert.False(eski.GetProperty("aktif").GetBoolean());

            var hepsi = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/evrak"));
            Assert.Equal(2, hepsi.GetArrayLength());
        }

        [Fact]
        public async Task DenormalizasyonDegismezi_EvrakinCompanyIdsiAracinkiyleAyni()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34EVR010");

            await EvrakEkleAsync(sahip, new { vehicleId = aracId, evrakTuru = "Kasko", bitisTarihi = "2027-01-01" });

            var arac = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}"));
            var evraklar = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/evrak"));
            var evrak = Assert.Single(evraklar.EnumerateArray());

            Assert.Equal(arac.GetProperty("id").GetInt32(), evrak.GetProperty("vehicleId").GetInt32());
        }

        [Fact]
        public async Task GuncellemeAlanlariDegistirir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34EVR011");

            var evrakId = (await VeriAsync(await EvrakEkleAsync(sahip, new
            {
                vehicleId = aracId,
                evrakTuru = "Kasko",
                bitisTarihi = "2027-01-01"
            }))).GetProperty("id").GetInt32();

            var guncelle = await sahip.PutAsJsonAsync($"/api/Evrak/{evrakId}", new
            {
                evrakTuru = "Kasko",
                bitisTarihi = "2027-02-01",
                saglayici = "Yeni Sigorta",
                policeNo = "P-999",
                not = "yenilendi"
            });

            Assert.Equal(HttpStatusCode.OK, guncelle.StatusCode);

            var veri = await VeriAsync(await sahip.GetAsync($"/api/Evrak/{evrakId}"));
            Assert.Equal("Yeni Sigorta", veri.GetProperty("saglayici").GetString());
            Assert.Equal(new DateTime(2027, 2, 1), veri.GetProperty("bitisTarihi").GetDateTime());
        }
    }
}
