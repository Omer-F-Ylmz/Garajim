using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Business.Abstract;
using Garajim.Entity.Dtos;
using Garajim.Entity.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class FisTaslakSayfalamaHttpTests : IDisposable
    {
        private sealed class SahteExtractor : IReceiptExtractor
        {
            public ReceiptExtractionResult Sonuc { get; set; } = new ReceiptExtractionResult();

            public Task<ReceiptExtractionResult> ExtractAsync(byte[] imageBytes, string mimeType, CancellationToken ct)
            {
                return Task.FromResult(Sonuc);
            }
        }

        private sealed class FisFactory : GarajimWebApplicationFactory
        {
            private readonly string _klasor;

            public SahteExtractor Extractor { get; } = new SahteExtractor();

            public FisFactory(string klasor)
            {
                _klasor = klasor;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureAppConfiguration(configuration =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string>
                    {
                        ["Documents:StoragePath"] = _klasor,
                        ["Receipts:AylikLimit"] = "100"
                    });
                });

                builder.ConfigureServices(services =>
                {
                    var kayitlar = services.Where(s => s.ServiceType == typeof(IReceiptExtractor)).ToList();
                    foreach (var kayit in kayitlar)
                    {
                        services.Remove(kayit);
                    }

                    services.AddSingleton<IReceiptExtractor>(Extractor);
                });
            }
        }

        private static readonly byte[] PngIcerik = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };

        private readonly string _klasor;
        private readonly FisFactory _factory;

        public FisTaslakSayfalamaHttpTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-fis-sayfa-" + Guid.NewGuid().ToString("N"));
            _factory = new FisFactory(_klasor);
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("fissayfa"), fullName = "Fiş Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task YukleAsync(HttpClient client, string dosyaAdi, string plaka)
        {
            _factory.Extractor.Sonuc = new ReceiptExtractionResult
            {
                Tarih = new DateTime(2026, 8, 20),
                ToplamTutar = 1980.50m,
                Plaka = plaka,
                TahminiTur = ReceiptType.Yakit,
                GuvenSkoru = 0.9
            };

            var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(PngIcerik);
            dosya.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(dosya, "file", dosyaAdi);

            var cevap = await client.PostAsync("/api/Receipts", form);

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
        }

        private static async Task<JsonElement> VeriAsync(HttpClient client, string yol)
        {
            var cevap = await client.GetAsync(yol);
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.True(cevap.IsSuccessStatusCode, govde);

            return JsonDocument.Parse(govde).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task ParametresizIstekEskiDuzBicimiKorur()
        {
            var client = await SahipOlusturAsync();
            await YukleAsync(client, "fis1.png", "34FIS001");

            var veri = await VeriAsync(client, "/api/Receipts");

            Assert.Equal(JsonValueKind.Array, veri.ValueKind);
            Assert.Equal(1, veri.GetArrayLength());
        }

        [Fact]
        public async Task SayfaParametresiZarfDondurur()
        {
            var client = await SahipOlusturAsync();
            await YukleAsync(client, "fis1.png", "34FIS001");
            await YukleAsync(client, "fis2.png", "34FIS002");
            await YukleAsync(client, "fis3.png", "34FIS003");

            var veri = await VeriAsync(client, "/api/Receipts?sayfa=1&boyut=2");

            Assert.Equal(JsonValueKind.Object, veri.ValueKind);
            Assert.Equal(3, veri.GetProperty("toplam").GetInt32());
            Assert.Equal(2, veri.GetProperty("kayitlar").GetArrayLength());
        }

        [Fact]
        public async Task PlakayaGoreAranir()
        {
            var client = await SahipOlusturAsync();
            await YukleAsync(client, "fis1.png", "34FIS001");
            await YukleAsync(client, "fis2.png", "06ABC123");

            var veri = await VeriAsync(client, "/api/Receipts?q=06ABC");

            Assert.Equal(1, veri.GetProperty("toplam").GetInt32());
        }

        [Fact]
        public async Task GecersizSiralamaAlaniDortYuzDoner()
        {
            var client = await SahipOlusturAsync();

            var cevap = await client.GetAsync("/api/Receipts?sirala=plaka:desc");

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        public void Dispose()
        {
            _factory.Dispose();

            if (Directory.Exists(_klasor))
            {
                Directory.Delete(_klasor, true);
            }
        }
    }
}
