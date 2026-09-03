using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Garajim.Business.Jobs;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Garajim.Entity.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class KatalogEslemeTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public KatalogEslemeTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int CompanyId, int UserId)> SahipOlusturAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Katalog Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Fiat",
                model = "Egea",
                year = 2019,
                currentKm = 50000,
                fuelType = "Benzin"
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);

            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();
            var sonArac = await context.Vehicles.IgnoreQueryFilters().OrderByDescending(v => v.Id).FirstAsync();

            return (client, sonArac.CompanyId, sonArac.UserId);
        }

        private static Vehicle EskiArac(int companyId, int userId, string marka, string model) => new Vehicle
        {
            CompanyId = companyId,
            UserId = userId,
            Plate = TestPlaka.Uret(),
            Brand = marka,
            Model = model,
            Year = 2015,
            CurrentKm = 100000,
            FuelType = FuelType.Benzin,
            CreatedAt = DateTime.UtcNow
        };

        [Fact]
        public async Task MevcutAraclarKatalogYazimiylaEslesir()
        {
            var (_, companyId, userId) = await SahipOlusturAsync("esleme");

            var ciftler = new[]
            {
                ("fiat", "egea", "Fiat", "Egea", false),
                ("VW", "Golf", "Volkswagen", "Golf", false),
                ("Mercedes", "C", "Mercedes - Benz", "C", false),
                ("TOYOTA", "Corolla", "Toyota", "Corolla", false),
                ("Renault", "Clio 1.5 dCi", "Renault", "Clio", false),
                ("Tofas", "Şahin", "Tofaş", "Şahin", false),
                ("Opel", "Astra", "Opel", "Astra", false),
                ("Hyundai", "i20", "Hyundai", "i20", false),
                ("Ford", "Transit", "Ford", "Transit", true),
                ("Zorlu", "Bilinmeyen", "Zorlu", "Bilinmeyen", true)
            };

            int[] idler;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();
                var araclar = ciftler.Select(c => EskiArac(companyId, userId, c.Item1, c.Item2)).ToList();
                context.Vehicles.AddRange(araclar);
                await context.SaveChangesAsync();
                idler = araclar.Select(a => a.Id).ToArray();
            }

            using (var scope = _factory.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<KatalogEslemeJob>().RunAsync();
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();

                for (var i = 0; i < ciftler.Length; i++)
                {
                    var arac = await context.Vehicles.IgnoreQueryFilters().FirstAsync(v => v.Id == idler[i]);
                    var beklenen = ciftler[i];

                    Assert.Equal(beklenen.Item3, arac.Brand);
                    Assert.Equal(beklenen.Item4, arac.Model);
                    Assert.Equal(beklenen.Item5, arac.ModelEslesmedi);
                }
            }
        }

        [Fact]
        public async Task EslemeIkinciCalismadaDegerDegistirmez()
        {
            var (_, companyId, userId) = await SahipOlusturAsync("yineleme");

            int aracId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();
                var arac = EskiArac(companyId, userId, "vw", "Passat");
                context.Vehicles.Add(arac);
                await context.SaveChangesAsync();
                aracId = arac.Id;
            }

            using (var scope = _factory.Services.CreateScope())
            {
                await scope.ServiceProvider.GetRequiredService<KatalogEslemeJob>().RunAsync();
            }

            DateTime? ilkGuncelleme;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();
                var arac = await context.Vehicles.IgnoreQueryFilters().FirstAsync(v => v.Id == aracId);
                Assert.Equal("Volkswagen", arac.Brand);
                Assert.False(arac.ModelEslesmedi);
                ilkGuncelleme = arac.SonKmGuncelleme;
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var sonuc = await scope.ServiceProvider.GetRequiredService<KatalogEslemeJob>().RunAsync();
                Assert.Equal(0, sonuc);
            }

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();
                var arac = await context.Vehicles.IgnoreQueryFilters().FirstAsync(v => v.Id == aracId);
                Assert.Equal("Volkswagen", arac.Brand);
                Assert.Equal("Passat", arac.Model);
                Assert.Equal(ilkGuncelleme, arac.SonKmGuncelleme);
            }
        }

        [Fact]
        public async Task EslesmeyenAracaDegerTahminiVerilmez()
        {
            var (client, companyId, userId) = await SahipOlusturAsync("tahmin");

            int aracId;

            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<GarajimDbContext>();
                var arac = EskiArac(companyId, userId, "Ford", "Transit");
                arac.KasaTipi = KasaTipi.Hatchback5;
                arac.ModelEslesmedi = true;
                context.Vehicles.Add(arac);
                await context.SaveChangesAsync();
                aracId = arac.Id;
            }

            var cevap = await client.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.UnprocessableEntity, cevap.StatusCode);
            Assert.Contains("katalog", govde, StringComparison.OrdinalIgnoreCase);
        }
    }
}
