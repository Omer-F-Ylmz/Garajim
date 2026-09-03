using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class ExportHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public ExportHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("export"), fullName = "Dışa Sahip", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("exportdriver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Dışa Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 100000,
                fuelType = "Benzin"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static string Plaka() => TestPlaka.Uret();

        [Fact]
        public async Task YakitDisaAktarimiBomluCsvDoner()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-01", liters = 40.5m, totalCost = 1900.75m, km = 100500 });

            var cevap = await sahip.GetAsync($"/api/Export/yakit.csv?vehicleId={aracId}");
            var bayt = await cevap.Content.ReadAsByteArrayAsync();

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal("text/csv", cevap.Content.Headers.ContentType.MediaType);
            Assert.Equal(0xEF, bayt[0]);
            Assert.Equal(0xBB, bayt[1]);
            Assert.Equal(0xBF, bayt[2]);

            var metin = Encoding.UTF8.GetString(bayt, 3, bayt.Length - 3);
            var satirlar = metin.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

            Assert.Equal("Plaka;Tarih;Kilometre;Litre;BirimFiyat;Tutar;Kwh;SarjTuru", satirlar[0]);
            Assert.Equal(2, satirlar.Length);
            Assert.Contains("01.03.2026", satirlar[1]);
            Assert.Contains("40,50", satirlar[1]);
            Assert.Contains("1900,75", satirlar[1]);
        }

        [Fact]
        public async Task DosyaAdiEkOlarakGonderilir()
        {
            var sahip = await SahipOlusturAsync();
            await AracEkleAsync(sahip, Plaka());

            var cevap = await sahip.GetAsync("/api/Export/masraf.csv");

            Assert.Equal("attachment", cevap.Content.Headers.ContentDisposition.DispositionType);
            Assert.Contains("masraf", cevap.Content.Headers.ContentDisposition.FileName);
        }

        [Fact]
        public async Task BakimVeMasrafBasliklariDoner()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());
            await sahip.PostAsJsonAsync("/api/Maintenance", new { vehicleId = aracId, type = "PeriyodikBakim", date = "2026-03-05", km = 100600, cost = 3000m, serviceName = "Yetkili; Servis" });
            await sahip.PostAsJsonAsync("/api/Expenses", new { vehicleId = aracId, category = "Otopark", date = "2026-03-06", amount = 500m, note = "aylık" });

            var bakim = await (await sahip.GetAsync($"/api/Export/bakim.csv?vehicleId={aracId}")).Content.ReadAsStringAsync();
            var masraf = await (await sahip.GetAsync($"/api/Export/masraf.csv?vehicleId={aracId}")).Content.ReadAsStringAsync();

            Assert.Contains("Plaka;Tarih;Kilometre;Tur;Servis;Tutar;Not", bakim);
            Assert.Contains("\"Yetkili; Servis\"", bakim);
            Assert.Contains("Plaka;Tarih;Kategori;Tutar;Not", masraf);
            Assert.Contains("Otopark", masraf);
        }

        [Fact]
        public async Task EvrakDisaAktarimiCalisir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());
            await sahip.PostAsJsonAsync("/api/Evrak", new { vehicleId = aracId, evrakTuru = "Kasko", bitisTarihi = "2026-12-31" });

            var metin = await (await sahip.GetAsync($"/api/Export/evrak.csv?vehicleId={aracId}")).Content.ReadAsStringAsync();

            Assert.Contains("Plaka;Tur;Baslangic;Bitis;Saglayici;PoliceNo;Durum", metin);
            Assert.Contains("Kasko", metin);
            Assert.Contains("31.12.2026", metin);
        }

        [Fact]
        public async Task TarihAraligiSuzer()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, Plaka());
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-01-15", liters = 30m, totalCost = 1400m, km = 100200 });
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-15", liters = 35m, totalCost = 1600m, km = 100800 });

            var metin = await (await sahip.GetAsync($"/api/Export/yakit.csv?vehicleId={aracId}&baslangic=2026-03-01&bitis=2026-03-31")).Content.ReadAsStringAsync();
            var satirlar = metin.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

            Assert.Equal(2, satirlar.Length);
            Assert.Contains("15.03.2026", satirlar[1]);
        }

        [Fact]
        public async Task AracVerilmezseTumErisilebilirAraclarGelir()
        {
            var sahip = await SahipOlusturAsync();
            var birinci = await AracEkleAsync(sahip, Plaka());
            var ikinci = await AracEkleAsync(sahip, Plaka());
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = birinci, date = "2026-03-01", liters = 30m, totalCost = 1400m, km = 100200 });
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = ikinci, date = "2026-03-02", liters = 35m, totalCost = 1600m, km = 100300 });

            var metin = await (await sahip.GetAsync("/api/Export/yakit.csv")).Content.ReadAsStringAsync();
            var satirlar = metin.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

            Assert.Equal(3, satirlar.Length);
        }

        [Fact]
        public async Task BilinmeyenTur404Doner()
        {
            var sahip = await SahipOlusturAsync();

            var cevap = await sahip.GetAsync("/api/Export/bilinmeyen.csv");

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task YabanciSirketinAraci404Doner()
        {
            var birinci = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(birinci, Plaka());

            var ikinci = await SahipOlusturAsync();
            var cevap = await ikinci.GetAsync($"/api/Export/yakit.csv?vehicleId={aracId}");

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task DriverYalnizZimmetliAraciDisaAktarir()
        {
            var sahip = await SahipOlusturAsync();
            var zimmetli = await AracEkleAsync(sahip, Plaka());
            var digeri = await AracEkleAsync(sahip, Plaka());
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = zimmetli, date = "2026-03-01", liters = 30m, totalCost = 1400m, km = 100200 });
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = digeri, date = "2026-03-02", liters = 35m, totalCost = 1600m, km = 100300 });

            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = zimmetli, userId = surucuId });

            var metin = await (await surucu.GetAsync("/api/Export/yakit.csv")).Content.ReadAsStringAsync();
            var satirlar = metin.Replace("\r\n", "\n").TrimEnd('\n').Split('\n');

            Assert.Equal(2, satirlar.Length);

            var yabanci = await surucu.GetAsync($"/api/Export/yakit.csv?vehicleId={digeri}");
            Assert.Equal(HttpStatusCode.NotFound, yabanci.StatusCode);
        }

        [Fact]
        public async Task DisaAktarilanYakitDosyasiGeriIceAktarilabilir()
        {
            var sahip = await SahipOlusturAsync();
            var kaynak = await AracEkleAsync(sahip, Plaka());
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = kaynak, date = "2026-03-01", liters = 40m, totalCost = 1900m, km = 100500 });
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = kaynak, date = "2026-03-15", liters = 35m, totalCost = 1600m, km = 101000 });

            var bayt = await (await sahip.GetAsync($"/api/Export/yakit.csv?vehicleId={kaynak}")).Content.ReadAsByteArrayAsync();

            var hedef = await AracEkleAsync(sahip, Plaka());
            var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(bayt);
            dosya.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(dosya, "file", "yakit.csv");
            form.Add(new StringContent("Yakit"), "kayitTuru");

            var onizleme = JsonDocument.Parse(await (await sahip.PostAsync("/api/Import/onizle", form)).Content.ReadAsStringAsync())
                .RootElement.GetProperty("data");

            Assert.Equal(2, onizleme.GetProperty("toplamSatir").GetInt32());
            Assert.Equal(0, onizleme.GetProperty("hataliSatirlar").GetArrayLength());

            var eslesme = onizleme.GetProperty("onerilenEslesme");
            Assert.True(eslesme.TryGetProperty("tarih", out _));
            Assert.True(eslesme.TryGetProperty("litre", out _));
            Assert.True(eslesme.TryGetProperty("tutar", out _));
            Assert.True(eslesme.TryGetProperty("km", out _));
        }
    }
}
