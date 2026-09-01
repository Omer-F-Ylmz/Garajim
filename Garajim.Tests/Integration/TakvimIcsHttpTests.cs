using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Garajim.Tests.Integration
{
    public class TakvimIcsHttpTests : IDisposable
    {
        private sealed class TakvimFactory : GarajimWebApplicationFactory
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                base.ConfigureWebHost(builder);
                builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["App:BaseUrl"] = "https://ornek.garajim.app"
                }));
            }
        }

        private readonly TakvimFactory _factory = new TakvimFactory();

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipOlusturAsync()
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new { email = Eposta("owner"), fullName = "Takvim Sahibi", password = "Test1234!" });
            var token = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private async Task<(HttpClient Client, int UserId)> SurucuOlusturAsync(HttpClient sahip)
        {
            var eposta = Eposta("driver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Takvim Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var client = _factory.CreateClient();
            var giris = await client.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return (client, veri.GetProperty("userId").GetInt32());
        }

        private static async Task<int> AracEkleAsync(HttpClient client, string plaka)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = plaka,
                brand = "Renault",
                model = "Clio",
                year = 2019,
                currentKm = 120000,
                fuelType = "Benzin"
            });
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        private static async Task<JsonElement> VeriAsync(HttpResponseMessage cevap)
        {
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        private static async Task<string> TokenAlAsync(HttpClient client)
        {
            var veri = await VeriAsync(await client.PostAsync("/api/Takvim/abonelik", null));
            var url = veri.GetProperty("url").GetString();
            var basla = url.LastIndexOf('/') + 1;
            return url.Substring(basla, url.Length - basla - ".ics".Length);
        }

        [Fact]
        public async Task IcsEvrakVeHatirlatmaOlaylariniIcerir()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34ICS001");

            await sahip.PostAsJsonAsync("/api/Evrak", new { vehicleId = aracId, evrakTuru = "Muayene", bitisTarihi = "2027-05-20" });
            await sahip.PostAsJsonAsync("/api/Reminders", new { vehicleId = aracId, type = "Kasko", dueDate = "2027-06-15", note = "kasko yenile" });

            var token = await TokenAlAsync(sahip);

            var anonim = _factory.CreateClient();
            var cevap = await anonim.GetAsync($"/api/takvim/{token}.ics");
            var govde = await cevap.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.Equal("text/calendar", cevap.Content.Headers.ContentType.MediaType);
            Assert.StartsWith("BEGIN:VCALENDAR", govde);
            Assert.Contains("END:VCALENDAR", govde);
            Assert.Contains("34ICS001", govde);
            Assert.Contains("Araç muayenesi", govde);
            Assert.Contains("TRIGGER:-P7D", govde);
            Assert.Equal(2, govde.Split("BEGIN:VEVENT").Length - 1);
        }

        [Fact]
        public async Task TokenBaskaSirketinAracniSizdirmaz()
        {
            var birinci = await SahipOlusturAsync();
            var birinciArac = await AracEkleAsync(birinci, "34ICS002");
            await birinci.PostAsJsonAsync("/api/Evrak", new { vehicleId = birinciArac, evrakTuru = "Kasko", bitisTarihi = "2027-05-20" });

            var ikinci = await SahipOlusturAsync();
            var ikinciArac = await AracEkleAsync(ikinci, "06ICS003");
            await ikinci.PostAsJsonAsync("/api/Evrak", new { vehicleId = ikinciArac, evrakTuru = "Kasko", bitisTarihi = "2027-05-20" });

            var token = await TokenAlAsync(birinci);

            var anonim = _factory.CreateClient();
            var govde = await (await anonim.GetAsync($"/api/takvim/{token}.ics")).Content.ReadAsStringAsync();

            Assert.Contains("34ICS002", govde);
            Assert.DoesNotContain("06ICS003", govde);
        }

        [Fact]
        public async Task DriverYalnizZimmetliAracinOlaylariniGorur()
        {
            var sahip = await SahipOlusturAsync();
            var zimmetli = await AracEkleAsync(sahip, "34ICS004");
            var zimmetsiz = await AracEkleAsync(sahip, "34ICS005");
            var (surucu, surucuId) = await SurucuOlusturAsync(sahip);
            await sahip.PostAsJsonAsync("/api/Assignments", new { vehicleId = zimmetli, userId = surucuId });

            await sahip.PostAsJsonAsync("/api/Evrak", new { vehicleId = zimmetli, evrakTuru = "Kasko", bitisTarihi = "2027-05-20" });
            await sahip.PostAsJsonAsync("/api/Evrak", new { vehicleId = zimmetsiz, evrakTuru = "Kasko", bitisTarihi = "2027-05-20" });

            var token = await TokenAlAsync(surucu);

            var anonim = _factory.CreateClient();
            var govde = await (await anonim.GetAsync($"/api/takvim/{token}.ics")).Content.ReadAsStringAsync();

            Assert.Contains("34ICS004", govde);
            Assert.DoesNotContain("34ICS005", govde);
        }

        [Fact]
        public async Task IptalSonrasiTokenBulunamaz()
        {
            var sahip = await SahipOlusturAsync();
            var token = await TokenAlAsync(sahip);

            var anonim = _factory.CreateClient();
            Assert.Equal(HttpStatusCode.OK, (await anonim.GetAsync($"/api/takvim/{token}.ics")).StatusCode);

            await sahip.DeleteAsync("/api/Takvim/abonelik");

            Assert.Equal(HttpStatusCode.NotFound, (await anonim.GetAsync($"/api/takvim/{token}.ics")).StatusCode);
        }

        [Fact]
        public async Task IkinciAbonelikEskiTokeniGecersizKilar()
        {
            var sahip = await SahipOlusturAsync();
            var ilk = await TokenAlAsync(sahip);
            var ikinci = await TokenAlAsync(sahip);

            var anonim = _factory.CreateClient();

            Assert.NotEqual(ilk, ikinci);
            Assert.Equal(HttpStatusCode.NotFound, (await anonim.GetAsync($"/api/takvim/{ilk}.ics")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await anonim.GetAsync($"/api/takvim/{ikinci}.ics")).StatusCode);
        }

        [Fact]
        public async Task GecersizToken404Doner()
        {
            var anonim = _factory.CreateClient();

            Assert.Equal(HttpStatusCode.NotFound, (await anonim.GetAsync("/api/takvim/olmayan-token.ics")).StatusCode);
        }

        [Fact]
        public async Task UidYenilemedenSonraDegismez()
        {
            var sahip = await SahipOlusturAsync();
            var aracId = await AracEkleAsync(sahip, "34ICS006");
            var evrakId = (await VeriAsync(await sahip.PostAsJsonAsync("/api/Evrak", new
            {
                vehicleId = aracId,
                evrakTuru = "Muayene",
                bitisTarihi = "2027-05-20"
            }))).GetProperty("id").GetInt32();

            var token = await TokenAlAsync(sahip);
            var anonim = _factory.CreateClient();

            var once = await (await anonim.GetAsync($"/api/takvim/{token}.ics")).Content.ReadAsStringAsync();
            Assert.Contains($"UID:evrak-{evrakId}@garajim", once);

            await sahip.PutAsJsonAsync($"/api/Evrak/{evrakId}", new
            {
                evrakTuru = "Muayene",
                bitisTarihi = "2027-08-20"
            });

            var sonra = await (await anonim.GetAsync($"/api/takvim/{token}.ics")).Content.ReadAsStringAsync();

            Assert.Contains($"UID:evrak-{evrakId}@garajim", sonra);
            Assert.Contains("20270820", sonra);
        }

        public void Dispose()
        {
            _factory.Dispose();
        }
    }
}
