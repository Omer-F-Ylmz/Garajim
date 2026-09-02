using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class TutanakFotografTests : IDisposable
    {
        private static readonly byte[] PngBaslik = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private sealed class TutanakFactory : GarajimWebApplicationFactory
        {
            private readonly string _klasor;

            public TutanakFactory(string klasor)
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

        private readonly string _klasor;
        private readonly TutanakFactory _factory;

        public TutanakFotografTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-tutanak-" + Guid.NewGuid().ToString("N"));
            _factory = new TutanakFactory(_klasor);
        }

        public void Dispose()
        {
            _factory.Dispose();
            if (Directory.Exists(_klasor))
            {
                Directory.Delete(_klasor, true);
            }
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        [Fact]
        public async Task TutanakFotograflariGomuluGelirVeAyriIstekGerektirmez()
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("tutanak"), fullName = "Tutanak Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = "34TT" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant(),
                brand = "Renault", model = "Clio", year = 2019, currentKm = 90000,
                fuelType = "Benzin", vites = "Otomatik", kasaTipi = "Hatchback5"
            });
            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var dosya = await client.PostAsJsonAsync("/api/Hasar", new
            {
                vehicleId = aracId,
                olayTarihi = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                tur = "Kaza",
                aciklama = "Tutanak fotoğrafı testi.",
                tutanakTuru = "Anlasmali"
            });
            var dosyaId = JsonDocument.Parse(await dosya.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var icerik = new byte[512];
            Array.Copy(PngBaslik, icerik, PngBaslik.Length);

            using (var form = new MultipartFormDataContent())
            {
                var foto = new ByteArrayContent(icerik);
                foto.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                form.Add(foto, "file", "hasar.png");
                form.Add(new StringContent("Genel"), "etiket");

                var yukle = await client.PostAsync($"/api/Hasar/{dosyaId}/foto", form);
                Assert.Equal(HttpStatusCode.OK, yukle.StatusCode);
            }

            var cevap = await client.GetAsync($"/api/Hasar/{dosyaId}/tutanak.html");
            var html = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Contains("data:image/png;base64,", html);
            Assert.DoesNotContain("/api/Documents/", html);
        }

        [Fact]
        public async Task FotografsizTutanakHalaUretilir()
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("tutanakbos"), fullName = "Boş Tutanak", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = "34TB" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant(),
                brand = "Renault", model = "Clio", year = 2019, currentKm = 90000,
                fuelType = "Benzin", vites = "Otomatik", kasaTipi = "Hatchback5"
            });
            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var dosya = await client.PostAsJsonAsync("/api/Hasar", new
            {
                vehicleId = aracId,
                olayTarihi = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                tur = "Cam",
                aciklama = "Fotoğrafsız dosya.",
                tutanakTuru = "Yok"
            });
            var dosyaId = JsonDocument.Parse(await dosya.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var cevap = await client.GetAsync($"/api/Hasar/{dosyaId}/tutanak.html");
            var html = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Contains("Bilgi değişim alanı", html);
            Assert.Contains("fotoğraf eklenmemiş", html);
        }
    }
}
