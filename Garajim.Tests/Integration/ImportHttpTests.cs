using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class ImportHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public ImportHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private const string FuelioCsv =
            "## Vehicle\nName;Fuel unit\nClio;L\n\n## Log\nData;Odometer (km);Fuel volume;Price;Total cost\n" +
            "01.02.2026;123456;42,50;46,60;1980,50\n" +
            "15.02.2026;123900;38,00;47,10;1789,80\n";

        private const string DrivvoCsv =
            "Tarih;Kilometre;Litre;Fiyat;Toplam maliyet\n" +
            "01.03.2026;124000;40,00;47,50;1900,00\n" +
            "10.03.2026;124500;35,50;47,80;1696,90\n";

        private const string GenelMasrafCsv =
            "Date;Total;Category;Notes\n" +
            "2026-04-01;350.00;Parking;aylik otopark\n" +
            "2026-04-15;1250.50;Insurance;police\n";

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "İçe Aktarım", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("driver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "İçe Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka, int km = 120000)
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

        private static MultipartFormDataContent Form(string csv, string kayitTuru, int? vehicleId = null, string eslesme = null, bool? dryRun = null, string dosyaAdi = "veri.csv")
        {
            var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(new UTF8Encoding(true).GetBytes(csv));
            dosya.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(dosya, "file", dosyaAdi);
            form.Add(new StringContent(kayitTuru), "kayitTuru");

            if (vehicleId != null)
                form.Add(new StringContent(vehicleId.Value.ToString()), "vehicleId");
            if (eslesme != null)
                form.Add(new StringContent(eslesme), "eslesme");
            if (dryRun != null)
                form.Add(new StringContent(dryRun.Value ? "true" : "false"), "dryRun");

            return form;
        }

        [Fact]
        public async Task FuelioOnizlemeSablonuVeEslesmeyiBulur()
        {
            var sahip = await SahipOlusturAsync();

            var veri = await VeriAsync(await sahip.PostAsync("/api/Import/onizle", Form(FuelioCsv, "Yakit")));

            Assert.Equal("Fuelio", veri.GetProperty("sablon").GetString());
            Assert.Equal(";", veri.GetProperty("ayrac").GetString());
            var eslesme = veri.GetProperty("onerilenEslesme");
            Assert.True(eslesme.TryGetProperty("tarih", out _));
            Assert.True(eslesme.TryGetProperty("litre", out _));
            Assert.True(eslesme.TryGetProperty("tutar", out _));
        }

        [Fact]
        public async Task DrivvoOnizlemeOrnekSatirlariVerir()
        {
            var sahip = await SahipOlusturAsync();

            var veri = await VeriAsync(await sahip.PostAsync("/api/Import/onizle", Form(DrivvoCsv, "Yakit")));

            Assert.Equal("Drivvo", veri.GetProperty("sablon").GetString());
            Assert.Equal(2, veri.GetProperty("toplamSatir").GetInt32());
            Assert.Equal(2, veri.GetProperty("ornekSatirlar").GetArrayLength());
            Assert.Equal(0, veri.GetProperty("hataliSatirlar").GetArrayLength());
        }

        [Fact]
        public async Task DryRunKayitYazmaz()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34IMP001");

            var veri = await VeriAsync(await sahip.PostAsync("/api/Import/uygula",
                Form(DrivvoCsv, "Yakit", aracId, "{\"tarih\":0,\"km\":1,\"litre\":2,\"tutar\":4}", true)));

            Assert.Equal(2, veri.GetProperty("eklenen").GetInt32());
            Assert.True(veri.GetProperty("dryRun").GetBoolean());

            var yakitlar = await VeriAsync(await sahip.GetAsync($"/api/Fuel?vehicleId={aracId}"));
            Assert.Equal(0, yakitlar.GetArrayLength());
        }

        [Fact]
        public async Task UygulaKayitlariYazarVeKmArtar()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34IMP002", 100000);

            var veri = await VeriAsync(await sahip.PostAsync("/api/Import/uygula",
                Form(DrivvoCsv, "Yakit", aracId, "{\"tarih\":0,\"km\":1,\"litre\":2,\"tutar\":4}", false)));

            Assert.Equal(2, veri.GetProperty("eklenen").GetInt32());

            var yakitlar = await VeriAsync(await sahip.GetAsync($"/api/Fuel?vehicleId={aracId}"));
            Assert.Equal(2, yakitlar.GetArrayLength());

            var arac = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}"));
            Assert.Equal(124500, arac.GetProperty("currentKm").GetInt32());
        }

        [Fact]
        public async Task AyniDosyaIkinciKezSifirYeniKayitEkler()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34IMP003");
            var eslesme = "{\"tarih\":0,\"km\":1,\"litre\":2,\"tutar\":4}";

            await sahip.PostAsync("/api/Import/uygula", Form(DrivvoCsv, "Yakit", aracId, eslesme, false));
            var ikinci = await VeriAsync(await sahip.PostAsync("/api/Import/uygula", Form(DrivvoCsv, "Yakit", aracId, eslesme, false)));

            Assert.Equal(0, ikinci.GetProperty("eklenen").GetInt32());
            Assert.Equal(2, ikinci.GetProperty("atlanan").GetInt32());

            var yakitlar = await VeriAsync(await sahip.GetAsync($"/api/Fuel?vehicleId={aracId}"));
            Assert.Equal(2, yakitlar.GetArrayLength());
        }

        [Fact]
        public async Task KmGerileyenSatirAracKilometresiniDusurmez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34IMP004", 200000);

            await sahip.PostAsync("/api/Import/uygula",
                Form(DrivvoCsv, "Yakit", aracId, "{\"tarih\":0,\"km\":1,\"litre\":2,\"tutar\":4}", false));

            var arac = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}"));
            Assert.Equal(200000, arac.GetProperty("currentKm").GetInt32());
        }

        [Fact]
        public async Task HataliSatirRaporlanirDigerleriYazilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34IMP005");

            var bozuk = "Tarih;Kilometre;Litre;Toplam maliyet\n01.03.2026;124000;40,00;1900,00\nsaçma;abc;xx;yy\n";

            var veri = await VeriAsync(await sahip.PostAsync("/api/Import/uygula",
                Form(bozuk, "Yakit", aracId, "{\"tarih\":0,\"km\":1,\"litre\":2,\"tutar\":3}", false)));

            Assert.Equal(1, veri.GetProperty("eklenen").GetInt32());
            var hatalar = veri.GetProperty("hatali");
            var hata = Assert.Single(hatalar.EnumerateArray());
            Assert.Equal(3, hata.GetProperty("satirNo").GetInt32());
            Assert.Contains("Tarih", hata.GetProperty("sebep").GetString());
        }

        [Fact]
        public async Task GenelCsvMasrafOlarakAktarilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34IMP006");

            var veri = await VeriAsync(await sahip.PostAsync("/api/Import/uygula",
                Form(GenelMasrafCsv, "Masraf", aracId, "{\"tarih\":0,\"tutar\":1,\"aciklama\":3}", false)));

            Assert.Equal(2, veri.GetProperty("eklenen").GetInt32());

            var masraflar = await VeriAsync(await sahip.GetAsync($"/api/Expenses?vehicleId={aracId}"));
            Assert.Equal(2, masraflar.GetArrayLength());
        }

        [Fact]
        public async Task EksikZorunluEslesmeReddedilir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34IMP007");

            var cevap = await sahip.PostAsync("/api/Import/uygula",
                Form(DrivvoCsv, "Yakit", aracId, "{\"tarih\":0}", false));

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task DriverIceAktaramaz()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34IMP008");
            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = aracId, userId = surucuId });

            var onizle = await surucu.PostAsync("/api/Import/onizle", Form(DrivvoCsv, "Yakit"));
            var uygula = await surucu.PostAsync("/api/Import/uygula",
                Form(DrivvoCsv, "Yakit", aracId, "{\"tarih\":0,\"km\":1,\"litre\":2,\"tutar\":4}", false));

            Assert.Equal(HttpStatusCode.Forbidden, onizle.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, uygula.StatusCode);
        }

        [Fact]
        public async Task YabanciSirketinAracina404Doner()
        {
            var birinci = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci, "34IMP009");

            var ikinci = await SahipOlusturAsync();

            var cevap = await ikinci.PostAsync("/api/Import/uygula",
                Form(DrivvoCsv, "Yakit", aracId, "{\"tarih\":0,\"km\":1,\"litre\":2,\"tutar\":4}", false));

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task CokFazlaSatirReddedilir()
        {
            var sahip = await SahipOlusturAsync();

            var sb = new StringBuilder("Tarih;Kilometre;Litre;Toplam maliyet\n");
            for (var i = 0; i < 5001; i++)
            {
                sb.Append("01.03.2026;124000;40,00;1900,00\n");
            }

            var cevap = await sahip.PostAsync("/api/Import/onizle", Form(sb.ToString(), "Yakit"));

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }
    }
}
