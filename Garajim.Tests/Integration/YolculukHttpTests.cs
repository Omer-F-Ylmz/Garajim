using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class YolculukHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public YolculukHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static string Plaka() => TestPlaka.Uret();

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("yol"), fullName = "Yol Sahip", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("yoldriver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Yol Sürücü", role = "Driver" });
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

        private static Task<HttpResponseMessage> YolculukEkleAsync(HttpClient client, int aracId, int basKm, int bitisKm, string amac = "Is", string tarih = "2026-03-01")
        {
            return client.PostAsJsonAsync("/api/Yolculuk", new
            {
                vehicleId = aracId,
                tarih,
                baslangicKm = basKm,
                bitisKm,
                amac,
                nereden = "Kadıköy",
                nereye = "Ataşehir",
                not = "müşteri ziyareti"
            });
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task YolculukEklenirMesafeHesaplanir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());

            var veri = await VeriAsync(await YolculukEkleAsync(sahip, aracId, 100000, 100120));

            Assert.Equal(120, veri.GetProperty("mesafeKm").GetInt32());
            Assert.Equal("Is", veri.GetProperty("amac").GetString());
            Assert.Equal("Kadıköy", veri.GetProperty("nereden").GetString());
        }

        [Fact]
        public async Task MesafeDegismeziGuncellemedeDeKorunur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());
            var id = (await VeriAsync(await YolculukEkleAsync(sahip, aracId, 100000, 100120))).GetProperty("id").GetInt32();

            await sahip.PutAsJsonAsync($"/api/Yolculuk/{id}", new
            {
                tarih = "2026-03-02",
                baslangicKm = 100200,
                bitisKm = 100500,
                amac = "Ozel",
                nereden = "Ev",
                nereye = "Tatil"
            });

            var liste = await VeriAsync(await sahip.GetAsync($"/api/Yolculuk?vehicleId={aracId}"));
            var kayit = liste[0];

            Assert.Equal(300, kayit.GetProperty("mesafeKm").GetInt32());
            Assert.Equal(kayit.GetProperty("bitisKm").GetInt32() - kayit.GetProperty("baslangicKm").GetInt32(), kayit.GetProperty("mesafeKm").GetInt32());
            Assert.Equal("Ozel", kayit.GetProperty("amac").GetString());
        }

        [Fact]
        public async Task AracKilometresiYalnizYukariGuncellenir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka(), 100000);

            await YolculukEkleAsync(sahip, aracId, 100000, 100450);
            var artan = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}"));
            Assert.Equal(100450, artan.GetProperty("currentKm").GetInt32());

            await YolculukEkleAsync(sahip, aracId, 99000, 99500, "Ozel", "2026-02-01");
            var sonra = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}"));
            Assert.Equal(100450, sonra.GetProperty("currentKm").GetInt32());
        }

        [Theory]
        [InlineData(100000, 100000)]
        [InlineData(100000, 99000)]
        [InlineData(-5, 100)]
        public async Task GecersizKilometreReddedilir(int basKm, int bitisKm)
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());

            var cevap = await YolculukEkleAsync(sahip, aracId, basKm, bitisKm);

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task TanimsizAmacReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());

            var cevap = await sahip.PostAsJsonAsync("/api/Yolculuk", new
            {
                vehicleId = aracId,
                tarih = "2026-03-01",
                baslangicKm = 100000,
                bitisKm = 100100,
                amac = 7
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task GelecekTarihReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());

            var cevap = await YolculukEkleAsync(sahip, aracId, 100000, 100100, "Is", DateTime.UtcNow.Date.AddDays(5).ToString("yyyy-MM-dd"));

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task OzetIsVeOzelKilometreyiAyirir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());

            await YolculukEkleAsync(sahip, aracId, 100000, 100300, "Is", "2026-03-01");
            await YolculukEkleAsync(sahip, aracId, 100300, 100400, "Ozel", "2026-03-02");
            await YolculukEkleAsync(sahip, aracId, 100400, 100500, "Is", "2026-03-03");

            var ozet = await VeriAsync(await sahip.GetAsync($"/api/Yolculuk/ozet?vehicleId={aracId}&baslangic=2026-03-01&bitis=2026-03-31"));

            Assert.Equal(500, ozet.GetProperty("toplamKm").GetInt32());
            Assert.Equal(400, ozet.GetProperty("isKm").GetInt32());
            Assert.Equal(100, ozet.GetProperty("ozelKm").GetInt32());
            Assert.Equal(3, ozet.GetProperty("yolculukSayisi").GetInt32());
            Assert.Equal(80m, ozet.GetProperty("isOrani").GetDecimal());
        }

        [Fact]
        public async Task TarihAraligiSuzer()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());

            await YolculukEkleAsync(sahip, aracId, 100000, 100300, "Is", "2026-01-10");
            await YolculukEkleAsync(sahip, aracId, 100300, 100400, "Is", "2026-03-31");

            var liste = await VeriAsync(await sahip.GetAsync($"/api/Yolculuk?vehicleId={aracId}&baslangic=2026-03-01&bitis=2026-03-31"));

            Assert.Equal(1, liste.GetArrayLength());
            Assert.Equal(100, liste[0].GetProperty("mesafeKm").GetInt32());
        }

        [Fact]
        public async Task DriverZimmetliAracaKayitEklerBaskasininkiniSilemez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());
            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = surucuId });

            var sahipKaydi = (await VeriAsync(await YolculukEkleAsync(sahip, aracId, 100000, 100100))).GetProperty("id").GetInt32();

            var surucuCevap = await YolculukEkleAsync(surucu, aracId, 100100, 100250, "Is", "2026-03-05");
            Assert.Equal(HttpStatusCode.OK, surucuCevap.StatusCode);

            var silme = await surucu.DeleteAsync($"/api/Yolculuk/{sahipKaydi}");
            Assert.Equal(HttpStatusCode.Forbidden, silme.StatusCode);
        }

        [Fact]
        public async Task DriverZimmetsizAracaKayitEkleyemez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());
            var (surucu, _) = await SurucuOlusturAsync(sahip);

            var cevap = await YolculukEkleAsync(surucu, aracId, 100000, 100100);

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task SirketlerBirbirinYolculugunuGormez()
        {
            var birinci = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci, Plaka());
            await YolculukEkleAsync(birinci, aracId, 100000, 100100);

            var ikinci = await SahipOlusturAsync();

            var liste = await VeriAsync(await ikinci.GetAsync("/api/Yolculuk"));
            Assert.Equal(0, liste.GetArrayLength());

            var yabanci = await ikinci.GetAsync($"/api/Yolculuk?vehicleId={aracId}");
            Assert.Equal(HttpStatusCode.NotFound, yabanci.StatusCode);
        }

        [Fact]
        public async Task SilinenKayitListedenDuser()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());
            var id = (await VeriAsync(await YolculukEkleAsync(sahip, aracId, 100000, 100100))).GetProperty("id").GetInt32();

            Assert.Equal(HttpStatusCode.OK, (await sahip.DeleteAsync($"/api/Yolculuk/{id}")).StatusCode);

            var liste = await VeriAsync(await sahip.GetAsync($"/api/Yolculuk?vehicleId={aracId}"));
            Assert.Equal(0, liste.GetArrayLength());
        }
    }
}
