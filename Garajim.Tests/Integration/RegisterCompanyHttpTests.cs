using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class RegisterCompanyHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public RegisterCompanyHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<JsonElement> KayitAsync(object govde)
        {
            var client = _factory.CreateClient();
            var cevap = await client.PostAsJsonAsync("/api/Auth/register", govde);
            return JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        }

        [Fact]
        public async Task KayitSirasindaSirketAdiVerilebilir()
        {
            var veri = await KayitAsync(new
            {
                email = Eposta("firma"),
                fullName = "Ömer Yılmaz",
                password = "Test1234!",
                companyName = "Yılmaz Nakliyat"
            });

            Assert.Equal("Yılmaz Nakliyat", veri.GetProperty("companyName").GetString());
        }

        [Fact]
        public async Task SirketAdiBosBirakilirsaAdSoyadKullanilir()
        {
            var veri = await KayitAsync(new
            {
                email = Eposta("bireysel"),
                fullName = "Tekil Kullanıcı",
                password = "Test1234!"
            });

            Assert.Equal("Tekil Kullanıcı", veri.GetProperty("companyName").GetString());
        }

        [Fact]
        public async Task TokenCevabiKurucununRolunuTasir()
        {
            var veri = await KayitAsync(new
            {
                email = Eposta("sahip"),
                fullName = "Filo Sahibi",
                password = "Test1234!",
                companyName = "Filo AŞ"
            });

            Assert.Equal("Owner", veri.GetProperty("role").GetString());
        }

        [Fact]
        public async Task EkipUyesininTokeniKendiRolunuTasir()
        {
            var sahipClient = _factory.CreateClient();
            var kayit = await sahipClient.PostAsJsonAsync("/api/Auth/register", new
            {
                email = Eposta("patron"),
                fullName = "Patron",
                password = "Test1234!",
                companyName = "Zimmet AŞ"
            });
            var sahipToken = JsonDocument.Parse(await kayit.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("token").GetString();
            sahipClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sahipToken);

            var eposta = Eposta("surucu");
            var ekle = await sahipClient.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Sürücü", role = "Driver" });
            var sifre = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data").GetProperty("temporaryPassword").GetString();

            var surucuClient = _factory.CreateClient();
            var giris = await surucuClient.PostAsJsonAsync("/api/Auth/login", new { email = eposta, password = sifre });
            var veri = JsonDocument.Parse(await giris.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.Equal("Driver", veri.GetProperty("role").GetString());
            Assert.Equal("Zimmet AŞ", veri.GetProperty("companyName").GetString());
        }
    }
}
