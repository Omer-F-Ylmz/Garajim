using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Business.Jobs;
using Garajim.Dal.Concrete.Context;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class HesapSilmeHttpTests : IDisposable
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

        public HesapSilmeHttpTests()
        {
            _klasor = Path.Combine(Path.GetTempPath(), "garajim-hesapsil-" + Guid.NewGuid().ToString("N"));
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

        private async Task<(HttpClient Client, string Eposta, int AracId)> SirketAsync(string on)
        {
            var client = _factory.CreateClient();
            var eposta = Eposta(on);
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = eposta, fullName = "Silinecek Sahip", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(), brand = "Fiat", model = "Egea", year = 2020,
                currentKm = 40000, fuelType = "Dizel", vites = "Manuel", kasaTipi = "Sedan"
            });
            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            return (client, eposta, aracId);
        }

        private async Task BelgeYukleAsync(HttpClient client, int aracId)
        {
            var icerik = new byte[256];
            Array.Copy(PngBaslik, icerik, PngBaslik.Length);

            using var form = new MultipartFormDataContent();
            var dosya = new ByteArrayContent(icerik);
            dosya.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(dosya, "file", "ruhsat.png");
            form.Add(new StringContent(aracId.ToString()), "vehicleId");

            var yukle = await client.PostAsync("/api/Documents", form);
            Assert.True(yukle.IsSuccessStatusCode, await yukle.Content.ReadAsStringAsync());
        }

        private async Task<string> KodAlAsync(HttpClient client, string eposta)
        {
            var cevap = await client.PostAsync("/api/Account/sil-kod", null);
            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            return SahteEpostaGonderici.Ortak.SonKod(eposta);
        }

        private T Oku<T>(Func<GarajimDbContext, T> islem)
        {
            using var scope = _factory.Services.CreateScope();
            return islem(scope.ServiceProvider.GetRequiredService<GarajimDbContext>());
        }

        private async Task JobuCalistirAsync()
        {
            using var scope = _factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<HesapSilmeJob>().RunAsync();
        }

        private void SilmeTarihiniGeriAl(string eposta)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();
            var user = context.Users.IgnoreQueryFilters().Single(u => u.Email == eposta);
            var sirket = context.Companies.IgnoreQueryFilters().Single(c => c.Id == user.CompanyId);
            sirket.SilinmePlanlanan = DateTime.UtcNow.AddDays(-1);
            context.SaveChanges();
        }

        [Fact]
        public async Task KodsuzSilmeIstegiReddedilir()
        {
            var (client, _, _) = await SirketAsync("kodsuz");

            var cevap = await client.PostAsJsonAsync("/api/Account/sil", new { kod = "000000" });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task DogruKodSilmeyiPlanlarVeIptalEdilebilir()
        {
            var (client, eposta, _) = await SirketAsync("plan");
            var kod = await KodAlAsync(client, eposta);

            var sil = await client.PostAsJsonAsync("/api/Account/sil", new { kod });
            Assert.True(sil.IsSuccessStatusCode, await sil.Content.ReadAsStringAsync());

            var planlanan = Oku(c => c.Companies.IgnoreQueryFilters()
                .Single(x => x.Id == c.Users.IgnoreQueryFilters().Single(u => u.Email == eposta).CompanyId)
                .SilinmePlanlanan);
            Assert.NotNull(planlanan);

            var durum = await client.GetAsync("/api/Account/durum");
            using var belge = JsonDocument.Parse(await durum.Content.ReadAsStringAsync());
            Assert.True(belge.RootElement.GetProperty("data").GetProperty("silmePlanlandi").GetBoolean());

            var iptal = await client.PostAsync("/api/Account/sil-iptal", null);
            Assert.True(iptal.IsSuccessStatusCode, await iptal.Content.ReadAsStringAsync());

            Assert.Null(Oku(c => c.Companies.IgnoreQueryFilters()
                .Single(x => x.Id == c.Users.IgnoreQueryFilters().Single(u => u.Email == eposta).CompanyId)
                .SilinmePlanlanan));
        }

        [Fact]
        public async Task SuresiDolanSirketJobIleKaliciSilinir()
        {
            var (client, eposta, aracId) = await SirketAsync("kalici");
            await BelgeYukleAsync(client, aracId);

            var kod = await KodAlAsync(client, eposta);
            await client.PostAsJsonAsync("/api/Account/sil", new { kod });
            SilmeTarihiniGeriAl(eposta);

            Assert.Single(Directory.GetFiles(_klasor));

            await JobuCalistirAsync();

            Assert.False(Oku(c => c.Users.IgnoreQueryFilters().Any(u => u.Email == eposta)));
            Assert.Equal(0, Oku(c => c.Vehicles.IgnoreQueryFilters().Count(v => v.Id == aracId)));
            Assert.Empty(Directory.GetFiles(_klasor));
        }

        [Fact]
        public async Task SuresiDolmayanSirketeDokunulmaz()
        {
            var (client, eposta, _) = await SirketAsync("bekleyen");
            var kod = await KodAlAsync(client, eposta);
            await client.PostAsJsonAsync("/api/Account/sil", new { kod });

            await JobuCalistirAsync();

            Assert.True(Oku(c => c.Users.IgnoreQueryFilters().Any(u => u.Email == eposta)));
        }

        [Fact]
        public async Task YabanciSirketSilinmez()
        {
            var (silinen, silinenEposta, _) = await SirketAsync("silinen");
            var (_, komsuEposta, _) = await SirketAsync("komsu");

            var kod = await KodAlAsync(silinen, silinenEposta);
            await silinen.PostAsJsonAsync("/api/Account/sil", new { kod });
            SilmeTarihiniGeriAl(silinenEposta);

            await JobuCalistirAsync();

            Assert.False(Oku(c => c.Users.IgnoreQueryFilters().Any(u => u.Email == silinenEposta)));
            Assert.True(Oku(c => c.Users.IgnoreQueryFilters().Any(u => u.Email == komsuEposta)));
        }
    }
}
