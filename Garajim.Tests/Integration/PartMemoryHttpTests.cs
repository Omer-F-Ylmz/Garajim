using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class PartMemoryHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public PartMemoryHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "Parça Sahibi", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("driver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Parça Sürücü", role = "Driver" });
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

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        private static Task<HttpResponseMessage> BakimEkleAsync(HttpClient client, int aracId, string tarih, int km, decimal tutar, object[] parcalar)
        {
            return client.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                date = tarih,
                km,
                cost = tutar,
                serviceName = "Servis",
                note = "",
                parcalar
            });
        }

        [Fact]
        public async Task BakimKaydiParcalariylaBirlikteOlusur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34PRC001");

            var cevap = await BakimEkleAsync(sahip, aracId, "2026-06-01", 95000, 4200m, new object[]
            {
                new { parcaTuru = "MotorYagi", aciklama = "5W30 tam sentetik", adet = 1, tutar = 1800m, marka = "Castrol" },
                new { parcaTuru = "YagFiltresi", aciklama = "Yağ filtresi", adet = 1, tutar = 350m, marka = (string)null }
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var bakimlar = await VeriAsync(await sahip.GetAsync($"/api/Maintenance?vehicleId={aracId}"));
            var kayit = Assert.Single(bakimlar.EnumerateArray());
            var parcalar = kayit.GetProperty("parcalar");

            Assert.Equal(2, parcalar.GetArrayLength());
            Assert.Equal("MotorYagi", parcalar[0].GetProperty("parcaTuru").GetString());
            Assert.Equal(1800m, parcalar[0].GetProperty("tutar").GetDecimal());
        }

        [Fact]
        public async Task GecersizParcaTuruReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34PRC002");

            var cevap = await BakimEkleAsync(sahip, aracId, "2026-06-01", 95000, 4200m, new object[]
            {
                new { parcaTuru = "MotorYagi", aciklama = "yağ", adet = 0, tutar = 100m, marka = (string)null }
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task ParcaninCompanyIdVeVehicleIdsiKaydinkiyleAynidir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34PRC003");

            await BakimEkleAsync(sahip, aracId, "2026-06-01", 95000, 1000m, new object[]
            {
                new { parcaTuru = "Buji", aciklama = "buji seti", adet = 4, tutar = 800m, marka = (string)null }
            });

            var hafiza = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/parca-hafizasi"));
            var buji = hafiza.EnumerateArray().Single(p => p.GetProperty("parcaTuru").GetString() == "Buji");

            Assert.Equal(1, buji.GetProperty("degisimSayisi").GetInt32());
            Assert.Equal(800m, buji.GetProperty("toplamTutar").GetDecimal());
            Assert.Equal(95000, buji.GetProperty("sonDegisimKm").GetInt32());
        }

        [Fact]
        public async Task YabanciSirketinBakimina404Doner()
        {
            var birinci = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci, "34PRC004");
            await BakimEkleAsync(birinci, aracId, "2026-06-01", 95000, 1000m, new object[]
            {
                new { parcaTuru = "HavaFiltresi", aciklama = "hava filtresi", adet = 1, tutar = 200m, marka = (string)null }
            });

            var ikinci = await SahipOlusturAsync();

            var cevap = await ikinci.GetAsync($"/api/Vehicles/{aracId}/parca-hafizasi");

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task HafizaDurumuAraliktanHesaplanir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34PRC005", 101500);

            await BakimEkleAsync(sahip, aracId, "2026-01-01", 92000, 2000m, new object[]
            {
                new { parcaTuru = "MotorYagi", aciklama = "yağ", adet = 1, tutar = 1500m, marka = (string)null },
                new { parcaTuru = "FrenBalatasiOn", aciklama = "ön balata", adet = 1, tutar = 500m, marka = (string)null }
            });

            var hafiza = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/parca-hafizasi"));

            var yag = hafiza.EnumerateArray().Single(p => p.GetProperty("parcaTuru").GetString() == "MotorYagi");
            Assert.Equal(102000, yag.GetProperty("sonrakiTahminiKm").GetInt32());
            Assert.Equal("Yaklasiyor", yag.GetProperty("durum").GetString());

            var balata = hafiza.EnumerateArray().Single(p => p.GetProperty("parcaTuru").GetString() == "FrenBalatasiOn");
            Assert.Equal(132000, balata.GetProperty("sonrakiTahminiKm").GetInt32());
            Assert.Equal("Iyi", balata.GetProperty("durum").GetString());
        }

        [Fact]
        public async Task AraligiGecenParcaGectiDoner()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34PRC006", 130000);

            await BakimEkleAsync(sahip, aracId, "2026-01-01", 100000, 2000m, new object[]
            {
                new { parcaTuru = "MotorYagi", aciklama = "yağ", adet = 1, tutar = 1500m, marka = (string)null }
            });

            var hafiza = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/parca-hafizasi"));
            var yag = hafiza.EnumerateArray().Single(p => p.GetProperty("parcaTuru").GetString() == "MotorYagi");

            Assert.Equal("Gecti", yag.GetProperty("durum").GetString());
        }

        [Fact]
        public async Task HatirlatmaOlusturulurVeTahmindenDoldurulur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34PRC007", 100000);

            await BakimEkleAsync(sahip, aracId, "2026-01-01", 95000, 2000m, new object[]
            {
                new { parcaTuru = "MotorYagi", aciklama = "yağ", adet = 1, tutar = 1500m, marka = (string)null }
            });

            var cevap = await sahip.PostAsync($"/api/Vehicles/{aracId}/parca-hafizasi/MotorYagi/hatirlatma", null);

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            var hatirlatmalar = await VeriAsync(await sahip.GetAsync($"/api/Reminders?vehicleId={aracId}"));
            var hatirlatma = Assert.Single(hatirlatmalar.EnumerateArray());

            Assert.Equal(105000, hatirlatma.GetProperty("dueKm").GetInt32());
            Assert.Contains("yağ", hatirlatma.GetProperty("note").GetString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task DriverZimmetsizAracinHafizasiniGoremez()
        {
            var sahip = await SahipOlusturAsync();
            var zimmetli = await AracEkleAsync(sahip, "34PRC008");
            var zimmetsiz = await AracEkleAsync(sahip, "34PRC009");
            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = zimmetli, userId = surucuId });

            Assert.Equal(HttpStatusCode.OK, (await surucu.GetAsync($"/api/Vehicles/{zimmetli}/parca-hafizasi")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await surucu.GetAsync($"/api/Vehicles/{zimmetsiz}/parca-hafizasi")).StatusCode);
        }

        [Fact]
        public async Task GuncellemeTumParcalariDegistirir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34PRC010");

            await BakimEkleAsync(sahip, aracId, "2026-06-01", 95000, 4200m, new object[]
            {
                new { parcaTuru = "MotorYagi", aciklama = "yağ", adet = 1, tutar = 1800m, marka = (string)null },
                new { parcaTuru = "YagFiltresi", aciklama = "filtre", adet = 1, tutar = 350m, marka = (string)null }
            });

            var bakimlar = await VeriAsync(await sahip.GetAsync($"/api/Maintenance?vehicleId={aracId}"));
            var kayitId = bakimlar[0].GetProperty("id").GetInt32();

            var guncelle = await sahip.PutAsJsonAsync($"/api/Maintenance/{kayitId}", new
            {
                type = "PeriyodikBakim",
                date = "2026-06-01",
                km = 95000,
                cost = 4200m,
                serviceName = "Servis",
                note = "",
                parcalar = new object[]
                {
                    new { parcaTuru = "Buji", aciklama = "buji", adet = 4, tutar = 900m, marka = (string)null }
                }
            });

            Assert.Equal(HttpStatusCode.OK, guncelle.StatusCode);

            var sonrasi = await VeriAsync(await sahip.GetAsync($"/api/Maintenance?vehicleId={aracId}"));
            var parcalar = sonrasi[0].GetProperty("parcalar");

            Assert.Equal(1, parcalar.GetArrayLength());
            Assert.Equal("Buji", parcalar[0].GetProperty("parcaTuru").GetString());
        }
    }
}
