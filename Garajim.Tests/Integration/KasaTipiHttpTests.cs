using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Garajim.Business.Abstract;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Garajim.Tests.Integration
{
    public class KasaTipiHttpTests : IDisposable
    {
        private sealed class KasaFactory : GarajimWebApplicationFactory
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

        private readonly KasaFactory _factory;

        public KasaTipiHttpTests()
        {
            _factory = new KasaFactory();
        }

        public void Dispose()
        {
            _factory.Dispose();
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private static string Plaka() => "34KT" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant();

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("kasa"), fullName = "Kasa Sahibi", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<HttpResponseMessage> AracEkleAsync(HttpClient client, object kasaTipi)
        {
            return await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = Plaka(),
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 90000,
                fuelType = "Benzin",
                vites = "Otomatik",
                kasaTipi
            });
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task KasaTipiKaydedilirVeGeriDoner()
        {
            var sahip = await SahipOlusturAsync();

            var cevap = await AracEkleAsync(sahip, "Suv");
            var arac = await VeriAsync(cevap);

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal("Suv", arac.GetProperty("kasaTipi").GetString());

            var liste = await VeriAsync(await sahip.GetAsync("/api/Vehicles"));
            Assert.Equal("Suv", liste[0].GetProperty("kasaTipi").GetString());
        }

        [Fact]
        public async Task KasaTipiZorunluDegildir()
        {
            var sahip = await SahipOlusturAsync();

            var cevap = await AracEkleAsync(sahip, null);
            var arac = await VeriAsync(cevap);

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal(JsonValueKind.Null, arac.GetProperty("kasaTipi").ValueKind);
        }

        [Fact]
        public async Task TanimsizKasaTipiKaydedilmez()
        {
            var sahip = await SahipOlusturAsync();

            var metinle = await AracEkleAsync(sahip, "Traktor");
            Assert.Equal(HttpStatusCode.BadRequest, metinle.StatusCode);

            var sayiyla = await AracEkleAsync(sahip, 99);
            var arac = await VeriAsync(sayiyla);

            Assert.Equal(HttpStatusCode.OK, sayiyla.StatusCode);
            Assert.Equal(JsonValueKind.Null, arac.GetProperty("kasaTipi").ValueKind);

            var tahmin = await sahip.PostAsync("/api/Vehicles/" + arac.GetProperty("id").GetInt32() + "/deger/tahmin", null);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, tahmin.StatusCode);
        }

        [Fact]
        public async Task KasaTipiYokkenTahmin422DonerVeModelCagrilmaz()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = (await VeriAsync(await AracEkleAsync(sahip, null))).GetProperty("id").GetInt32();

            var oncekiCagri = _factory.Tahminci.CagriSayisi;
            var cevap = await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.UnprocessableEntity, cevap.StatusCode);
            Assert.Contains("kasa tipi", govde, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(oncekiCagri, _factory.Tahminci.CagriSayisi);

            var seri = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}/deger"));
            Assert.Equal(0, seri.GetProperty("kayitlar").GetArrayLength());
        }

        [Fact]
        public async Task KasaTipiSecilinceModeleGercekDegerGider()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = (await VeriAsync(await AracEkleAsync(sahip, "Hatchback5"))).GetProperty("id").GetInt32();

            var cevap = await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal("Hatchback/5", _factory.Tahminci.SonKasaTipi);
        }

        [Fact]
        public async Task DarUcMevcutAracaKasaTipiAtarVeTahminiAcar()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = (await VeriAsync(await AracEkleAsync(sahip, null))).GetProperty("id").GetInt32();

            var once = await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, once.StatusCode);

            var sec = await sahip.PutAsJsonAsync($"/api/Vehicles/{aracId}/kasa-tipi", "StationWagon");
            Assert.Equal(HttpStatusCode.OK, sec.StatusCode);

            var arac = await VeriAsync(await sahip.GetAsync($"/api/Vehicles/{aracId}"));
            Assert.Equal("StationWagon", arac.GetProperty("kasaTipi").GetString());

            var sonra = await sahip.PostAsync($"/api/Vehicles/{aracId}/deger/tahmin", null);
            Assert.Equal(HttpStatusCode.OK, sonra.StatusCode);
            Assert.Equal("Station wagon", _factory.Tahminci.SonKasaTipi);
        }

        [Fact]
        public async Task DarUcTanimsizDegeriReddederBaskaSirketiGormez()
        {
            var birinci = await SahipOlusturAsync();
            var aracId = (await VeriAsync(await AracEkleAsync(birinci, null))).GetProperty("id").GetInt32();

            var gecersiz = await birinci.PutAsJsonAsync($"/api/Vehicles/{aracId}/kasa-tipi", "Traktor");

            var ikinci = await SahipOlusturAsync();
            var yabanci = await ikinci.PutAsJsonAsync($"/api/Vehicles/{aracId}/kasa-tipi", "Sedan");

            Assert.Equal(HttpStatusCode.BadRequest, gecersiz.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, yabanci.StatusCode);
        }
    }
}
