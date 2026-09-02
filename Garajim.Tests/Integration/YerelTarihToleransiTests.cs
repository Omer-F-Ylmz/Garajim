using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class YerelTarihToleransiTests : IDisposable
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
                new { email = Eposta("yereltarih"), fullName = "Yerel Tarih", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var arac = await client.PostAsJsonAsync("/api/Vehicles", new
            {
                plate = "34YT" + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpperInvariant(),
                brand = "Renault", model = "Clio", year = 2019, currentKm = 90000,
                fuelType = "Benzin", vites = "Otomatik", kasaTipi = "Hatchback5"
            });

            var aracId = JsonDocument.Parse(await arac.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("id").GetInt32();

            return (client, aracId);
        }

        [Fact]
        public async Task SaatFarkiKadarIleriTarihliHasarKabulEdilir()
        {
            var (client, aracId) = await SahipVeAracAsync();
            var yarin = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

            var cevap = await client.PostAsJsonAsync("/api/Hasar", new
            {
                vehicleId = aracId,
                olayTarihi = yarin,
                tur = "Kaza",
                aciklama = "Gece kazası, yerel tarih UTC'den bir gün ileride.",
                tutanakTuru = "Yok"
            });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        }

        [Fact]
        public async Task IkiGunIleriTarihliHasarReddedilir()
        {
            var (client, aracId) = await SahipVeAracAsync();
            var ikiGunSonra = DateTime.UtcNow.Date.AddDays(2).ToString("yyyy-MM-dd");

            var cevap = await client.PostAsJsonAsync("/api/Hasar", new
            {
                vehicleId = aracId,
                olayTarihi = ikiGunSonra,
                tur = "Kaza",
                aciklama = "Gerçekten gelecek tarih.",
                tutanakTuru = "Yok"
            });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }

        [Fact]
        public async Task SaatFarkiKadarIleriTarihliDegerKabulEdilir()
        {
            var (client, aracId) = await SahipVeAracAsync();
            var yarin = DateTime.UtcNow.Date.AddDays(1).ToString("yyyy-MM-dd");

            var cevap = await client.PostAsJsonAsync($"/api/Vehicles/{aracId}/deger",
                new { tarih = yarin, deger = 500000.0, kaynak = "Beyan" });

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
        }

        [Fact]
        public async Task IkiGunIleriTarihliDegerReddedilir()
        {
            var (client, aracId) = await SahipVeAracAsync();
            var ikiGunSonra = DateTime.UtcNow.Date.AddDays(2).ToString("yyyy-MM-dd");

            var cevap = await client.PostAsJsonAsync($"/api/Vehicles/{aracId}/deger",
                new { tarih = ikiGunSonra, deger = 500000.0, kaynak = "Beyan" });

            Assert.Equal(HttpStatusCode.BadRequest, cevap.StatusCode);
        }
    }
}
