using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Garajim.Tests.Integration
{
    public class SaglikHttpTests : IClassFixture<GarajimWebApplicationFactory>
    {
        private readonly GarajimWebApplicationFactory _factory;

        public SaglikHttpTests(GarajimWebApplicationFactory factory)
        {
            _factory = factory;
        }

        private static string Eposta(string on) => $"{on}-{Guid.NewGuid():N}@garajim.local";

        private async Task<HttpClient> SahipAsync(string on)
        {
            var client = _factory.CreateClient();
            var kayit = await client.PostAsJsonAsync("/api/Auth/register",
                new { email = Eposta(on), fullName = "Sağlık", password = "Test1234!" });
            var token = await TestKayit.TokenAl(client, kayit);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        [Fact]
        public async Task SaglikBellekSayaclariniDoner()
        {
            var client = await SahipAsync("saglik");

            var cevap = await client.GetAsync("/api/Saglik");
            var veri = JsonDocument.Parse(await cevap.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Assert.Equal(HttpStatusCode.OK, cevap.StatusCode);
            Assert.True(veri.GetProperty("yonetilenBellekMb").GetDouble() > 0);
            Assert.True(veri.GetProperty("calismaKumesiMb").GetDouble() > 0);
            Assert.True(veri.GetProperty("enYuksekCalismaKumesiMb").GetDouble() > 0);
            Assert.True(veri.GetProperty("gcSayisi").GetInt32() >= 0);
            Assert.False(string.IsNullOrWhiteSpace(veri.GetProperty("surum").GetString()));
        }

        [Fact]
        public async Task SaglikGirissiz401Doner()
        {
            var cevap = await _factory.CreateClient().GetAsync("/api/Saglik");

            Assert.Equal(HttpStatusCode.Unauthorized, cevap.StatusCode);
        }

        [Fact]
        public async Task SaglikSurucuyeKapali()
        {
            var sahip = await SahipAsync("saglikowner");
            var eposta = Eposta("saglikdriver");
            var ekle = await sahip.PostAsJsonAsync("/api/Team", new { email = eposta, fullName = "Sürücü", role = "Driver" });
            var veri = JsonDocument.Parse(await ekle.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            var surucu = _factory.CreateClient();
            var giris = await surucu.PostAsJsonAsync("/api/Auth/login",
                new { email = eposta, password = veri.GetProperty("temporaryPassword").GetString() });
            var token = JsonDocument.Parse(await giris.Content.ReadAsStringAsync())
                .RootElement.GetProperty("data").GetProperty("token").GetString();
            surucu.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var cevap = await surucu.GetAsync("/api/Saglik");

            Assert.Equal(HttpStatusCode.Forbidden, cevap.StatusCode);
        }
    }
}
