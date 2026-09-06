using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class BelgeOnizlemeHttpTests : IDisposable
    {
        private sealed class BelgeFactory : GarajimWebApplicationFactory
        {
            private readonly string _klasor;

            public BelgeFactory(string klasor)
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
                        ["Documents:StoragePath"] = _klasor
                    });
                });
            }
        }

        private static readonly byte[] PngIcerik = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4 };
        private static readonly byte[] PdfIcerik = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34, 10, 11 };

        private readonly string _klasor;
        private readonly BelgeFactory _factory;

        public BelgeOnizlemeHttpTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-onizleme-" + Guid.NewGuid().ToString("N"));
            _factory = new BelgeFactory(_klasor);
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int AracId)> HazirlaAsync(string on, string plaka)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Belge", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Fiat",
                model = "Egea",
                year = 2019,
                currentKm = 40000,
                fuelType = "Dizel"
            });

            var govde = await arac.Content.ReadAsStringAsync();
            Assert.True(arac.IsSuccessStatusCode, govde);

            return (client, JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32());
        }

        private static async Task<int> BelgeYukleAsync(HttpClient client, int aracId, string ad, byte[] icerik, string tip)
        {
            using var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(icerik);
            dosya.Headers.ContentType = new MediaTypeHeaderValue(tip);
            form.Add(dosya, "file", ad);
            form.Add(new StringContent(aracId.ToString()), "vehicleId");

            var cevap = await client.PostAsync("/api/Documents", form);
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.True(cevap.IsSuccessStatusCode, govde);

            return JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task<int> BakimEkleAsync(HttpClient client, int aracId)
        {
            var cevap = await client.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                date = DateTime.UtcNow.Date.AddDays(-3),
                km = 41000,
                cost = 1500m,
                serviceName = "Servis"
            });

            var govde = await cevap.Content.ReadAsStringAsync();
            Assert.True(cevap.IsSuccessStatusCode, govde);

            return JsonDocument.Parse(govde).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        [Fact]
        public async Task GorselOnizlemeSatirIciDoner()
        {
            var (client, aracId) = await HazirlaAsync("onizgorsel", "34BO1001");
            var id = await BelgeYukleAsync(client, aracId, "ruhsat.png", PngIcerik, "image/png");

            var cevap = await client.GetAsync($"/api/Documents/{id}/onizleme");

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
            Assert.Equal("image/png", cevap.Content.Headers.ContentType?.MediaType);
            Assert.Equal("inline", cevap.Content.Headers.ContentDisposition?.DispositionType);
        }

        [Fact]
        public async Task PdfOnizlemeSatirIciDoner()
        {
            var (client, aracId) = await HazirlaAsync("onizpdf", "34BO1002");
            var id = await BelgeYukleAsync(client, aracId, "police.pdf", PdfIcerik, "application/pdf");

            var cevap = await client.GetAsync($"/api/Documents/{id}/onizleme");

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());
            Assert.Equal("application/pdf", cevap.Content.Headers.ContentType?.MediaType);
            Assert.Equal("inline", cevap.Content.Headers.ContentDisposition?.DispositionType);
        }

        [Fact]
        public async Task BaskaSirketinBelgesiOnizlenemez()
        {
            var (yabanci, yabanciArac) = await HazirlaAsync("onizyab", "34BO1003");
            var id = await BelgeYukleAsync(yabanci, yabanciArac, "ruhsat.png", PngIcerik, "image/png");

            var (client, _) = await HazirlaAsync("onizben", "34BO1004");

            var cevap = await client.GetAsync($"/api/Documents/{id}/onizleme");

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task BelgeBakimKaydinaBaglanir()
        {
            var (client, aracId) = await HazirlaAsync("baglabelge", "34BO1005");
            var belgeId = await BelgeYukleAsync(client, aracId, "fatura.pdf", PdfIcerik, "application/pdf");
            var bakimId = await BakimEkleAsync(client, aracId);

            var cevap = await client.PutAsJsonAsync($"/api/Documents/{belgeId}/bagla", new { maintenanceRecordId = bakimId });

            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());

            var liste = await client.GetAsync($"/api/Documents?maintenanceRecordId={bakimId}");
            var veri = JsonDocument.Parse(await liste.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.Equal(1, veri.GetArrayLength());
            Assert.Equal(belgeId, veri[0].GetProperty("id").GetInt32());
        }

        [Fact]
        public async Task BaskaAracinBakimKaydinaBaglanamaz()
        {
            var (client, aracId) = await HazirlaAsync("baglayanlis", "34BO1006");
            var belgeId = await BelgeYukleAsync(client, aracId, "fatura.pdf", PdfIcerik, "application/pdf");

            var (yabanci, yabanciArac) = await HazirlaAsync("baglayab", "34BO1007");
            var yabanciBakim = await BakimEkleAsync(yabanci, yabanciArac);

            var cevap = await client.PutAsJsonAsync($"/api/Documents/{belgeId}/bagla", new { maintenanceRecordId = yabanciBakim });

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }

        [Fact]
        public async Task BagIptalEdilebilir()
        {
            var (client, aracId) = await HazirlaAsync("bagiptal", "34BO1008");
            var belgeId = await BelgeYukleAsync(client, aracId, "fatura.pdf", PdfIcerik, "application/pdf");
            var bakimId = await BakimEkleAsync(client, aracId);

            await client.PutAsJsonAsync($"/api/Documents/{belgeId}/bagla", new { maintenanceRecordId = bakimId });
            var iptal = await client.PutAsJsonAsync($"/api/Documents/{belgeId}/bagla", new { maintenanceRecordId = (int?)null });

            Assert.True(iptal.IsSuccessStatusCode, await iptal.Content.ReadAsStringAsync());

            var liste = await client.GetAsync($"/api/Documents?maintenanceRecordId={bakimId}");
            var veri = JsonDocument.Parse(await liste.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.Equal(0, veri.GetArrayLength());
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
