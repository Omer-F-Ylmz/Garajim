using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Business.Constants;
using Garajim.Entity.Concrete;
using Microsoft.Extensions.DependencyInjection;
using Garajim.Dal.Concrete.Context;

namespace Garajim.Tests.Integration
{
    public class ExportSiniriHttpTests : IDisposable
    {
        private readonly GarajimWebApplicationFactory _factory = new GarajimWebApplicationFactory();

        public void Dispose()
        {
            _factory.Dispose();
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<(HttpClient Client, int AracId)> SahipVeAracAsync()
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta("exportsinir"), fullName = "Export Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = "34EX" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant(),
                brand = "Renault", model = "Clio", year = 2019, currentKm = 90000,
                fuelType = "Benzin", vites = "Otomatik", kasaTipi = "Hatchback5"
            });

            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            return (client, aracId);
        }

        [Fact]
        public async Task YakitDisaAktarimiUstSinirdaDurur()
        {
            var (client, aracId) = await SahipVeAracAsync();
            var fazla = QueryLimits.MaxListSize + 25;

            using (var kapsam = _factory.Services.CreateScope())
            {
                var db = kapsam.ServiceProvider.GetRequiredService<GarajimDbContext>();
                var arac = await db.Vehicles.IgnoreQueryFiltersAsNoTrackingSingle(aracId);

                for (var i = 0; i < fazla; i++)
                {
                    db.FuelRecords.Add(new FuelRecord
                    {
                        CompanyId = arac.CompanyId,
                        VehicleId = aracId,
                        Date = new DateTime(2026, 1, 1).AddMinutes(i),
                        Km = 90000 + i,
                        Liters = 10m,
                        TotalCost = 500m
                    });
                }

                await db.SaveChangesAsync();
            }

            var cevap = await client.GetAsync("/api/Export/yakit.csv");
            var csv = await cevap.Content.ReadAsStringAsync();
            var satir = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length - 1;

            Assert.True(satir <= QueryLimits.MaxListSize,
                $"Dışa aktarım {QueryLimits.MaxListSize} sınırını aştı: {satir} satır.");
        }

        [Fact]
        public async Task SinirAltindakiKayitlarTamGelir()
        {
            var (client, aracId) = await SahipVeAracAsync();

            for (var i = 0; i < 3; i++)
            {
                await client.PostAsJsonAsync("/api/Fuel", new
                {
                    vehicleId = aracId,
                    date = new DateTime(2026, 3, 1).AddDays(i).ToString("yyyy-MM-dd"),
                    km = 91000 + i * 100,
                    liters = 40.0,
                    totalCost = 2000.0
                });
            }

            var csv = await client.GetStringAsync("/api/Export/yakit.csv");
            var satir = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length - 1;

            Assert.Equal(3, satir);
        }
    }

    internal static class ExportTestYardimcisi
    {
        public static async Task<Vehicle> IgnoreQueryFiltersAsNoTrackingSingle(
            this Microsoft.EntityFrameworkCore.DbSet<Vehicle> set, int id)
        {
            return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync(
                Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(
                    Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.IgnoreQueryFilters(set)),
                v => v.Id == id);
        }
    }
}
