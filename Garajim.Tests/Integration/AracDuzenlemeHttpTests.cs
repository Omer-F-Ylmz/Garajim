using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Business.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class AracDuzenlemeHttpTests : IDisposable
    {
        private sealed class DuzenlemeFactory : GarajimWebApplicationFactory
        {
            public SahteDegerTahminEdici Tahminci { get; } = new SahteDegerTahminEdici();

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);

                builder.ConfigureServices(services =>
                {
                    foreach (var kayit in services.Where(d => d.ServiceType == typeof(IDegerTahminEdici)).ToList())
                    {
                        services.Remove(kayit);
                    }

                    services.AddSingleton<IDegerTahminEdici>(Tahminci);
                });
            }
        }

        private readonly DuzenlemeFactory _factory;

        public AracDuzenlemeHttpTests()
        {
            _factory = new DuzenlemeFactory();
        }

        public void Dispose()
        {
            _factory.Dispose();
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static string Plaka() => "34DZ" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("duzenle"), fullName = "Düzenleme Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, cevap);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        private static async Task<int> TicariAracEkleAsync(HttpClient client)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = Plaka(),
                brand = "Ford",
                model = "Focus",
                year = 2020,
                currentKm = 70000,
                fuelType = "Dizel",
                kullanimTuru = "Ticari",
                vites = "Otomatik",
                kasaTipi = "Sedan"
            });
            return (await VeriAsync(cevap)).GetProperty("id").GetInt32();
        }

        [Fact]
        public async Task TicariAracKullanimTurusuzPuttaTicariKalir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await TicariAracEkleAsync(sahip);

            Assert.Equal("Ticari", (await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}"))).GetProperty("kullanimTuru").GetString());

            var guncelle = await sahip.PutAsJsonAsync($"/api/Vehicles/{aracId}", new
            {
                brand = "Ford",
                model = "Focus",
                year = 2020,
                currentKm = 71500,
                fuelType = "Dizel"
            });

            Assert.Equal(HttpStatusCode.OK, guncelle.StatusCode);

            var arac = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}"));

            Assert.Equal("Ticari", arac.GetProperty("kullanimTuru").GetString());
            Assert.Equal(71500, arac.GetProperty("currentKm").GetInt32());
        }

        [Fact]
        public async Task KullanimTuruAcikcaGonderilirseDegisir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await TicariAracEkleAsync(sahip);

            await sahip.PutAsJsonAsync($"/api/Vehicles/{aracId}", new
            {
                brand = "Ford",
                model = "Focus",
                year = 2020,
                currentKm = 70000,
                fuelType = "Dizel",
                kullanimTuru = "Hususi"
            });

            var arac = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}"));

            Assert.Equal("Hususi", arac.GetProperty("kullanimTuru").GetString());
        }

        [Fact]
        public async Task PutVitesKasaMotorVeAcilAlanlariniTasir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await TicariAracEkleAsync(sahip);

            var guncelle = await sahip.PutAsJsonAsync($"/api/Vehicles/{aracId}", new
            {
                brand = "Ford",
                model = "Focus",
                year = 2021,
                currentKm = 72000,
                fuelType = "Dizel",
                kullanimTuru = "Ticari",
                vites = "Düz",
                kasaTipi = "StationWagon",
                motor = "1.5 EcoBlue",
                ilkTescilTarihi = "2021-03-15",
                acilKisiAd = "Ayşe Yılmaz",
                acilKisiTelefon = "0555 000 11 22",
                acilNot = "Kan grubu 0 Rh+"
            });

            Assert.Equal(HttpStatusCode.OK, guncelle.StatusCode);

            var arac = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}"));

            Assert.Equal("Düz", arac.GetProperty("vites").GetString());
            Assert.Equal("StationWagon", arac.GetProperty("kasaTipi").GetString());
            Assert.Equal("1.5 EcoBlue", arac.GetProperty("motor").GetString());
            Assert.Equal("Ayşe Yılmaz", arac.GetProperty("acilKisiAd").GetString());
            Assert.Equal("0555 000 11 22", arac.GetProperty("acilKisiTelefon").GetString());
            Assert.Equal("Kan grubu 0 Rh+", arac.GetProperty("acilNot").GetString());
            Assert.StartsWith("2021-03-15", arac.GetProperty("ilkTescilTarihi").GetString());
            Assert.Equal(2021, arac.GetProperty("year").GetInt32());
        }

        [Fact]
        public async Task VitesGonderilmezseMevcutDegerKorunur()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await TicariAracEkleAsync(sahip);

            await sahip.PutAsJsonAsync($"/api/Vehicles/{aracId}", new
            {
                brand = "Ford",
                model = "Focus",
                year = 2020,
                currentKm = 70000,
                fuelType = "Dizel"
            });

            var arac = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}"));

            Assert.Equal("Otomatik", arac.GetProperty("vites").GetString());
            Assert.Equal("Sedan", arac.GetProperty("kasaTipi").GetString());
        }

        [Fact]
        public async Task SurucuAraciDuzenleyemez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await TicariAracEkleAsync(sahip);

            var eposta = Eposta("duzenledriver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Sürücü", role = "Driver" });
            var veri = await VeriAsync(ekle);

            var surucu = _factory.CreateClient();
            var giris = await surucu.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = (await VeriAsync(giris)).GetProperty("token").GetString();
            surucu.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var cevap = await surucu.PutAsJsonAsync($"/api/Vehicles/{aracId}", new
            {
                brand = "Ford",
                model = "Focus",
                year = 2020,
                currentKm = 70000,
                fuelType = "Dizel"
            });

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }

        [Fact]
        public async Task VitesYokkenTahmin422DonerVeModelCagrilmaz()
        {
            var sahip = await SahipOlusturAsync();

            var cevap = await sahip.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = Plaka(),
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 90000,
                fuelType = "Benzin",
                kasaTipi = "Hatchback5"
            });
            var aracId = (await VeriAsync(cevap)).GetProperty("id").GetInt32();

            var oncekiCagri = _factory.Tahminci.CagriSayisi;
            var tahmin = await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);
            var govde = await tahmin.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.UnprocessableEntity, tahmin.StatusCode);
            Assert.Contains("vites", govde, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(oncekiCagri, _factory.Tahminci.CagriSayisi);

            var seri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/deger"));
            Assert.Equal(0, seri.GetProperty("kayitlar").GetArrayLength());
        }

        [Fact]
        public async Task VitesSecilinceModeleGercekDegerGider()
        {
            var sahip = await SahipOlusturAsync();

            var cevap = await sahip.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = Plaka(),
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 90000,
                fuelType = "Benzin",
                kasaTipi = "Hatchback5",
                vites = "Yarı Otomatik"
            });
            var aracId = (await VeriAsync(cevap)).GetProperty("id").GetInt32();

            var tahmin = await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);

            Assert.Equal(HttpStatusCode.OK, tahmin.StatusCode);
            Assert.Equal("Yarı Otomatik", _factory.Tahminci.SonVitesTipi);
        }

        [Fact]
        public async Task DuzenlemeSonrasiVitesTahmineAcilir()
        {
            var sahip = await SahipOlusturAsync();

            var cevap = await sahip.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = Plaka(),
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 90000,
                fuelType = "Benzin",
                kasaTipi = "Hatchback5"
            });
            var aracId = (await VeriAsync(cevap)).GetProperty("id").GetInt32();

            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null)).StatusCode);

            await sahip.PutAsJsonAsync($"/api/Vehicles/{aracId}", new
            {
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 90000,
                fuelType = "Benzin",
                vites = "Otomatik"
            });

            var sonra = await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);

            Assert.Equal(HttpStatusCode.OK, sonra.StatusCode);
            Assert.Equal("Otomatik", _factory.Tahminci.SonVitesTipi);
        }
    }
}
