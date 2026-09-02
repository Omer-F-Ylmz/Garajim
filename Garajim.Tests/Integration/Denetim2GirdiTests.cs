using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class Denetim2GirdiTests : IClassFixture<UstaWebApplicationFactory>
    {
        private const string Surum = "2026-09-v1";

        private readonly UstaWebApplicationFactory _factory;

        public Denetim2GirdiTests(UstaWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.Istemci.Uretici = null;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static string Plaka() => "34GD" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("gd"), fullName = "Girdi Sahip", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await client.PostAsJsonAsync("/api/Usta/onay", new { metinSurumu = Surum });
            return client;
        }

        private static async Task<int> AracEkleAsync(HttpClient client)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = Plaka(),
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 120000,
                fuelType = "Benzin"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        // T-3 · tanımsız enum değerleri
        [Theory]
        [InlineData("/api/Evrak", "{\"vehicleId\":VID,\"evrakTuru\":99,\"bitisTarihi\":\"2027-01-01\"}")]
        [InlineData("/api/Yolculuk", "{\"vehicleId\":VID,\"tarih\":\"2026-03-01\",\"baslangicKm\":1,\"bitisKm\":2,\"amac\":77}")]
        [InlineData("/api/Lastik", "{\"vehicleId\":VID,\"ad\":\"Set\",\"mevsim\":42,\"takilmaTarihi\":\"2026-04-01\",\"takilmaKm\":1}")]
        [InlineData("/api/Fuel", "{\"vehicleId\":VID,\"date\":\"2026-03-01\",\"liters\":10,\"totalCost\":100,\"km\":120100,\"sarjTuru\":8}")]
        public async Task TanimsizEnumDegeriReddedilir(string yol, string sablon)
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var govde = new StringContent(sablon.Replace("VID", aracId.ToString()), Encoding.UTF8, "application/json");
            var cevap = await sahip.PostAsync(yol, govde);

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task TanimsizPlanVeGeriBildirimEnumuReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var sohbetId = (await VeriAsync(await sahip.PostAsJsonAsync("/api/Usta/sohbet", new { vehicleId = aracId }))).GetProperty("id").GetInt32();
            var mesajId = (await VeriAsync(await sahip.PostAsJsonAsync($"/api/Usta/sohbet/{sohbetId}/mesaj", new { metin = "frende ses var" })))
                .GetProperty("mesaj").GetProperty("id").GetInt32();

            var plan = await sahip.PostAsync("/api/Plan/yukseltme-talebi",
                new StringContent("{\"istenenPlan\":9}", Encoding.UTF8, "application/json"));
            var geri = await sahip.PostAsync($"/api/Usta/mesaj/{mesajId}/geri-bildirim",
                new StringContent("{\"geriBildirim\":7}", Encoding.UTF8, "application/json"));

            Assert.Equal(HttpStatusCode.BadRequest, plan.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, geri.StatusCode);
        }

        // T-3 · tarih uçları
        [Theory]
        [InlineData("0001-01-01")]
        [InlineData("9999-12-31")]
        public async Task TarihUclariYolculuktaReddedilir(string tarih)
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var cevap = await sahip.PostAsJsonAsync("/api/Yolculuk", new { vehicleId = aracId, tarih, baslangicKm = 1, bitisKm = 2, amac = "Is" });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task GelecekTarihliLastikTakmaReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var cevap = await sahip.PostAsJsonAsync("/api/Lastik", new
            {
                vehicleId = aracId,
                ad = "Yaz",
                mevsim = "Yaz",
                takilmaTarihi = DateTime.UtcNow.Date.AddDays(30).ToString("yyyy-MM-dd"),
                takilmaKm = 120000
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        // T-3 · negatif sayısal değerler
        [Fact]
        public async Task NegatifDegerlerReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var negatifKm = await sahip.PostAsJsonAsync("/api/Yolculuk", new { vehicleId = aracId, tarih = "2026-03-01", baslangicKm = -5, bitisKm = 100, amac = "Is" });
            var negatifKwh = await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-01", liters = 10m, totalCost = 100m, km = 120100, kwh = -5m });
            var negatifDis = await sahip.PostAsJsonAsync("/api/Lastik", new { vehicleId = aracId, ad = "Yaz", mevsim = "Yaz", takilmaTarihi = "2026-04-01", takilmaKm = 1, disDerinligiMm = -3m });
            var buyukDis = await sahip.PostAsJsonAsync("/api/Lastik", new { vehicleId = aracId, ad = "Yaz", mevsim = "Yaz", takilmaTarihi = "2026-04-01", takilmaKm = 1, disDerinligiMm = 99m });
            var negatifTutar = await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-01", liters = 10m, totalCost = -100m, km = 120100 });

            Assert.Equal(HttpStatusCode.BadRequest, negatifKm.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, negatifKwh.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, negatifDis.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, buyukDis.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, negatifTutar.StatusCode);
        }

        // T-3 · string uzunlukları
        [Fact]
        public async Task AsiriUzunStringlerPatlamadanKirpilirVeyaReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var uzunAd = new string('x', 5000);
            var lastik = await sahip.PostAsJsonAsync("/api/Lastik", new
            {
                vehicleId = aracId,
                ad = uzunAd,
                mevsim = "Yaz",
                marka = uzunAd,
                ebat = uzunAd,
                takilmaTarihi = "2026-04-01",
                takilmaKm = 120000
            });

            Assert.Equal(HttpStatusCode.OK, lastik.StatusCode);

            var durum = await VeriAsync(await sahip.GetAsync($"/api/Lastik?vehicleId={aracId}"));
            var set = durum.GetProperty("setler")[0];

            Assert.Equal(100, set.GetProperty("ad").GetString().Length);
            Assert.Equal(100, set.GetProperty("marka").GetString().Length);
            Assert.Equal(50, set.GetProperty("ebat").GetString().Length);
        }

        [Fact]
        public async Task EvrakMetinAlanlariKolonSinirinaKirpilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var uzun = new string('y', 4000);
            var cevap = await sahip.PostAsJsonAsync("/api/Evrak", new
            {
                vehicleId = aracId,
                evrakTuru = "Kasko",
                bitisTarihi = "2027-01-01",
                saglayici = uzun,
                policeNo = uzun,
                not = uzun
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var kayit = (await VeriAsync(await sahip.GetAsync($"/api/Evrak?vehicleId={aracId}")))[0];

            Assert.Equal(100, kayit.GetProperty("saglayici").GetString().Length);
            Assert.Equal(50, kayit.GetProperty("policeNo").GetString().Length);
            Assert.Equal(300, kayit.GetProperty("not").GetString().Length);
        }

        [Fact]
        public async Task EvrakGuncellemesindeDeKirpilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Evrak", new { vehicleId = aracId, evrakTuru = "Kasko", bitisTarihi = "2027-01-01" });
            var evrakId = (await VeriAsync(await sahip.GetAsync($"/api/Evrak?vehicleId={aracId}")))[0].GetProperty("id").GetInt32();

            var cevap = await sahip.PutAsJsonAsync($"/api/Evrak/{evrakId}", new
            {
                evrakTuru = "Kasko",
                bitisTarihi = "2027-06-01",
                saglayici = new string('z', 4000),
                policeNo = new string('z', 4000),
                not = new string('z', 4000)
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var kayit = (await VeriAsync(await sahip.GetAsync($"/api/Evrak?vehicleId={aracId}")))[0];
            Assert.Equal(100, kayit.GetProperty("saglayici").GetString().Length);
            Assert.Equal(50, kayit.GetProperty("policeNo").GetString().Length);
            Assert.Equal(300, kayit.GetProperty("not").GetString().Length);
        }


        [Fact]
        public async Task YolculukNotuVeGuzergahiKirpilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);

            var cevap = await sahip.PostAsJsonAsync("/api/Yolculuk", new
            {
                vehicleId = aracId,
                tarih = "2026-03-01",
                baslangicKm = 1,
                bitisKm = 2,
                amac = "Is",
                nereden = new string('a', 500),
                nereye = new string('b', 500),
                not = new string('c', 5000)
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var kayit = (await VeriAsync(await sahip.GetAsync($"/api/Yolculuk?vehicleId={aracId}")))[0];
            Assert.True(kayit.GetProperty("nereden").GetString().Length <= 150);
            Assert.True(kayit.GetProperty("nereye").GetString().Length <= 150);
            Assert.True(kayit.GetProperty("not").GetString().Length <= 500);
        }

        // T-3 · sınırlar
        [Fact]
        public async Task UstaMesajiBinKarakterSinirinda()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip);
            var sohbetId = (await VeriAsync(await sahip.PostAsJsonAsync("/api/Usta/sohbet", new { vehicleId = aracId }))).GetProperty("id").GetInt32();

            var tamSinir = await sahip.PostAsJsonAsync($"/api/Usta/sohbet/{sohbetId}/mesaj", new { metin = new string('a', 1000) });
            var asan = await sahip.PostAsJsonAsync($"/api/Usta/sohbet/{sohbetId}/mesaj", new { metin = new string('a', 1001) });
            var bos = await sahip.PostAsJsonAsync($"/api/Usta/sohbet/{sohbetId}/mesaj", new { metin = "   " });

            Assert.Equal(HttpStatusCode.OK, tamSinir.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, asan.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, bos.StatusCode);
        }

        [Fact]
        public async Task ImportBesMbUzeriDosyaReddedilir()
        {
            var sahip = await SahipOlusturAsync();

            var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(new byte[5 * 1024 * 1024 + 16]);
            dosya.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(dosya, "file", "buyuk.csv");
            form.Add(new StringContent("Yakit"), "kayitTuru");

            var cevap = await sahip.PostAsync("/api/Import/onizle", form);

            Assert.True(cevap.StatusCode == HttpStatusCode.BadRequest || cevap.StatusCode == HttpStatusCode.RequestEntityTooLarge,
                $"Beklenmeyen durum: {cevap.StatusCode}");
        }

        [Fact]
        public async Task ExportTersTarihAraligiReddedilir()
        {
            var sahip = await SahipOlusturAsync();

            var cevap = await sahip.GetAsync("/api/Export/yakit.csv?baslangic=2026-12-31&bitis=2026-01-01");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }
    }
}
