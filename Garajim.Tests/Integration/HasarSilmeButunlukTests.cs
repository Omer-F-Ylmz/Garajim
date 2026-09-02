using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Business.Abstract;
using Microsoft.Extensions.Configuration;
using Garajim.Core.Utilities.Results;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class HasarSilmeButunlukTests : IDisposable
    {
        private static readonly byte[] PngBaslik = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private sealed class PatlayanBelgeServisi : IDocumentService
        {
            private readonly IDocumentService _ic;

            public PatlayanBelgeServisi(IDocumentService ic)
            {
                _ic = ic;
            }

            public static int PatlamaSirasi { get; set; }

            public static int SilmeSayaci { get; set; }

            public Task<IDataResult<DocumentDto>> UploadAsync(int userId, DocumentUploadDto dto) => _ic.UploadAsync(userId, dto);

            public Task<IDataResult<List<DocumentDto>>> GetListAsync(int userId, int? vehicleId, int? maintenanceRecordId)
                => _ic.GetListAsync(userId, vehicleId, maintenanceRecordId);

            public Task<IDataResult<DocumentContentDto>> DownloadAsync(int userId, int documentId) => _ic.DownloadAsync(userId, documentId);

            public Task<IResult> DeleteAsync(int userId, int documentId)
            {
                Say();
                return _ic.DeleteAsync(userId, documentId);
            }

            public Task<IDataResult<string>> SatirSilAsync(int userId, int documentId)
            {
                Say();
                return _ic.SatirSilAsync(userId, documentId);
            }

            public void DosyaSil(string saklananAd) => _ic.DosyaSil(saklananAd);

            private static void Say()
            {
                SilmeSayaci++;

                if (PatlamaSirasi > 0 && SilmeSayaci == PatlamaSirasi)
                {
                    throw new IOException("Belge deposu yanıt vermedi.");
                }
            }
        }

        private sealed class PatlayanFactory : GarajimWebApplicationFactory
        {
            private readonly string _klasor;

            public PatlayanFactory(string klasor)
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

                builder.ConfigureServices(services =>
                {
                    var kayit = services.Single(d => d.ServiceType == typeof(IDocumentService));
                    services.Remove(kayit);

                    services.Add(new ServiceDescriptor(
                        typeof(IDocumentService),
                        provider => new PatlayanBelgeServisi((IDocumentService)ActivatorUtilities.CreateInstance(provider, kayit.ImplementationType)),
                        kayit.Lifetime));
                });
            }
        }

        private readonly string _klasor;
        private readonly PatlayanFactory _factory;

        public HasarSilmeButunlukTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-hasarsil-" + Guid.NewGuid().ToString("N"));
            _factory = new PatlayanFactory(_klasor);
            PatlayanBelgeServisi.PatlamaSirasi = 0;
            PatlayanBelgeServisi.SilmeSayaci = 0;
        }

        public void Dispose()
        {
            PatlayanBelgeServisi.PatlamaSirasi = 0;
            PatlayanBelgeServisi.SilmeSayaci = 0;
            _factory.Dispose();

            if (Directory.Exists(_klasor))
            {
                Directory.Delete(_klasor, true);
            }
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int DosyaId)> IkiFotografliDosyaAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Hasar Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = "34HS" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant(),
                brand = "Fiat", model = "Egea", year = 2020, currentKm = 50000,
                fuelType = "Dizel", vites = "Manuel", kasaTipi = "Sedan"
            });
            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            var dosya = await client.PostAsJsonAsync("/api/Hasar", new
            {
                vehicleId = aracId,
                olayTarihi = DateTime.UtcNow.Date.ToString("yyyy-MM-dd"),
                tur = "Kaza",
                aciklama = "Silme bütünlüğü testi.",
                tutanakTuru = "Anlasmali"
            });
            var dosyaId = JsonDocument.Parse(await dosya.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            for (var i = 0; i < 2; i++)
            {
                var icerik = new byte[256];
                Array.Copy(PngBaslik, icerik, PngBaslik.Length);

                using var form = new MultipartFormDataContent();
                var foto = new ByteArrayContent(icerik);
                foto.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                form.Add(foto, "file", $"hasar-{i}.png");
                form.Add(new StringContent("Genel"), "etiket");

                var yukle = await client.PostAsync($"/api/Hasar/{dosyaId}/foto", form);
                Assert.True(yukle.IsSuccessStatusCode, await yukle.Content.ReadAsStringAsync());
            }

            return (client, dosyaId);
        }

        private (int Dosya, int Foto, int Belge) Sayimlar()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();

            return (
                context.HasarDosyalari.IgnoreQueryFilters().Count(),
                context.HasarFotograflari.IgnoreQueryFilters().Count(),
                context.Documents.IgnoreQueryFilters().Count());
        }

        [Fact]
        public async Task BelgeSilmeHatasindaHicbirSatirKaybolmaz()
        {
            var (client, dosyaId) = await IkiFotografliDosyaAsync("hasarsil");
            var once = Sayimlar();

            Assert.Equal((1, 2, 2), once);

            PatlayanBelgeServisi.SilmeSayaci = 0;
            PatlayanBelgeServisi.PatlamaSirasi = 2;

            await Assert.ThrowsAnyAsync<Exception>(() => client.DeleteAsync($"/api/Hasar/{dosyaId}"));

            Assert.Equal(once, Sayimlar());
        }

        [Fact]
        public async Task BasariliSilmeHicbirYetimBelgeBirakmaz()
        {
            var (client, dosyaId) = await IkiFotografliDosyaAsync("hasarsiltemiz");

            Assert.Equal((1, 2, 2), Sayimlar());

            var cevap = await client.DeleteAsync($"/api/Hasar/{dosyaId}");
            Assert.True(cevap.IsSuccessStatusCode, await cevap.Content.ReadAsStringAsync());

            Assert.Equal((0, 0, 0), Sayimlar());
            Assert.Empty(Directory.GetFiles(_klasor));
        }
    }
}
