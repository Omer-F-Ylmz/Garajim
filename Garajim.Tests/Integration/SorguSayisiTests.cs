using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Dal.Abstract;
using Garajim.Dal.Concrete.Context;
using Garajim.Entity.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class SorguSayisiTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public SorguSayisiTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task HasarFotoSayilariTekSorguylaOkunur()
        {
            using var kapsam = _factory.Services.CreateScope();
            var dal = kapsam.ServiceProvider.GetRequiredService<IHasarFotoDal>();

            var sayilar = await dal.SayilarAsync(new List<int> { 1, 2, 3 });

            Assert.NotNull(sayilar);
        }

        [Fact]
        public async Task BosListeVeritabaninaGitmez()
        {
            using var kapsam = _factory.Services.CreateScope();
            var dal = kapsam.ServiceProvider.GetRequiredService<IHasarFotoDal>();

            Assert.Empty(await dal.SayilarAsync(new List<int>()));
            Assert.Empty(await dal.SayilarAsync(null));
        }

        [Fact]
        public async Task HasarListesiFotoSayisiniDogruDoner()
        {
            var client = _factory.CreateClient();
            await TestKayit.GirisYapAsync(client, "hasarsayi");

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = "34HS" + Random.Shared.Next(1000, 9999),
                brand = "Fiat",
                model = "Egea",
                year = 2019,
                currentKm = 50000,
                fuelType = "Dizel"
            });

            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            for (var i = 0; i < 3; i++)
            {
                var olustur = await client.PostAsJsonAsync("/api/Hasar", new
                {
                    vehicleId = aracId,
                    olayTarihi = DateTime.UtcNow.Date.AddDays(-i - 1),
                    tur = "Kaza",
                    tutanakTuru = "Yok",
                    aciklama = "Test hasar " + i,
                    konum = "On tampon"
                });

                Assert.True(olustur.IsSuccessStatusCode, await olustur.Content.ReadAsStringAsync());
            }

            var liste = await client.GetAsync("/api/Hasar");
            var veri = JsonDocument.Parse(await liste.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.Equal(3, veri.GetArrayLength());

            foreach (var dosya in veri.EnumerateArray())
            {
                Assert.Equal(0, dosya.GetProperty("fotoSayisi").GetInt32());
            }
        }

        [Fact]
        public async Task BakimParcalariTekSaveIleYazilir()
        {
            using var kapsam = _factory.Services.CreateScope();
            var dal = kapsam.ServiceProvider.GetRequiredService<IMaintenancePartDal>();
            var baglam = kapsam.ServiceProvider.GetRequiredService<GarajimDbContext>();

            var oncekiSayi = await baglam.MaintenanceParts.IgnoreQueryFilters().CountAsync();

            await dal.TopluEkleAsync(new List<MaintenancePart>());

            Assert.Equal(oncekiSayi, await baglam.MaintenanceParts.IgnoreQueryFilters().CountAsync());
        }
    }
}
