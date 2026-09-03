using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class UygunsuzIfadeHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public UygunsuzIfadeHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Filtre Sahibi", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        private static async Task<int> AracEkleAsync(HttpClient client)
        {
            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Fiat",
                model = "Egea",
                year = 2019,
                currentKm = 40000,
                fuelType = "Benzin"
            });

            var govde = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync());
            return govde.RootElement.GetProperty("data").GetProperty("id").GetInt32();
        }

        [Fact]
        public async Task BakimServisAdiUygunsuzsaReddedilir()
        {
            var client = await SahipAsync("servis");
            var aracId = await AracEkleAsync(client);

            var cevap = await client.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                date = DateTime.UtcNow.Date,
                km = 41000,
                cost = 1500m,
                serviceName = "Orospu Oto",
                note = "Yağ değişimi"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
            Assert.Contains("uygunsuz", await cevap.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task BakimNotuUygunsuzsaReddedilir()
        {
            var client = await SahipAsync("bakimnot");
            var aracId = await AracEkleAsync(client);

            var cevap = await client.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                date = DateTime.UtcNow.Date,
                km = 41000,
                cost = 1500m,
                serviceName = "Şişli Oto Servis",
                note = "usta piç çıktı"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task ParcaAciklamasiUygunsuzsaReddedilir()
        {
            var client = await SahipAsync("parca");
            var aracId = await AracEkleAsync(client);

            var cevap = await client.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                date = DateTime.UtcNow.Date,
                km = 41000,
                cost = 1500m,
                serviceName = "Kartal Sanayi",
                note = "Yağ değişimi",
                parcalar = new[] { new { parcaTuru = "YagFiltresi", aciklama = "amk filtre", adet = 1, tutar = 300m, marka = "Bosch" } }
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task HasarAciklamasiUygunsuzsaReddedilir()
        {
            var client = await SahipAsync("hasar");
            var aracId = await AracEkleAsync(client);

            var cevap = await client.PostAsJsonAsync("/api/Hasar", new
            {
                vehicleId = aracId,
                olayTarihi = DateTime.UtcNow.Date,
                tur = "Kaza",
                konum = "Kartal",
                aciklama = "karşı taraf şerefsiz",
                tutanakTuru = "Yok"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task EvrakNotuUygunsuzsaReddedilir()
        {
            var client = await SahipAsync("evrak");
            var aracId = await AracEkleAsync(client);

            var cevap = await client.PostAsJsonAsync("/api/Evrak", new
            {
                vehicleId = aracId,
                evrakTuru = "Sigorta",
                baslangicTarihi = DateTime.UtcNow.Date,
                bitisTarihi = DateTime.UtcNow.Date.AddYears(1),
                saglayici = "Şişli Sigorta",
                not = "yarrak acente"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task SirketAdiUygunsuzsaKayitReddedilir()
        {
            var client = _factory.CreateClient();

            var cevap = await client.PostAsJsonAsync("/api/Auth/register", new
            {
                email = Eposta("sirket"),
                fullName = "Deneme Kullanıcı",
                password = "Test1234!",
                companyName = "Orospu Lojistik"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task ListedeYokModelMetniUygunsuzsaReddedilir()
        {
            var client = await SahipAsync("model");

            var cevap = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = TestPlaka.Uret(),
                brand = "Fiat",
                model = "daşak arabası",
                listedeYok = true,
                year = 2019,
                currentKm = 40000,
                fuelType = "Benzin"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
            Assert.Contains("uygunsuz", await cevap.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task TemizMetinlerKabulEdilir()
        {
            var client = await SahipAsync("temiz");
            var aracId = await AracEkleAsync(client);

            var cevap = await client.PostAsJsonAsync("/api/Maintenance", new
            {
                vehicleId = aracId,
                type = "PeriyodikBakim",
                date = DateTime.UtcNow.Date,
                km = 41000,
                cost = 1500m,
                serviceName = "Şişli Kartal Oto",
                note = "Balata ve yağ filtresi değişimi",
                parcalar = new[] { new { parcaTuru = "YagFiltresi", aciklama = "Sikke Sokak tedarikçisi", adet = 1, tutar = 300m, marka = "Bosch" } }
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        }
    }
}
