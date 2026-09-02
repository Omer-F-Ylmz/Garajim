using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class MaliyetHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public MaliyetHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "Maliyet Sahip", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> UyeOlusturAsync(HttpClient sahip, string rol)
        {
            var eposta = Eposta(rol.ToLowerInvariant());
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Maliyet " + rol, role = rol });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka, int km = 100000)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = km,
                fuelType = "Benzin"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task YakitEkleAsync(HttpClient client, int aracId, string tarih, int km, decimal litre, decimal tutar)
        {
            await client.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = tarih, liters = litre, totalCost = tutar, km });
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        private static async Task VeriDoldurAsync(HttpClient client, int aracId)
        {
            await YakitEkleAsync(client, aracId, "2026-01-10", 100000, 40m, 1900m);
            await YakitEkleAsync(client, aracId, "2026-02-10", 100500, 45m, 2100m);
            await YakitEkleAsync(client, aracId, "2026-03-10", 101000, 35m, 1600m);

            await client.PostAsJsonAsync("/api/Maintenance", new { vehicleId = aracId, type = "PeriyodikBakim", date = "2026-02-15", km = 100600, cost = 3000m, serviceName = "Servis" });
            await client.PostAsJsonAsync("/api/Expenses", new { vehicleId = aracId, category = "Otopark", date = "2026-03-01", amount = 500m, note = "otopark" });
        }

        [Fact]
        public async Task AracMaliyetiKirilimVeKmBasiMaliyetDoner()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34MLY001");
            await VeriDoldurAsync(sahip, aracId);

            var veri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-01-01&bitis=2026-03-31"));

            Assert.Equal(5600m, veri.GetProperty("toplamYakit").GetDecimal());
            Assert.Equal(3000m, veri.GetProperty("toplamBakim").GetDecimal());
            Assert.Equal(500m, veri.GetProperty("toplamMasraf").GetDecimal());
            Assert.Equal(9100m, veri.GetProperty("toplamMaliyet").GetDecimal());
            Assert.Equal(1000, veri.GetProperty("mesafeKm").GetInt32());
            Assert.Equal(9.1m, veri.GetProperty("maliyetKmBasi").GetDecimal());
        }

        [Fact]
        public async Task AracMaliyetiOnIkiAylikSeriDoner()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34MLY002");
            await VeriDoldurAsync(sahip, aracId);

            var veri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-01-01&bitis=2026-03-31"));
            var seri = veri.GetProperty("aylikSeri");

            Assert.Equal(12, seri.GetArrayLength());

            var sonuncu = seri[11];
            Assert.Equal(2026, sonuncu.GetProperty("yil").GetInt32());
            Assert.Equal(3, sonuncu.GetProperty("ay").GetInt32());
            Assert.Equal(2100m, sonuncu.GetProperty("toplam").GetDecimal());

            var subat = seri[10];
            Assert.Equal(2, subat.GetProperty("ay").GetInt32());
            Assert.Equal(2100m, subat.GetProperty("yakit").GetDecimal());
            Assert.Equal(3000m, subat.GetProperty("bakim").GetDecimal());
        }

        [Fact]
        public async Task TuketimSerisiIlkDolumuHaricTutar()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34MLY003");
            await VeriDoldurAsync(sahip, aracId);

            var veri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-01-01&bitis=2026-03-31"));
            var tuketim = veri.GetProperty("tuketimSeri");

            var subat = tuketim.EnumerateArray().Single(t => t.GetProperty("ay").GetInt32() == 2);
            Assert.Equal(9m, subat.GetProperty("litre100Km").GetDecimal());

            var ocak = tuketim.EnumerateArray().FirstOrDefault(t => t.GetProperty("ay").GetInt32() == 1);
            Assert.Equal(JsonValueKind.Undefined, ocak.ValueKind);

            Assert.Equal(8m, veri.GetProperty("litre100Km").GetDecimal());
        }

        [Fact]
        public async Task TekYakitKaydiVarkenKmBasiMaliyetBos()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34MLY004");
            await YakitEkleAsync(sahip, aracId, "2026-01-10", 100000, 40m, 1900m);

            var veri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-01-01&bitis=2026-03-31"));

            Assert.Equal(0, veri.GetProperty("mesafeKm").GetInt32());
            Assert.Equal(JsonValueKind.Null, veri.GetProperty("maliyetKmBasi").ValueKind);
            Assert.Equal(JsonValueKind.Null, veri.GetProperty("litre100Km").ValueKind);
        }

        [Fact]
        public async Task TarihAraligiDisindakiKayitlarSayilmaz()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34MLY005");
            await VeriDoldurAsync(sahip, aracId);

            var veri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-02-01&bitis=2026-02-28"));

            Assert.Equal(2100m, veri.GetProperty("toplamYakit").GetDecimal());
            Assert.Equal(3000m, veri.GetProperty("toplamBakim").GetDecimal());
            Assert.Equal(0m, veri.GetProperty("toplamMasraf").GetDecimal());
        }

        [Fact]
        public async Task BitisGununeAitKayitDahilEdilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34MLY006");
            await YakitEkleAsync(sahip, aracId, "2026-01-10", 100000, 40m, 1900m);
            await YakitEkleAsync(sahip, aracId, "2026-01-31", 100400, 30m, 1400m);

            var veri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-01-01&bitis=2026-01-31"));

            Assert.Equal(3300m, veri.GetProperty("toplamYakit").GetDecimal());
        }

        [Fact]
        public async Task TersTarihAraligi400Doner()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34MLY007");

            var cevap = await sahip.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-03-31&bitis=2026-01-01");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task YabanciSirketinAracinaMaliyet404Doner()
        {
            var birinci = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci, "34MLY008");

            var ikinci = await SahipOlusturAsync();
            var cevap = await ikinci.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-01-01&bitis=2026-03-31");

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task DriverZimmetliAracinMaliyetiniGorur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34MLY009");
            await VeriDoldurAsync(sahip, aracId);

            var (surucu, surucuId) = await UyeOlusturAsync(sahip, "Driver");
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = surucuId });

            var cevap = await surucu.GetAsync($"/api/Vehicles/{aracId}/maliyet?baslangic=2026-01-01&bitis=2026-03-31");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        }

        [Fact]
        public async Task FiloMaliyetiKmBasiMaliyeteGoreSiralanir()
        {
            var sahip = await SahipOlusturAsync();

            var pahali = await AracEkleAsync(sahip, "34FLO001");
            await YakitEkleAsync(sahip, pahali, "2026-01-10", 100000, 40m, 2000m);
            await YakitEkleAsync(sahip, pahali, "2026-02-10", 100200, 20m, 1000m);

            var ucuz = await AracEkleAsync(sahip, "34FLO002");
            await YakitEkleAsync(sahip, ucuz, "2026-01-10", 50000, 40m, 2000m);
            await YakitEkleAsync(sahip, ucuz, "2026-02-10", 52000, 60m, 3000m);

            var veri = await VeriAsync(await sahip.GetAsync("/api/Reports/filo-maliyet?baslangic=2026-01-01&bitis=2026-03-31"));
            var araclar = veri.GetProperty("araclar");

            Assert.Equal(2, araclar.GetArrayLength());
            Assert.Equal("34FLO001", araclar[0].GetProperty("plaka").GetString());
            Assert.Equal(15m, araclar[0].GetProperty("maliyetKmBasi").GetDecimal());
            Assert.Equal("34FLO002", araclar[1].GetProperty("plaka").GetString());
            Assert.Equal(2.5m, araclar[1].GetProperty("maliyetKmBasi").GetDecimal());
            Assert.Equal(8000m, veri.GetProperty("toplamMaliyet").GetDecimal());
        }

        [Fact]
        public async Task FiloMaliyetindeTekYakitKaydiOlanAracKmBasiHesaplamaz()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34FLO003");
            await YakitEkleAsync(sahip, aracId, "2026-01-10", 100000, 40m, 2000m);

            var veri = await VeriAsync(await sahip.GetAsync("/api/Reports/filo-maliyet?baslangic=2026-01-01&bitis=2026-03-31"));
            var satir = veri.GetProperty("araclar")[0];

            Assert.Equal(1, satir.GetProperty("yakitKaydiSayisi").GetInt32());
            Assert.Equal(JsonValueKind.Null, satir.GetProperty("maliyetKmBasi").ValueKind);
            Assert.Equal(2000m, satir.GetProperty("toplamMaliyet").GetDecimal());
        }

        [Fact]
        public async Task FiloMaliyetiSirketleriKaristirmaz()
        {
            var birinci = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci, "34FLO004");
            await YakitEkleAsync(birinci, aracId, "2026-01-10", 100000, 40m, 2000m);

            var ikinci = await SahipOlusturAsync();
            var veri = await VeriAsync(await ikinci.GetAsync("/api/Reports/filo-maliyet?baslangic=2026-01-01&bitis=2026-03-31"));

            Assert.Equal(0, veri.GetProperty("araclar").GetArrayLength());
            Assert.Equal(0m, veri.GetProperty("toplamMaliyet").GetDecimal());
        }

        [Fact]
        public async Task DriverFiloMaliyetiniGoremez()
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, "34FLO005");
            var (surucu, _) = await UyeOlusturAsync(sahip, "Driver");

            var cevap = await surucu.GetAsync("/api/Reports/filo-maliyet?baslangic=2026-01-01&bitis=2026-03-31");

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }

        [Fact]
        public async Task ManagerFiloMaliyetiniGorur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34FLO006");
            await YakitEkleAsync(sahip, aracId, "2026-01-10", 100000, 40m, 2000m);
            var (yonetici, _) = await UyeOlusturAsync(sahip, "Manager");

            var cevap = await yonetici.GetAsync("/api/Reports/filo-maliyet?baslangic=2026-01-01&bitis=2026-03-31");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal(1, (await VeriAsync(cevap)).GetProperty("araclar").GetArrayLength());
        }
    }
}
