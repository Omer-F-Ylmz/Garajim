using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Garajim.Tests.Integration
{
    public class YuklemeSiniriHttpTests : IDisposable
    {
        private static readonly byte[] PngBaslik = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private readonly GarajimWebApplicationFactory _factory = new GarajimWebApplicationFactory();

        public void Dispose()
        {
            _factory.Dispose();
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("yukleme"), fullName = "Yükleme Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static byte[] Dosya(int boyut)
        {
            var icerik = new byte[Math.Max(PngBaslik.Length, boyut)];
            Array.Copy(PngBaslik, icerik, PngBaslik.Length);
            return icerik;
        }

        private static MultipartFormDataContent Form(byte[] icerik, string ad)
        {
            var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(icerik);
            dosya.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(dosya, "file", ad);
            return form;
        }

        [Fact]
        public async Task CokBuyukBelgeIstegiGovdeSinirindaReddedilir()
        {
            var client = await SahipOlusturAsync();

            using var form = Form(Dosya(12 * 1024 * 1024), "buyuk.png");
            form.Add(new StringContent("1"), "vehicleId");

            var cevap = await client.PostAsync("/api/Documents", form);

            Assert.True(
                cevap.StatusCode == HttpStatusCode.RequestEntityTooLarge ||
                cevap.StatusCode == HttpStatusCode.BadRequest,
                "12 MB'lık gövde reddedilmeliydi, dönen: " + cevap.StatusCode);
        }

        [Fact]
        public async Task CokBuyukFisIstegiReddedilir()
        {
            var client = await SahipOlusturAsync();

            using var form = Form(Dosya(12 * 1024 * 1024), "fis.png");

            var cevap = await client.PostAsync("/api/Receipts", form);

            Assert.True(
                cevap.StatusCode == HttpStatusCode.RequestEntityTooLarge ||
                cevap.StatusCode == HttpStatusCode.BadRequest,
                "12 MB'lık fiş gövdesi reddedilmeliydi, dönen: " + cevap.StatusCode);
        }

        [Fact]
        public async Task PahaliUclarHizSinirinaTabi()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);

            var klasor = Path.Combine(kok.FullName, "Garajim.API", "Controllers");

            foreach (var dosya in new[]
                     {
                         "PricePredictionController.cs",
                         "ImportController.cs",
                         "ExportController.cs",
                         "DocumentsController.cs",
                         "ReceiptsController.cs",
                         "UstaController.cs"
                     })
            {
                var kaynak = await File.ReadAllTextAsync(Path.Combine(klasor, dosya));

                Assert.True(kaynak.Contains("EnableRateLimiting", StringComparison.Ordinal),
                    dosya + " pahalı uç taşıyor ama hiçbir hız sınırı politikası yok.");
            }
        }

        [Fact]
        public async Task IstekGovdeSiniriYapilandirilmis()
        {
            var kok = new DirectoryInfo(AppContext.BaseDirectory);
            while (kok != null && !File.Exists(Path.Combine(kok.FullName, "Garajim.sln")))
            {
                kok = kok.Parent;
            }

            Assert.NotNull(kok);

            var program = await File.ReadAllTextAsync(Path.Combine(kok.FullName, "Garajim.API", "Program.cs"));

            Assert.Contains("MaxRequestBodySize", program);
            Assert.Contains("MultipartBodyLengthLimit", program);
        }

        [Fact]
        public async Task GecerliBoyuttakiBelgeHalaYuklenir()
        {
            var client = await SahipOlusturAsync();

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Renault", model = "Clio", year = 2019, currentKm = 90000,
                fuelType = "Benzin", vites = "Otomatik", kasaTipi = "Hatchback5"
            });
            var aracId = System.Text.Json.JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            using var form = Form(Dosya(2048), "kucuk.png");
            form.Add(new StringContent(aracId.ToString()), "vehicleId");

            var cevap = await client.PostAsync("/api/Documents", form);

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        }
    }
}
