using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Dal.Concrete.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class AracSilmeKaskadTests : IDisposable
    {
        private static readonly byte[] PngBaslik = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private sealed class DepoFactory : GarajimWebApplicationFactory
        {
            private readonly string _klasor;

            public DepoFactory(string klasor)
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
        private readonly DepoFactory _factory;

        public AracSilmeKaskadTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-aracsil-" + Guid.NewGuid().ToString("N"));
            _factory = new DepoFactory(_klasor);
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

        private (int Belge, int Yakit) Sayimlar()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();

            return (
                context.Documents.IgnoreQueryFilters().Count(),
                context.FuelRecords.IgnoreQueryFilters().Count());
        }

        [Fact]
        public async Task AracSilinincaBelgeSatirlariVeDosyalariGider()
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("aracsil"), fullName = "Silme Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(), brand = "Fiat", model = "Egea", year = 2020,
                currentKm = 40000, fuelType = "Dizel", vites = "Manuel", kasaTipi = "Sedan"
            });
            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            await client.PostAsJsonAsync("/api/Fuel", new
            {
                vehicleId = aracId, date = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                km = 40100, liters = 40m, totalCost = 2000m
            });

            var icerik = new byte[256];
            Array.Copy(PngBaslik, icerik, PngBaslik.Length);

            using (var form = new MultipartFormDataContent())
            {
                var dosya = new ByteArrayContent(icerik);
                dosya.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                form.Add(dosya, "file", "ruhsat.png");
                form.Add(new StringContent(aracId.ToString()), "vehicleId");

                var yukle = await client.PostAsync("/api/Documents", form);
                Assert.True(yukle.IsSuccessStatusCode, await yukle.Content.ReadAsStringAsync());
            }

            Assert.Equal((1, 1), Sayimlar());
            Assert.Single(Directory.GetFiles(_klasor));

            var sil = await client.DeleteAsync($"/api/Vehicles/{aracId}");
            Assert.True(sil.IsSuccessStatusCode, await sil.Content.ReadAsStringAsync());

            Assert.Equal((0, 0), Sayimlar());
            Assert.Empty(Directory.GetFiles(_klasor));
        }

        [Fact]
        public async Task YabanciAracSilinemez()
        {
            var birinci = _factory.CreateClient();
            var kayitA = await birinci.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("sahipA"), fullName = "Sahip A", password = "Test1234!" });
            birinci.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await TestKayit.TokenAl(birinci, kayitA));

            var arac = await birinci.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(), brand = "Fiat", model = "Egea", year = 2020,
                currentKm = 40000, fuelType = "Dizel", vites = "Manuel", kasaTipi = "Sedan"
            });
            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var ikinci = _factory.CreateClient();
            var kayitB = await ikinci.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("sahipB"), fullName = "Sahip B", password = "Test1234!" });
            ikinci.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await TestKayit.TokenAl(ikinci, kayitB));

            var cevap = await ikinci.DeleteAsync($"/api/Vehicles/{aracId}");

            Assert.Equal(HttpStatusCode.NotFound, cevap.StatusCode);
        }
    }
}
