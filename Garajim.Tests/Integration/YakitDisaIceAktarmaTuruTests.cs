using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class YakitDisaIceAktarmaTuruTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public YakitDisaIceAktarmaTuruTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("tur"), fullName = "Tur Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<int> AracEkleAsync(HttpClient client, int km)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Fiat",
                model = "Egea",
                year = 2020,
                currentKm = km,
                fuelType = "Benzin"
            });

            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        private static MultipartFormDataContent Form(string csv, int aracId)
        {
            var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(new UTF8Encoding(true).GetBytes(csv));
            dosya.Headers.ContentType = new MediaTypeHeaderValue("text/csv");
            form.Add(dosya, "file", "yakit.csv");
            form.Add(new StringContent("Yakit"), "kayitTuru");
            form.Add(new StringContent(aracId.ToString()), "vehicleId");
            return form;
        }

        [Fact]
        public async Task DisaAktarilanYakitDosyasiTamDolumSutunuTasir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, 10000);

            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-01", liters = 40m, totalCost = 1900m, km = 10100, tamDolum = true });
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = aracId, date = "2026-03-10", liters = 20m, totalCost = 950m, km = 10300, tamDolum = false });

            var metin = await (await sahip.GetAsync($"/api/Export/yakit.csv?vehicleId={aracId}")).Content.ReadAsStringAsync();
            var satirlar = metin.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            Assert.Contains("TamDolum", satirlar[0]);
            Assert.Contains("Evet", satirlar[1]);
            Assert.Contains("Hayır", satirlar[2]);
        }

        [Fact]
        public async Task DisaAktarIceAktarTuruKismiDolumuKorur()
        {
            var sahip = await SahipOlusturAsync();
            var kaynak = await AracEkleAsync(sahip, 10000);

            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = kaynak, date = "2026-03-01", liters = 40m, totalCost = 1900m, km = 10100, tamDolum = true });
            await sahip.PostAsJsonAsync("/api/Fuel", new { vehicleId = kaynak, date = "2026-03-10", liters = 20m, totalCost = 950m, km = 10300, tamDolum = false });

            var csv = await (await sahip.GetAsync($"/api/Export/yakit.csv?vehicleId={kaynak}")).Content.ReadAsStringAsync();

            var hedef = await AracEkleAsync(sahip, 9000);

            var onizleme = await VeriAsync(await sahip.PostAsync("/api/Import/onizle", Form(csv, hedef)));
            var eslesme = onizleme.GetProperty("onerilenEslesme");

            Assert.True(eslesme.TryGetProperty("tamdolum", out _), "onerilen eslesme tamdolum tasimiyor: " + eslesme);

            var form = Form(csv, hedef);
            form.Add(new StringContent(eslesme.GetRawText()), "eslesme");
            form.Add(new StringContent("false"), "dryRun");

            var sonuc = await VeriAsync(await sahip.PostAsync("/api/Import/uygula", form));
            Assert.Equal(2, sonuc.GetProperty("eklenen").GetInt32());

            var yakitlar = (await VeriAsync(await sahip.GetAsync($"/api/Fuel?vehicleId={hedef}")))
                .EnumerateArray()
                .OrderBy(y => y.GetProperty("km").GetInt32())
                .ToList();

            Assert.True(yakitlar[0].GetProperty("tamDolum").GetBoolean());
            Assert.False(yakitlar[1].GetProperty("tamDolum").GetBoolean());
        }
    }
}
